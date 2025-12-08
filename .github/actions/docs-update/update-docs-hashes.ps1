#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates external documentation repository commit hashes to their latest default branch commits.
    
.DESCRIPTION
    This script reads the list of external documentation repositories from import_external_docs.ps1
    and updates their commit hashes to the latest commit from their default branches.
    
.NOTES
    This script maintains a single source of truth for the repository list by reading it from
    import_external_docs.ps1, avoiding duplication.
#>

param()

$ErrorActionPreference = 'Stop'

Write-Host "Updating external docs commit hashes..." -ForegroundColor Green

# Configuration
$scriptPath = "doc/import_external_docs.ps1"
$organization = "unoplatform"

# GitHub API headers
$apiHeaders = @{
    "Accept" = "application/vnd.github.v3+json"
    "User-Agent" = "Uno-Docs-Updater"
}

if (-Not (Test-Path $scriptPath)) {
    Write-Error "Script not found: $scriptPath"
    exit 1
}

# Read the import_external_docs.ps1 script to extract the repository list
$scriptContent = Get-Content $scriptPath -Raw

# Parse the $external_docs hashtable from the script to get the list of repos
# Extract repository names from lines that match the pattern: "repo-name" = @{ ... }
$repoPattern = '"([^"]+)"\s*=\s*@\{'
$repos = [regex]::Matches($scriptContent, $repoPattern) | ForEach-Object {
    $_.Groups[1].Value
}

if ($repos.Count -eq 0) {
    Write-Error "Could not parse repository list from $scriptPath"
    exit 1
}

Write-Host "Found $($repos.Count) repositories to update" -ForegroundColor Cyan

# Read the current script content for updating
$content = Get-Content $scriptPath -Raw
$updatedCount = 0

foreach ($repo in $repos) {
    Write-Host "Processing $repo..." -ForegroundColor Cyan
    
    try {
        # First, get the repository info to find the default branch
        $repoInfoUrl = "https://api.github.com/repos/$organization/$repo"
        $repoInfo = Invoke-RestMethod -Uri $repoInfoUrl -Headers $apiHeaders
        
        $defaultBranch = $repoInfo.default_branch
        Write-Host "  Default branch: $defaultBranch" -ForegroundColor Gray
        
        # Get the latest commit hash from the default branch
        $commitUrl = "https://api.github.com/repos/$organization/$repo/commits/$defaultBranch"
        $commitInfo = Invoke-RestMethod -Uri $commitUrl -Headers $apiHeaders
        
        $latestHash = $commitInfo.sha
        Write-Host "  Latest commit: $latestHash" -ForegroundColor Gray
        
        # Update the hash in the script using regex
        # Pattern explanation:
        # - (`"$repo`"\s*=\s*@\{\s*ref\s*=\s*`") : Matches "repo-name" = @{ ref="
        # - [a-f0-9]{40} : Matches the 40-character commit hash
        # - (`"\s*}[^#]*) : Matches closing quote, brace, and any whitespace before comment
        # - (#.*)? : Optionally matches the comment at the end
        $pattern = "(`"$repo`"\s*=\s*@\{\s*ref\s*=\s*`")[a-f0-9]{40}(`"\s*}[^#]*)(#.*)?$"
        $comment = "latest $defaultBranch commit"
        $replacement = "`${1}$latestHash`${2}#$comment"
        
        $newContent = $content -replace $pattern, $replacement
        
        if ($newContent -ne $content) {
            $content = $newContent
            $updatedCount++
            Write-Host "  ✓ Updated $repo to $latestHash" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ No change for $repo (already up to date or pattern not matched)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Warning "Failed to process $repo : $_"
    }
}

# Write the updated content back to the file
if ($updatedCount -gt 0) {
    Set-Content -Path $scriptPath -Value $content -NoNewline
    Write-Host "`n✓ Updated $updatedCount commit hash(es) in $scriptPath" -ForegroundColor Green
} else {
    Write-Host "`n⚠ No updates were made" -ForegroundColor Yellow
}

# Show git diff for verification
Write-Host "`nChanges made:" -ForegroundColor Cyan
git diff $scriptPath
