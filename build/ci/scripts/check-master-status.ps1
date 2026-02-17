param(
	[string]$Project,
	[string]$DefinitionId,
	[string]$GitHubRepo = "unoplatform/uno",
	[switch]$ForceFailure
)

$url = "$($env:SYSTEM_COLLECTIONURI)$Project/_apis/build/builds?definitions=$DefinitionId&branchName=refs/heads/master&statusFilter=completed&queryOrder=finishTimeDescending&api-version=6.0"

try {
	$response = Invoke-RestMethod -Uri $url -Method Get
	$builds = $response.value

	if (-not $builds -or $builds.Count -eq 0) {
		Write-Host "No builds found on master."
		exit 0
	}

	function Test-BuildSucceeded($build) {
		if ($build.result -eq "succeeded" -or $build.result -eq "partiallySucceeded") {
			return $true
		}

		$publishIssues = $build.issues |
			Where-Object { $_.type -eq "error" -and $_.message -match "Publish to nuget\.org" }

		if ($publishIssues.Count -gt 0) {
			Write-Host "Build $($build.buildNumber) failed only due to 'Publish to nuget.org'. Treating as succeeded."
			return $true
		}

		return $false
	}

	$latestBuild = $builds[0]
	if (-not $ForceFailure -and (Test-BuildSucceeded $latestBuild)) {
		Write-Host "Latest master build $($latestBuild.buildNumber) succeeded."
		exit 0
	}

	if ($ForceFailure) {
		Write-Host "ForceFailure is set. Simulating a master failure scenario."
	}

	Write-Host "Latest master build $($latestBuild.buildNumber) failed."

	$lastSuccess = $builds |
		Where-Object { Test-BuildSucceeded $_ } |
		Select-Object -First 1

	if ($null -eq $lastSuccess) {
		Write-Host "No successful builds found in history."
		exit 0
	}

	$lastSuccessTime = [DateTime]$lastSuccess.finishTime
	$now = [DateTime]::UtcNow
	$diff = $now - $lastSuccessTime

	Write-Host "Last successful build was $($lastSuccess.buildNumber) at $lastSuccessTime ($([Math]::Round($diff.TotalHours, 2)) hours ago)."

	if ($ForceFailure -or $diff.TotalHours -gt 24) {
		Write-Host "Master has been failing for more than 24 hours. Last success was at $lastSuccessTime."

		# Find the first failing build after the last successful one
		$lastSuccessIndex = [Array]::IndexOf($builds, $lastSuccess)
		$firstFailure = $builds[$lastSuccessIndex - 1]

		$lastSuccessCommit = $lastSuccess.sourceVersion
		$firstFailureCommit = $firstFailure.sourceVersion
		$lastSuccessCommitShort = $lastSuccessCommit.Substring(0, 7)
		$firstFailureCommitShort = $firstFailureCommit.Substring(0, 7)
		$hoursDown = [Math]::Round($diff.TotalHours, 1)

		Write-Host "Last successful commit: $lastSuccessCommit (build $($lastSuccess.buildNumber))"
		Write-Host "First failing commit: $firstFailureCommit (build $($firstFailure.buildNumber))"

		# Check for existing open issue to avoid duplicates
		$searchLabel = "master-ci-failure"
		$existingIssues = gh issue list --repo $GitHubRepo --label $searchLabel --state open --json body --jq ".[].body" 2>&1

		$alreadyReported = $false
		if ($LASTEXITCODE -eq 0 -and $existingIssues) {
			foreach ($body in $existingIssues) {
				if ($body -match $firstFailureCommitShort) {
					$alreadyReported = $true
					break
				}
			}
		}

		if ($alreadyReported) {
			Write-Host "An open issue already exists for this failure window (first failing commit $firstFailureCommitShort). Skipping issue creation."
		}
		else {
			$pipelineUrl = "$($env:SYSTEM_COLLECTIONURI)$Project/_build?definitionId=$DefinitionId&branchName=refs%2Fheads%2Fmaster"

			$issueBody = @"
## Master CI has been failing for more than 24 hours

| | Build | Commit | Time (UTC) |
|---|---|---|---|
| **Last success** | $($lastSuccess.buildNumber) | [$lastSuccessCommitShort](https://github.com/$GitHubRepo/commit/$lastSuccessCommit) | $($lastSuccess.finishTime) |
| **First failure** | $($firstFailure.buildNumber) | [$firstFailureCommitShort](https://github.com/$GitHubRepo/commit/$firstFailureCommit) | $($firstFailure.finishTime) |

**Hours failing:** $hoursDown hours

[View pipeline runs]($pipelineUrl)
"@

			Write-Host "Creating GitHub issue in $GitHubRepo..."
			gh issue create `
				--repo $GitHubRepo `
				--title "Master CI has been failing for more than 24 hours" `
				--body $issueBody `
				--label "master-ci-failure"

			if ($LASTEXITCODE -eq 0) {
				Write-Host "GitHub issue created successfully."
			}
			else {
				Write-Warning "Failed to create GitHub issue. gh exit code: $LASTEXITCODE"
			}
		}
	}
	else {
		Write-Host "Master is failing, but within the 24h grace period."
	}

	exit 0
}
catch {
	Write-Error "Failed to check master status: $($_.Exception.Message)"
	# Do not block the PR even if the script fails
	exit 0
}
