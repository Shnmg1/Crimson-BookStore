using System.Data;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IDatabaseService _databaseService;

    public PaymentMethodService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<PaymentMethodResponse>> GetPaymentMethodsAsync(int userId)
    {
        var query = @"
            SELECT 
                PaymentMethodID,
                CardType,
                LastFourDigits,
                ExpirationDate,
                IsDefault,
                CreatedDate
            FROM PaymentMethod
            WHERE UserID = @UserID
            ORDER BY IsDefault DESC, CreatedDate DESC";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId }
        };

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

        var paymentMethods = new List<PaymentMethodResponse>();

        foreach (DataRow row in dataTable.Rows)
        {
            paymentMethods.Add(new PaymentMethodResponse
            {
                PaymentMethodId = Convert.ToInt32(row["PaymentMethodID"]),
                CardType = row["CardType"].ToString() ?? string.Empty,
                LastFourDigits = row["LastFourDigits"].ToString() ?? string.Empty,
                ExpirationDate = row["ExpirationDate"].ToString() ?? string.Empty,
                IsDefault = Convert.ToBoolean(row["IsDefault"]),
                CreatedDate = Convert.ToDateTime(row["CreatedDate"])
            });
        }

        return paymentMethods;
    }

    public async Task<int> CreatePaymentMethodAsync(int userId, CreatePaymentMethodRequest request)
    {
        // Validate expiration date format (MM/YYYY)
        if (!IsValidExpirationDate(request.ExpirationDate))
        {
            throw new ArgumentException("Invalid expiration date format. Expected MM/YYYY");
        }

        // Validate last four digits (should be 4 digits)
        if (string.IsNullOrWhiteSpace(request.LastFourDigits) || request.LastFourDigits.Length != 4 || !request.LastFourDigits.All(char.IsDigit))
        {
            throw new ArgumentException("Last four digits must be exactly 4 digits");
        }

        // Validate card type
        if (string.IsNullOrWhiteSpace(request.CardType))
        {
            throw new ArgumentException("Card type is required");
        }

        // If this is being set as default, unset all other defaults for this user
        if (request.IsDefault)
        {
            var unsetDefaultsQuery = @"
                UPDATE PaymentMethod
                SET IsDefault = 0
                WHERE UserID = @UserID AND IsDefault = 1";

            await _databaseService.ExecuteNonQueryAsync(
                unsetDefaultsQuery,
                new Dictionary<string, object> { { "@UserID", userId } }
            );
        }

        // Insert new payment method
        var insertQuery = @"
            INSERT INTO PaymentMethod (UserID, CardType, LastFourDigits, ExpirationDate, IsDefault, CreatedDate)
            VALUES (@UserID, @CardType, @LastFourDigits, @ExpirationDate, @IsDefault, NOW())";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId },
            { "@CardType", request.CardType },
            { "@LastFourDigits", request.LastFourDigits },
            { "@ExpirationDate", request.ExpirationDate },
            { "@IsDefault", request.IsDefault ? 1 : 0 }
        };

        await _databaseService.ExecuteNonQueryAsync(insertQuery, parameters);

        // Get the PaymentMethodID that was just inserted
        var getPaymentMethodIdQuery = @"
            SELECT PaymentMethodID
            FROM PaymentMethod
            WHERE UserID = @UserID AND CardType = @CardType AND LastFourDigits = @LastFourDigits
            ORDER BY CreatedDate DESC
            LIMIT 1";

        var result = await _databaseService.ExecuteQueryAsync(getPaymentMethodIdQuery, parameters);

        if (result.Rows.Count > 0)
        {
            return Convert.ToInt32(result.Rows[0]["PaymentMethodID"]);
        }

        throw new InvalidOperationException("Failed to create payment method");
    }

    public async Task<bool> DeletePaymentMethodAsync(int userId, int paymentMethodId)
    {
        // First, verify the payment method belongs to the user
        var verifyQuery = @"
            SELECT PaymentMethodID
            FROM PaymentMethod
            WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID";

        var verifyParams = new Dictionary<string, object>
        {
            { "@PaymentMethodID", paymentMethodId },
            { "@UserID", userId }
        };

        var verifyResult = await _databaseService.ExecuteQueryAsync(verifyQuery, verifyParams);

        if (verifyResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Payment method not found or does not belong to user");
        }

        // Check if payment method is referenced in any Payment records
        var checkUsageQuery = @"
            SELECT COUNT(*) as UsageCount
            FROM Payment
            WHERE PaymentMethodID = @PaymentMethodID";

        var usageResult = await _databaseService.ExecuteScalarAsync(
            checkUsageQuery,
            new Dictionary<string, object> { { "@PaymentMethodID", paymentMethodId } }
        );

        var usageCount = Convert.ToInt32(usageResult);
        if (usageCount > 0)
        {
            throw new InvalidOperationException("Cannot delete payment method that has been used in orders");
        }

        // Delete the payment method
        var deleteQuery = @"
            DELETE FROM PaymentMethod
            WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID";

        var rowsAffected = await _databaseService.ExecuteNonQueryAsync(deleteQuery, verifyParams);
        return rowsAffected > 0;
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId)
    {
        // Verify the payment method belongs to the user
        var verifyQuery = @"
            SELECT PaymentMethodID
            FROM PaymentMethod
            WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID";

        var verifyParams = new Dictionary<string, object>
        {
            { "@PaymentMethodID", paymentMethodId },
            { "@UserID", userId }
        };

        var verifyResult = await _databaseService.ExecuteQueryAsync(verifyQuery, verifyParams);

        if (verifyResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Payment method not found or does not belong to user");
        }

        // Unset all other defaults for this user
        var unsetDefaultsQuery = @"
            UPDATE PaymentMethod
            SET IsDefault = 0
            WHERE UserID = @UserID AND IsDefault = 1";

        await _databaseService.ExecuteNonQueryAsync(
            unsetDefaultsQuery,
            new Dictionary<string, object> { { "@UserID", userId } }
        );

        // Set this payment method as default
        var setDefaultQuery = @"
            UPDATE PaymentMethod
            SET IsDefault = 1
            WHERE PaymentMethodID = @PaymentMethodID AND UserID = @UserID";

        var rowsAffected = await _databaseService.ExecuteNonQueryAsync(setDefaultQuery, verifyParams);
        return rowsAffected > 0;
    }

    private bool IsValidExpirationDate(string expirationDate)
    {
        if (string.IsNullOrWhiteSpace(expirationDate))
        {
            return false;
        }

        // Expected format: MM/YYYY
        var parts = expirationDate.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out int month) || month < 1 || month > 12)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out int year) || year < 2000 || year > 2099)
        {
            return false;
        }

        return true;
    }
}

