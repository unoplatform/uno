param(
    [string]$Project,
    [string]$DefinitionId,
    [string]$AccessToken
)

$headers = @{ Authorization = "Bearer $AccessToken" }
$url = "$($env:SYSTEM_COLLECTIONURI)$Project/_apis/build/builds?definitions=$DefinitionId&branchName=refs/heads/master&statusFilter=completed&queryOrder=finishTimeDescending&api-version=6.0"

try {
    $response = Invoke-RestMethod -Uri $url -Headers $headers -Method Get
    $builds = $response.value

    if ($builds.Count -eq 0) {
        Write-Host "No builds found on master."
        exit 0
    }

    $latestBuild = $builds[0]
    if ($latestBuild.result -eq "succeeded" -or $latestBuild.result -eq "partiallySucceeded") {
        Write-Host "Latest master build $($latestBuild.buildNumber) succeeded."
        exit 0
    }

    Write-Host "Latest master build $($latestBuild.buildNumber) failed."

    # Find last success
    $lastSuccess = $builds | Where-Object { $_.result -eq "succeeded" -or $_.result -eq "partiallySucceeded" } | Select-Object -First 1

    if ($null -eq $lastSuccess) {
        Write-Host "No successful builds found in history."
        exit 0
    }

    $lastSuccessTime = [DateTime]$lastSuccess.finishTime
    $now = [DateTime]::UtcNow
    $diff = $now - $lastSuccessTime

    Write-Host "Last successful build was $($lastSuccess.buildNumber) at $lastSuccessTime ($($diff.TotalHours) hours ago)."

    if ($diff.TotalHours -gt 24) {
        Write-Error "Master has been failing for more than 24 hours. Last success was at $lastSuccessTime. Blocking PR merge."
        exit 1
    } else {
        Write-Host "Master is failing, but within the 24h grace period."
        exit 0
    }
}
catch {
    Write-Error "Failed to check master status: $_"
    exit 1
}
