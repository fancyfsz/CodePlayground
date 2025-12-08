param(
    [Parameter(Mandatory = $true)]
    [string]$JenkinsUrl,

    [Parameter(Mandatory = $true)]
    [string]$JobName,

    [Parameter(Mandatory = $true)]
    [string]$User,

    [Parameter(Mandatory = $true)]
    [string]$Token
)

$ErrorActionPreference = "Stop"

try {
    Write-Host "=== Getting Jenkins crumb ==="

    $authHeader = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${User}:${Token}"))
    $crumbJson = Invoke-RestMethod -Uri "$JenkinsUrl/crumbIssuer/api/json" `
                                   -Headers @{ Authorization = $authHeader } `
                                   -Method GET

    $crumbField = $crumbJson.crumbRequestField
    $crumbValue = $crumbJson.crumb
    Write-Host "Crumb acquired: $crumbField = $crumbValue"

    Write-Host "=== Triggering Jenkins Job: $JobName ==="

    $headers = @{
        Authorization = $authHeader
        "$crumbField" = $crumbValue
    }

    Invoke-RestMethod -Uri "$JenkinsUrl/job/$JobName/build" `
                      -Headers $headers `
                      -Method POST

    Write-Host "=== Jenkins job triggered successfully ==="
}
catch {
    Write-Host "Failed to trigger Jenkins job. Error:" $_.Exception.Message -ForegroundColor Red
    exit 1
}
