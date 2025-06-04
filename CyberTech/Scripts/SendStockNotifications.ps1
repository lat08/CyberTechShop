# Send stock notifications PowerShell script
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
try {
    $apiUrl = "http://localhost:5246/api/StockNotification/send"
    Write-Host "[$timestamp] Checking stock notifications..."
    
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post
    
    Write-Host "[$timestamp] Processed: $($response.sentCount)/$($response.totalCount) notifications"
    
    if ($response.errors.Count -gt 0) {
        Write-Host "[$timestamp] Errors: $($response.errors.Count)"
        foreach ($error in $response.errors) {
            Write-Host "- $error"
        }
    }
}
catch {
    Write-Host "[$timestamp] Error: $_"
    exit 1
} 