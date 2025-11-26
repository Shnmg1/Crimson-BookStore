using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using CrimsonBookStore.Models;

namespace CrimsonBookStore.Services;

public class SellSubmissionService : ISellSubmissionService
{
    private readonly IDatabaseService _databaseService;
    private readonly string _connectionString;

    public SellSubmissionService(IDatabaseService databaseService, IConfiguration configuration)
    {
        _databaseService = databaseService;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<SellSubmissionCreateResponse> CreateSellSubmissionAsync(int userId, CreateSellSubmissionRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.ISBN) || 
            string.IsNullOrWhiteSpace(request.Title) || 
            string.IsNullOrWhiteSpace(request.Author) || 
            string.IsNullOrWhiteSpace(request.Edition) ||
            string.IsNullOrWhiteSpace(request.PhysicalCondition) ||
            request.AskingPrice <= 0)
        {
            throw new ArgumentException("All required fields must be provided and asking price must be greater than 0");
        }

        // Validate condition
        if (!new[] { "New", "Good", "Fair" }.Contains(request.PhysicalCondition))
        {
            throw new ArgumentException("PhysicalCondition must be 'New', 'Good', or 'Fair'");
        }

        var query = @"
            INSERT INTO SellSubmission (
                UserID, ISBN, Title, Author, Edition, 
                PhysicalCondition, CourseMajor, AskingPrice, 
                Status, SubmissionDate
            )
            VALUES (
                @UserID, @ISBN, @Title, @Author, @Edition,
                @PhysicalCondition, @CourseMajor, @AskingPrice,
                'Pending Review', CURRENT_TIMESTAMP
            )";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId },
            { "@ISBN", request.ISBN },
            { "@Title", request.Title },
            { "@Author", request.Author },
            { "@Edition", request.Edition },
            { "@PhysicalCondition", request.PhysicalCondition },
            { "@AskingPrice", request.AskingPrice }
        };

        if (!string.IsNullOrWhiteSpace(request.CourseMajor))
        {
            parameters.Add("@CourseMajor", request.CourseMajor);
        }
        else
        {
            parameters.Add("@CourseMajor", DBNull.Value);
        }

        await _databaseService.ExecuteNonQueryAsync(query, parameters);

        // Get the SubmissionID that was just inserted
        var submissionIdQuery = "SELECT LAST_INSERT_ID()";
        var submissionIdObj = await _databaseService.ExecuteScalarAsync(submissionIdQuery);
        var submissionId = Convert.ToInt32(submissionIdObj);

        // Get submission date
        var dateQuery = "SELECT SubmissionDate FROM SellSubmission WHERE SubmissionID = @SubmissionID";
        var dateResult = await _databaseService.ExecuteQueryAsync(
            dateQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );
        var submissionDate = Convert.ToDateTime(dateResult.Rows[0]["SubmissionDate"]);

        return new SellSubmissionCreateResponse
        {
            SubmissionId = submissionId,
            Status = "Pending Review",
            SubmissionDate = submissionDate
        };
    }

    public async Task<List<SellSubmissionListResponse>> GetUserSubmissionsAsync(int userId, string? status = null)
    {
        var query = @"
            SELECT 
                SubmissionID,
                ISBN,
                Title,
                Author,
                Edition,
                AskingPrice,
                Status,
                SubmissionDate
            FROM SellSubmission
            WHERE UserID = @UserID";

        var parameters = new Dictionary<string, object>
        {
            { "@UserID", userId }
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query += " AND Status = @Status";
            parameters.Add("@Status", status);
        }

        query += " ORDER BY SubmissionDate DESC";

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters);

        var submissions = new List<SellSubmissionListResponse>();

        foreach (DataRow row in dataTable.Rows)
        {
            submissions.Add(new SellSubmissionListResponse
            {
                SubmissionId = Convert.ToInt32(row["SubmissionID"]),
                ISBN = row["ISBN"].ToString() ?? string.Empty,
                Title = row["Title"].ToString() ?? string.Empty,
                Author = row["Author"].ToString() ?? string.Empty,
                Edition = row["Edition"].ToString() ?? string.Empty,
                AskingPrice = Convert.ToDecimal(row["AskingPrice"]),
                Status = row["Status"].ToString() ?? string.Empty,
                SubmissionDate = Convert.ToDateTime(row["SubmissionDate"])
            });
        }

        return submissions;
    }

    public async Task<SellSubmissionResponse?> GetSubmissionDetailsAsync(int submissionId, int userId)
    {
        // First verify the submission belongs to the user
        var verifyQuery = @"
            SELECT SubmissionID, UserID
            FROM SellSubmission
            WHERE SubmissionID = @SubmissionID";

        var verifyResult = await _databaseService.ExecuteQueryAsync(
            verifyQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (verifyResult.Rows.Count == 0)
        {
            return null; // Submission not found
        }

        var submissionUserId = Convert.ToInt32(verifyResult.Rows[0]["UserID"]);
        if (submissionUserId != userId)
        {
            throw new UnauthorizedAccessException("Submission does not belong to you");
        }

        // Get submission details
        var submissionQuery = @"
            SELECT 
                SubmissionID,
                ISBN,
                Title,
                Author,
                Edition,
                AskingPrice,
                Status,
                SubmissionDate
            FROM SellSubmission
            WHERE SubmissionID = @SubmissionID";

        var submissionResult = await _databaseService.ExecuteQueryAsync(
            submissionQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (submissionResult.Rows.Count == 0)
        {
            return null;
        }

        var submissionRow = submissionResult.Rows[0];

        // Get negotiation history
        var negotiationsQuery = @"
            SELECT 
                NegotiationID,
                OfferedBy,
                OfferedPrice,
                OfferDate,
                OfferMessage,
                OfferStatus,
                RoundNumber
            FROM PriceNegotiation
            WHERE SubmissionID = @SubmissionID
            ORDER BY RoundNumber ASC";

        var negotiationsResult = await _databaseService.ExecuteQueryAsync(
            negotiationsQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        var negotiations = new List<PriceNegotiationResponse>();
        foreach (DataRow row in negotiationsResult.Rows)
        {
            negotiations.Add(new PriceNegotiationResponse
            {
                NegotiationId = Convert.ToInt32(row["NegotiationID"]),
                OfferedBy = row["OfferedBy"].ToString() ?? string.Empty,
                OfferedPrice = Convert.ToDecimal(row["OfferedPrice"]),
                OfferDate = Convert.ToDateTime(row["OfferDate"]),
                OfferMessage = row["OfferMessage"] == DBNull.Value ? null : row["OfferMessage"].ToString(),
                OfferStatus = row["OfferStatus"].ToString() ?? string.Empty,
                RoundNumber = Convert.ToInt32(row["RoundNumber"])
            });
        }

        return new SellSubmissionResponse
        {
            SubmissionId = Convert.ToInt32(submissionRow["SubmissionID"]),
            ISBN = submissionRow["ISBN"].ToString() ?? string.Empty,
            Title = submissionRow["Title"].ToString() ?? string.Empty,
            Author = submissionRow["Author"].ToString() ?? string.Empty,
            Edition = submissionRow["Edition"].ToString() ?? string.Empty,
            AskingPrice = Convert.ToDecimal(submissionRow["AskingPrice"]),
            Status = submissionRow["Status"].ToString() ?? string.Empty,
            SubmissionDate = Convert.ToDateTime(submissionRow["SubmissionDate"]),
            Negotiations = negotiations
        };
    }

    public async Task<NegotiateResponse> NegotiateAsync(int submissionId, int userId, NegotiateRequest request)
    {
        // Verify submission belongs to user
        var verifyQuery = @"
            SELECT SubmissionID, UserID, Status
            FROM SellSubmission
            WHERE SubmissionID = @SubmissionID";

        var verifyResult = await _databaseService.ExecuteQueryAsync(
            verifyQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (verifyResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Submission not found");
        }

        var submissionUserId = Convert.ToInt32(verifyResult.Rows[0]["UserID"]);
        var submissionStatus = verifyResult.Rows[0]["Status"].ToString() ?? string.Empty;

        if (submissionUserId != userId)
        {
            throw new UnauthorizedAccessException("Submission does not belong to you");
        }

        if (submissionStatus != "Pending Review")
        {
            throw new InvalidOperationException("Submission is not in Pending Review status");
        }

        var action = request.Action.ToLower();
        
        if (action == "accept")
        {
            if (!request.NegotiationId.HasValue)
            {
                throw new ArgumentException("NegotiationId is required for accept action");
            }

            // Verify negotiation exists and is pending
            var negotiationQuery = @"
                SELECT NegotiationID, OfferStatus, OfferedPrice, OfferedBy
                FROM PriceNegotiation
                WHERE NegotiationID = @NegotiationID AND SubmissionID = @SubmissionID";

            var negotiationResult = await _databaseService.ExecuteQueryAsync(
                negotiationQuery,
                new Dictionary<string, object> 
                { 
                    { "@NegotiationID", request.NegotiationId.Value },
                    { "@SubmissionID", submissionId }
                }
            );

            if (negotiationResult.Rows.Count == 0)
            {
                throw new KeyNotFoundException("Negotiation not found");
            }

            var offerStatus = negotiationResult.Rows[0]["OfferStatus"].ToString() ?? string.Empty;
            var offeredBy = negotiationResult.Rows[0]["OfferedBy"].ToString() ?? string.Empty;
            
            if (offerStatus != "Pending")
            {
                throw new InvalidOperationException("Negotiation is not in Pending status");
            }
            
            // Customer can only accept Admin offers
            if (offeredBy != "Admin")
            {
                throw new InvalidOperationException("You can only accept offers from Admin");
            }

            // Check if this is the LATEST pending admin offer
            var latestPendingQuery = @"
                SELECT NegotiationID
                FROM PriceNegotiation
                WHERE SubmissionID = @SubmissionID
                  AND OfferedBy = 'Admin'
                  AND OfferStatus = 'Pending'
                ORDER BY RoundNumber DESC
                LIMIT 1";

            var latestPendingResult = await _databaseService.ExecuteQueryAsync(
                latestPendingQuery,
                new Dictionary<string, object> { { "@SubmissionID", submissionId } }
            );

            if (latestPendingResult.Rows.Count == 0)
            {
                throw new InvalidOperationException("No pending admin offer found");
            }

            var latestNegotiationId = Convert.ToInt32(latestPendingResult.Rows[0]["NegotiationID"]);

            if (request.NegotiationId.Value != latestNegotiationId)
            {
                throw new InvalidOperationException("You can only accept the latest admin offer. Please refresh and accept the most recent offer.");
            }

            // Start transaction for accept
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Update negotiation status
                var updateNegotiationQuery = @"
                    UPDATE PriceNegotiation
                    SET OfferStatus = 'Accepted'
                    WHERE NegotiationID = @NegotiationID";

                await using var updateNegotiationCmd = new MySqlCommand(updateNegotiationQuery, connection, transaction);
                updateNegotiationCmd.Parameters.AddWithValue("@NegotiationID", request.NegotiationId.Value);
                await updateNegotiationCmd.ExecuteNonQueryAsync();

                // Update submission status to Approved
                var updateSubmissionQuery = @"
                    UPDATE SellSubmission
                    SET Status = 'Approved'
                    WHERE SubmissionID = @SubmissionID";

                await using var updateSubmissionCmd = new MySqlCommand(updateSubmissionQuery, connection, transaction);
                updateSubmissionCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
                await updateSubmissionCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return new NegotiateResponse
                {
                    NegotiationId = request.NegotiationId.Value,
                    OfferStatus = "Accepted",
                    Message = "Price accepted. Book will be added to inventory."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else if (action == "reject")
        {
            if (!request.NegotiationId.HasValue)
            {
                throw new ArgumentException("NegotiationId is required for reject action");
            }

            // Verify negotiation exists
            var negotiationQuery = @"
                SELECT NegotiationID, OfferStatus, OfferedBy
                FROM PriceNegotiation
                WHERE NegotiationID = @NegotiationID AND SubmissionID = @SubmissionID";

            var negotiationResult = await _databaseService.ExecuteQueryAsync(
                negotiationQuery,
                new Dictionary<string, object> 
                { 
                    { "@NegotiationID", request.NegotiationId.Value },
                    { "@SubmissionID", submissionId }
                }
            );

            if (negotiationResult.Rows.Count == 0)
            {
                throw new KeyNotFoundException("Negotiation not found");
            }
            
            var offeredBy = negotiationResult.Rows[0]["OfferedBy"].ToString() ?? string.Empty;
            
            // Customer can only reject Admin offers
            if (offeredBy != "Admin")
            {
                throw new InvalidOperationException("You can only reject offers from Admin");
            }

            // Check if there are other pending admin offers
            var otherPendingQuery = @"
                SELECT COUNT(*) as pending_count
                FROM PriceNegotiation
                WHERE SubmissionID = @SubmissionID
                  AND OfferedBy = 'Admin'
                  AND OfferStatus = 'Pending'
                  AND NegotiationID != @NegotiationID";

            var otherPendingResult = await _databaseService.ExecuteQueryAsync(
                otherPendingQuery,
                new Dictionary<string, object> 
                { 
                    { "@SubmissionID", submissionId },
                    { "@NegotiationID", request.NegotiationId.Value }
                }
            );

            var otherPendingCount = Convert.ToInt32(otherPendingResult.Rows[0]["pending_count"]);

            // Start transaction for reject
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Update negotiation status
                var updateNegotiationQuery = @"
                    UPDATE PriceNegotiation
                    SET OfferStatus = 'Rejected'
                    WHERE NegotiationID = @NegotiationID";

                await using var updateNegotiationCmd = new MySqlCommand(updateNegotiationQuery, connection, transaction);
                updateNegotiationCmd.Parameters.AddWithValue("@NegotiationID", request.NegotiationId.Value);
                await updateNegotiationCmd.ExecuteNonQueryAsync();

                // Only reject submission if this was the only pending offer
                if (otherPendingCount == 0)
                {
                    var updateSubmissionQuery = @"
                        UPDATE SellSubmission
                        SET Status = 'Rejected'
                        WHERE SubmissionID = @SubmissionID";

                    await using var updateSubmissionCmd = new MySqlCommand(updateSubmissionQuery, connection, transaction);
                    updateSubmissionCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
                    await updateSubmissionCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new NegotiateResponse
                {
                    NegotiationId = request.NegotiationId.Value,
                    OfferStatus = "Rejected",
                    Message = otherPendingCount > 0 
                        ? "Offer rejected. Other offers may still be pending." 
                        : "Offer rejected. Submission has been rejected."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else if (action == "counter")
        {
            if (!request.OfferedPrice.HasValue || request.OfferedPrice.Value <= 0)
            {
                throw new ArgumentException("OfferedPrice is required and must be greater than 0 for counter action");
            }

            // Check if there's a pending negotiation from Admin that customer can counter
            var lastNegotiationQuery = @"
                SELECT OfferedBy, OfferStatus
                FROM PriceNegotiation
                WHERE SubmissionID = @SubmissionID
                ORDER BY RoundNumber DESC
                LIMIT 1";

            var lastNegotiationResult = await _databaseService.ExecuteQueryAsync(
                lastNegotiationQuery,
                new Dictionary<string, object> { { "@SubmissionID", submissionId } }
            );

            if (lastNegotiationResult.Rows.Count > 0)
            {
                var lastOfferedBy = lastNegotiationResult.Rows[0]["OfferedBy"].ToString() ?? string.Empty;
                var lastOfferStatus = lastNegotiationResult.Rows[0]["OfferStatus"].ToString() ?? string.Empty;
                
                // Customer can only counter Admin offers
                if (lastOfferedBy != "Admin" || lastOfferStatus != "Pending")
                {
                    throw new InvalidOperationException("You can only counter pending offers from Admin");
                }
            }
            else
            {
                // No negotiations yet - customer can't counter without an admin offer first
                throw new InvalidOperationException("No admin offer to counter. Please wait for admin to make an offer.");
            }

            // Get next round number
            var roundNumberQuery = @"
                SELECT COALESCE(MAX(RoundNumber), 0) + 1 as next_round
                FROM PriceNegotiation
                WHERE SubmissionID = @SubmissionID";

            var roundNumberResult = await _databaseService.ExecuteScalarAsync(
                roundNumberQuery,
                new Dictionary<string, object> { { "@SubmissionID", submissionId } }
            );
            var roundNumber = Convert.ToInt32(roundNumberResult);

            // Insert new negotiation round
            var insertNegotiationQuery = @"
                INSERT INTO PriceNegotiation (
                    SubmissionID, OfferedBy, OfferedPrice, 
                    OfferDate, OfferMessage, OfferStatus, RoundNumber
                )
                VALUES (
                    @SubmissionID, 'User', @OfferedPrice,
                    CURRENT_TIMESTAMP, @OfferMessage, 'Pending', @RoundNumber
                )";

            var parameters = new Dictionary<string, object>
            {
                { "@SubmissionID", submissionId },
                { "@OfferedPrice", request.OfferedPrice.Value },
                { "@RoundNumber", roundNumber }
            };

            if (!string.IsNullOrWhiteSpace(request.OfferMessage))
            {
                parameters.Add("@OfferMessage", request.OfferMessage);
            }
            else
            {
                parameters.Add("@OfferMessage", DBNull.Value);
            }

            await _databaseService.ExecuteNonQueryAsync(insertNegotiationQuery, parameters);

            // Get the NegotiationID that was just inserted
            var negotiationIdQuery = "SELECT LAST_INSERT_ID()";
            var negotiationIdObj = await _databaseService.ExecuteScalarAsync(negotiationIdQuery);
            var negotiationId = Convert.ToInt32(negotiationIdObj);

            return new NegotiateResponse
            {
                NegotiationId = negotiationId,
                RoundNumber = roundNumber,
                OfferedPrice = request.OfferedPrice.Value,
                OfferStatus = "Pending",
                Message = "Counter-offer submitted."
            };
        }
        else
        {
            throw new ArgumentException("Action must be 'accept', 'reject', or 'counter'");
        }
    }

    public async Task<List<AdminSellSubmissionListResponse>> GetAdminSubmissionsAsync(string? status = null)
    {
        var query = @"
            SELECT 
                ss.SubmissionID,
                ss.UserID,
                u.Username,
                ss.ISBN,
                ss.Title,
                ss.AskingPrice,
                ss.Status,
                ss.SubmissionDate
            FROM SellSubmission ss
            JOIN User u ON ss.UserID = u.UserID";

        var parameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query += " WHERE ss.Status = @Status";
            parameters.Add("@Status", status);
        }

        query += " ORDER BY ss.SubmissionDate DESC";

        var dataTable = await _databaseService.ExecuteQueryAsync(query, parameters.Count > 0 ? parameters : null);

        var submissions = new List<AdminSellSubmissionListResponse>();

        foreach (DataRow row in dataTable.Rows)
        {
            submissions.Add(new AdminSellSubmissionListResponse
            {
                SubmissionId = Convert.ToInt32(row["SubmissionID"]),
                UserId = Convert.ToInt32(row["UserID"]),
                Username = row["Username"].ToString() ?? string.Empty,
                ISBN = row["ISBN"].ToString() ?? string.Empty,
                Title = row["Title"].ToString() ?? string.Empty,
                AskingPrice = Convert.ToDecimal(row["AskingPrice"]),
                Status = row["Status"].ToString() ?? string.Empty,
                SubmissionDate = Convert.ToDateTime(row["SubmissionDate"])
            });
        }

        return submissions;
    }

    public async Task<AdminSellSubmissionResponse?> GetAdminSubmissionDetailsAsync(int submissionId)
    {
        // Get submission details
        var submissionQuery = @"
            SELECT 
                ss.SubmissionID,
                ss.UserID,
                u.Username,
                ss.ISBN,
                ss.Title,
                ss.Author,
                ss.Edition,
                ss.AskingPrice,
                ss.PhysicalCondition,
                ss.CourseMajor,
                ss.Status,
                ss.SubmissionDate
            FROM SellSubmission ss
            JOIN User u ON ss.UserID = u.UserID
            WHERE ss.SubmissionID = @SubmissionID";

        var submissionResult = await _databaseService.ExecuteQueryAsync(
            submissionQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (submissionResult.Rows.Count == 0)
        {
            return null;
        }

        var submissionRow = submissionResult.Rows[0];

        // Get negotiation history
        var negotiationsQuery = @"
            SELECT 
                NegotiationID,
                OfferedBy,
                OfferedPrice,
                OfferDate,
                OfferMessage,
                OfferStatus,
                RoundNumber
            FROM PriceNegotiation
            WHERE SubmissionID = @SubmissionID
            ORDER BY RoundNumber ASC";

        var negotiationsResult = await _databaseService.ExecuteQueryAsync(
            negotiationsQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        var negotiations = new List<PriceNegotiationResponse>();
        foreach (DataRow row in negotiationsResult.Rows)
        {
            negotiations.Add(new PriceNegotiationResponse
            {
                NegotiationId = Convert.ToInt32(row["NegotiationID"]),
                OfferedBy = row["OfferedBy"].ToString() ?? string.Empty,
                OfferedPrice = Convert.ToDecimal(row["OfferedPrice"]),
                OfferDate = Convert.ToDateTime(row["OfferDate"]),
                OfferMessage = row["OfferMessage"] == DBNull.Value ? null : row["OfferMessage"].ToString(),
                OfferStatus = row["OfferStatus"].ToString() ?? string.Empty,
                RoundNumber = Convert.ToInt32(row["RoundNumber"])
            });
        }

        return new AdminSellSubmissionResponse
        {
            SubmissionId = Convert.ToInt32(submissionRow["SubmissionID"]),
            UserId = Convert.ToInt32(submissionRow["UserID"]),
            Username = submissionRow["Username"].ToString() ?? string.Empty,
            ISBN = submissionRow["ISBN"].ToString() ?? string.Empty,
            Title = submissionRow["Title"].ToString() ?? string.Empty,
            Author = submissionRow["Author"].ToString() ?? string.Empty,
            Edition = submissionRow["Edition"].ToString() ?? string.Empty,
            AskingPrice = Convert.ToDecimal(submissionRow["AskingPrice"]),
            PhysicalCondition = submissionRow["PhysicalCondition"].ToString() ?? string.Empty,
            CourseMajor = submissionRow["CourseMajor"] == DBNull.Value ? null : submissionRow["CourseMajor"].ToString(),
            Status = submissionRow["Status"].ToString() ?? string.Empty,
            SubmissionDate = Convert.ToDateTime(submissionRow["SubmissionDate"]),
            Negotiations = negotiations
        };
    }

    public async Task<NegotiateResponse> AdminNegotiateAsync(int submissionId, int adminUserId, AdminNegotiateRequest request)
    {
        if (request.OfferedPrice <= 0)
        {
            throw new ArgumentException("OfferedPrice must be greater than 0");
        }

        // Verify submission exists and is in pending status
        var verifyQuery = @"
            SELECT SubmissionID, Status
            FROM SellSubmission
            WHERE SubmissionID = @SubmissionID";

        var verifyResult = await _databaseService.ExecuteQueryAsync(
            verifyQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (verifyResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Submission not found");
        }

        var submissionStatus = verifyResult.Rows[0]["Status"].ToString() ?? string.Empty;
        if (submissionStatus != "Pending Review")
        {
            throw new InvalidOperationException("Submission is not in Pending Review status");
        }

        // Check if there's a pending negotiation from User (customer counter-offer)
        // Admin can only negotiate if there's no pending admin offer or if the last pending offer is from User
        var lastNegotiationQuery = @"
            SELECT OfferedBy, OfferStatus
            FROM PriceNegotiation
            WHERE SubmissionID = @SubmissionID
            ORDER BY RoundNumber DESC
            LIMIT 1";

        var lastNegotiationResult = await _databaseService.ExecuteQueryAsync(
            lastNegotiationQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (lastNegotiationResult.Rows.Count > 0)
        {
            var lastOfferedBy = lastNegotiationResult.Rows[0]["OfferedBy"].ToString() ?? string.Empty;
            var lastOfferStatus = lastNegotiationResult.Rows[0]["OfferStatus"].ToString() ?? string.Empty;
            
            // If last offer is from Admin and still pending, admin can't make another offer
            if (lastOfferedBy == "Admin" && lastOfferStatus == "Pending")
            {
                throw new InvalidOperationException("You already have a pending offer. Wait for customer response.");
            }
            
            // If last offer was accepted or rejected, negotiation is closed
            if (lastOfferStatus == "Accepted" || lastOfferStatus == "Rejected")
            {
                throw new InvalidOperationException("Negotiation has been closed. Cannot make new offers.");
            }
        }

        // Auto-reject any previous pending admin offers (superseded by new offer)
        var rejectOldOffersQuery = @"
            UPDATE PriceNegotiation
            SET OfferStatus = 'Rejected'
            WHERE SubmissionID = @SubmissionID
              AND OfferedBy = 'Admin'
              AND OfferStatus = 'Pending'";

        await _databaseService.ExecuteNonQueryAsync(rejectOldOffersQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } });

        // Get next round number
        var roundNumberQuery = @"
            SELECT COALESCE(MAX(RoundNumber), 0) + 1 as next_round
            FROM PriceNegotiation
            WHERE SubmissionID = @SubmissionID";

        var roundNumberResult = await _databaseService.ExecuteScalarAsync(
            roundNumberQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );
        var roundNumber = Convert.ToInt32(roundNumberResult);

        // Insert new negotiation round
        var insertNegotiationQuery = @"
            INSERT INTO PriceNegotiation (
                SubmissionID, OfferedBy, OfferedPrice, 
                OfferDate, OfferMessage, OfferStatus, RoundNumber
            )
            VALUES (
                @SubmissionID, 'Admin', @OfferedPrice,
                CURRENT_TIMESTAMP, @OfferMessage, 'Pending', @RoundNumber
            )";

        var parameters = new Dictionary<string, object>
        {
            { "@SubmissionID", submissionId },
            { "@OfferedPrice", request.OfferedPrice },
            { "@RoundNumber", roundNumber }
        };

        if (!string.IsNullOrWhiteSpace(request.OfferMessage))
        {
            parameters.Add("@OfferMessage", request.OfferMessage);
        }
        else
        {
            parameters.Add("@OfferMessage", DBNull.Value);
        }

        await _databaseService.ExecuteNonQueryAsync(insertNegotiationQuery, parameters);

        // Get the NegotiationID that was just inserted
        var negotiationIdQuery = "SELECT LAST_INSERT_ID()";
        var negotiationIdObj = await _databaseService.ExecuteScalarAsync(negotiationIdQuery);
        var negotiationId = Convert.ToInt32(negotiationIdObj);

        return new NegotiateResponse
        {
            NegotiationId = negotiationId,
            RoundNumber = roundNumber,
            OfferedPrice = request.OfferedPrice,
            OfferStatus = "Pending"
        };
    }

    public async Task<ApproveSubmissionResponse> ApproveSubmissionAsync(int submissionId, int adminUserId, ApproveSubmissionRequest request)
    {
        if (request.SellingPrice <= 0)
        {
            throw new ArgumentException("SellingPrice must be greater than 0");
        }

        // Get submission details
        var submissionQuery = @"
            SELECT 
                ss.SubmissionID,
                ss.ISBN,
                ss.Title,
                ss.Author,
                ss.Edition,
                ss.PhysicalCondition,
                ss.CourseMajor,
                ss.Status,
                ss.AskingPrice
            FROM SellSubmission ss
            WHERE ss.SubmissionID = @SubmissionID";

        var submissionResult = await _databaseService.ExecuteQueryAsync(
            submissionQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (submissionResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Submission not found");
        }

        var submissionRow = submissionResult.Rows[0];
        var submissionStatus = submissionRow["Status"].ToString() ?? string.Empty;

        // Check if a book already exists for this submission (already processed)
        var existingBookQuery = @"
            SELECT COUNT(*) as book_count
            FROM Book
            WHERE SubmissionID = @SubmissionID";

        var existingBookResult = await _databaseService.ExecuteQueryAsync(
            existingBookQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        var bookCount = Convert.ToInt32(existingBookResult.Rows[0]["book_count"]);
        if (bookCount > 0)
        {
            throw new InvalidOperationException("This submission has already been approved and a book has been created. Cannot approve again.");
        }

        // Check if submission is in correct status
        if (submissionStatus != "Pending Review" && submissionStatus != "Approved")
        {
            throw new InvalidOperationException($"Submission is in {submissionStatus} status and cannot be approved");
        }

        decimal acquisitionCost;
        
        // Check if there's an accepted negotiation
        var negotiationQuery = @"
            SELECT OfferedPrice
            FROM PriceNegotiation
            WHERE SubmissionID = @SubmissionID
              AND OfferStatus = 'Accepted'
            ORDER BY RoundNumber DESC
            LIMIT 1";

        var negotiationResult = await _databaseService.ExecuteQueryAsync(
            negotiationQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (negotiationResult.Rows.Count > 0)
        {
            // Use accepted negotiation price as acquisition cost
            acquisitionCost = Convert.ToDecimal(negotiationResult.Rows[0]["OfferedPrice"]);
            
            if (submissionStatus != "Approved")
            {
                throw new InvalidOperationException("Submission must be in Approved status (customer must accept negotiation first)");
            }
        }
        else
        {
            // No negotiations - use asking price as acquisition cost (initial approval)
            acquisitionCost = Convert.ToDecimal(submissionRow["AskingPrice"]);
            
            if (submissionStatus != "Pending Review")
            {
                throw new InvalidOperationException("Submission must be in Pending Review status for initial approval");
            }
        }

        if (request.SellingPrice <= acquisitionCost)
        {
            throw new ArgumentException($"SellingPrice (${request.SellingPrice}) must be greater than AcquisitionCost (${acquisitionCost})");
        }

        // Start transaction for approval (update submission, create book)
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Update submission status to Completed (book has been created) and set AdminUserID
            var updateSubmissionQuery = @"
                UPDATE SellSubmission
                SET Status = 'Completed', AdminUserID = @AdminUserID
                WHERE SubmissionID = @SubmissionID";

            await using var updateSubmissionCmd = new MySqlCommand(updateSubmissionQuery, connection, transaction);
            updateSubmissionCmd.Parameters.AddWithValue("@AdminUserID", adminUserId);
            updateSubmissionCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
            await updateSubmissionCmd.ExecuteNonQueryAsync();

            // Create Book record
            var createBookQuery = @"
                INSERT INTO Book (
                    SubmissionID, ISBN, Title, Author, Edition,
                    SellingPrice, AcquisitionCost, BookCondition,
                    CourseMajor, Status, CreatedDate
                )
                VALUES (
                    @SubmissionID, @ISBN, @Title, @Author, @Edition,
                    @SellingPrice, @AcquisitionCost, @BookCondition,
                    @CourseMajor, 'Available', CURRENT_TIMESTAMP
                )";

            await using var createBookCmd = new MySqlCommand(createBookQuery, connection, transaction);
            createBookCmd.Parameters.AddWithValue("@SubmissionID", submissionId);
            createBookCmd.Parameters.AddWithValue("@ISBN", submissionRow["ISBN"]);
            createBookCmd.Parameters.AddWithValue("@Title", submissionRow["Title"]);
            createBookCmd.Parameters.AddWithValue("@Author", submissionRow["Author"]);
            createBookCmd.Parameters.AddWithValue("@Edition", submissionRow["Edition"]);
            createBookCmd.Parameters.AddWithValue("@SellingPrice", request.SellingPrice);
            createBookCmd.Parameters.AddWithValue("@AcquisitionCost", acquisitionCost);
            createBookCmd.Parameters.AddWithValue("@BookCondition", submissionRow["PhysicalCondition"]);
            
            if (submissionRow["CourseMajor"] != DBNull.Value)
            {
                createBookCmd.Parameters.AddWithValue("@CourseMajor", submissionRow["CourseMajor"]);
            }
            else
            {
                createBookCmd.Parameters.AddWithValue("@CourseMajor", DBNull.Value);
            }

            await createBookCmd.ExecuteNonQueryAsync();

            // Get the BookID that was just inserted
            var bookIdQuery = "SELECT LAST_INSERT_ID()";
            await using var bookIdCmd = new MySqlCommand(bookIdQuery, connection, transaction);
            var bookIdObj = await bookIdCmd.ExecuteScalarAsync();
            var bookId = Convert.ToInt32(bookIdObj);

            await transaction.CommitAsync();

            return new ApproveSubmissionResponse
            {
                SubmissionId = submissionId,
                BookId = bookId,
                Status = "Completed"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RejectSubmissionAsync(int submissionId, int adminUserId, RejectSubmissionRequest request)
    {
        // Verify submission exists
        var verifyQuery = @"
            SELECT SubmissionID, Status
            FROM SellSubmission
            WHERE SubmissionID = @SubmissionID";

        var verifyResult = await _databaseService.ExecuteQueryAsync(
            verifyQuery,
            new Dictionary<string, object> { { "@SubmissionID", submissionId } }
        );

        if (verifyResult.Rows.Count == 0)
        {
            throw new KeyNotFoundException("Submission not found");
        }

        var submissionStatus = verifyResult.Rows[0]["Status"].ToString() ?? string.Empty;
        if (submissionStatus == "Rejected" || submissionStatus == "Approved")
        {
            throw new InvalidOperationException($"Submission is already {submissionStatus}");
        }

        // Update submission status to Rejected and set AdminUserID
        var updateQuery = @"
            UPDATE SellSubmission
            SET Status = 'Rejected', AdminUserID = @AdminUserID
            WHERE SubmissionID = @SubmissionID";

        var parameters = new Dictionary<string, object>
        {
            { "@SubmissionID", submissionId },
            { "@AdminUserID", adminUserId }
        };

        await _databaseService.ExecuteNonQueryAsync(updateQuery, parameters);

        return true;
    }
}

