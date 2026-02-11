/**
 * Add Connection Form - MCP Apps Script
 * 
 * Handles form submission and MCP server communication.
 */

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', init);

function init() {
    const form = document.getElementById('connectionForm');
    const cancelBtn = document.getElementById('cancelBtn');
    const status = document.getElementById('status');

    // Form submission handler
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleSubmit();
    });

    // Cancel button handler
    cancelBtn.addEventListener('click', () => {
        showStatus('Cancelled', 'info');
        // Optionally notify the model
        updateModelContext('User cancelled the connection form');
    });
}

/**
 * Handle form submission
 */
async function handleSubmit() {
    const formData = getFormData();
    
    if (!validateForm(formData)) {
        return;
    }

    showStatus('Creating connection...', 'info');

    try {
        // Call server tool to create the connection
        // Note: In a real implementation, this would use the MCP Apps SDK
        // await app.callServerTool({ name: 'add_connection', arguments: formData });
        
        showStatus(`Connection "${formData.connectionName}" created successfully!`, 'success');
        
        // Update model context with the result
        updateModelContext(`User created a new ${formData.connectionType} connection named "${formData.connectionName}"`);
    } catch (error) {
        showStatus(`Error: ${error.message}`, 'error');
    }
}

/**
 * Get form data as an object
 */
function getFormData() {
    return {
        connectionName: document.getElementById('connectionName').value.trim(),
        connectionType: document.getElementById('connectionType').value,
        server: document.getElementById('server').value.trim(),
        database: document.getElementById('database').value.trim()
    };
}

/**
 * Validate form data
 */
function validateForm(data) {
    if (!data.connectionName) {
        showStatus('Please enter a connection name', 'error');
        return false;
    }
    if (!data.connectionType) {
        showStatus('Please select a connection type', 'error');
        return false;
    }
    return true;
}

/**
 * Show status message
 */
function showStatus(message, type) {
    const status = document.getElementById('status');
    status.textContent = message;
    status.className = `status ${type}`;
}

/**
 * Update model context (placeholder for MCP Apps SDK integration)
 */
function updateModelContext(message) {
    // In a real implementation with MCP Apps SDK:
    // app.updateModelContext({ content: [{ type: 'text', text: message }] });
    console.log('Model context:', message);
}
