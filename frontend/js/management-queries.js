// Management Queries functionality
let availableQueries = [];

async function loadManagementQueries() {
    try {
        const response = await apiCall('/admin/queries/list');
        if (response.success && response.data) {
            availableQueries = response.data;
            displayQueries();
        } else {
            showAlert('Failed to load queries', 'danger');
        }
    } catch (error) {
        showAlert('Error loading queries: ' + error.message, 'danger');
    }
}

function displayQueries() {
    const queriesList = document.getElementById('queriesList');
    if (!queriesList) return;

    // Group queries by category
    const categories = {};
    availableQueries.forEach(query => {
        if (!categories[query.category]) {
            categories[query.category] = [];
        }
        categories[query.category].push(query);
    });

    let html = '';
    for (const [category, queries] of Object.entries(categories)) {
        html += `
            <div class="mb-4">
                <h4 class="text-white mb-3">${category}</h4>
        `;
        
        queries.forEach(query => {
            html += `
                <div class="mb-2">
                    <div class="card query-card" onclick="executeQuery('${query.id}')" style="cursor: pointer;">
                        <div class="card-body">
                            <div class="row align-items-center">
                                <div class="col-md-4">
                                    <h6 class="card-title text-white mb-0">${query.name}</h6>
                                </div>
                                <div class="col-md-8">
                                    <p class="card-text text-white-50 small mb-0">${query.description}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
        
        html += `
            </div>
        `;
    }

    queriesList.innerHTML = html;
}

async function executeQuery(queryId) {
    const resultsDiv = document.getElementById('queryResults');
    const queryInfo = availableQueries.find(q => q.id === queryId);
    
    if (!resultsDiv) return;

    // Show loading
    resultsDiv.innerHTML = `
        <div class="text-center">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-white mt-2">Executing query: ${queryInfo?.name || queryId}</p>
        </div>
    `;

    try {
        const response = await apiCall('/admin/queries/execute', {
            method: 'POST',
            body: JSON.stringify({ queryId: queryId })
        });

        if (response.success && response.data) {
            displayQueryResults(response.data, response.columns, queryInfo?.name || queryId);
        } else {
            resultsDiv.innerHTML = `
                <div class="alert alert-danger">
                    <strong>Error:</strong> ${response.error || 'Failed to execute query'}
                </div>
            `;
        }
    } catch (error) {
        resultsDiv.innerHTML = `
            <div class="alert alert-danger">
                <strong>Error:</strong> ${error.message}
            </div>
        `;
    }
}

function displayQueryResults(data, columns, queryName) {
    const resultsDiv = document.getElementById('queryResults');
    if (!resultsDiv || !data || data.length === 0) {
        resultsDiv.innerHTML = `
            <div class="alert alert-info">
                <strong>Query:</strong> ${queryName}<br>
                <strong>Result:</strong> No data returned
            </div>
        `;
        return;
    }

    let html = `
        <div class="mb-3">
            <h5 class="text-white">Query: ${queryName}</h5>
            <p class="text-white-50">Results: ${data.length} row(s)</p>
        </div>
        <div class="table-responsive">
            <table class="table table-dark table-striped table-hover">
                <thead>
                    <tr>
    `;

    columns.forEach(col => {
        html += `<th>${col}</th>`;
    });

    html += `
                    </tr>
                </thead>
                <tbody>
    `;

    data.forEach(row => {
        html += '<tr>';
        columns.forEach(col => {
            const value = row[col];
            html += `<td>${value !== null && value !== undefined ? value : '<em class="text-muted">NULL</em>'}</td>`;
        });
        html += '</tr>';
    });

    html += `
                </tbody>
            </table>
        </div>
    `;

    resultsDiv.innerHTML = html;
}

// Make functions available globally
window.loadManagementQueries = loadManagementQueries;
window.executeQuery = executeQuery;
window.displayQueries = displayQueries;

