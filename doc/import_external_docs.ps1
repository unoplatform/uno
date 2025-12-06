param(
    [Parameter(ValueFromPipelineByPropertyName = $true)]$branches = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string]$contributor_git_url = $null,
    [Parameter(ValueFromPipelineByPropertyName = $true)][string[]]$forks_to_import = $null
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="3b707687f10ffd3b93dbeff3a867c23d250ae3ca" } #latest release/stable/10.0 branch commit
    "uno.themes"         = @{ ref="25d5ed1b611e433912fffbd7be68c69af737e5dd" } #latest release/stable/6.0 branch commit
    "uno.toolkit.ui"     = @{ ref="8ff0d5ca9fb373cf4e460debd0129682dd3869cd" } #latest release/stable/8.3 branch commit
    "uno.check"          = @{ ref="0fabc47c022c7eb86b1c4760441780ff3638b06e" } #latest release/stable/1.33 branch commit
    "uno.xamlmerge.task" = @{ ref="377ce2d9fdeab0d4f0b94a61e008731a40b10220" } #latest release/stable/1.33 branch commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" } #latest main commit
    "uno.resizetizer"    = @{ ref="e051343230e86d2e4ebc5e1840e530dd4fc9a4da" } #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" } #latest master commit
    "uno.extensions"     = @{ ref="9761c4acdf766fef6f6e1ba76d73795a9d263743" } #latest release/stable/7.0 branch commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" } #latest master commit
    "uno.samples"        = @{ ref="8098a452951c9f73cbcf8d0ac1348f029820e53a" } #latest master commit
    "uno.chefs"          = @{ ref="16a62fdd6950a2f89fe0f94dc5cc67207cab9c4a" } #latest main commit
    "hd-docs"            = @{ ref="ded00dc100ae7dcba4a78fd32d393a58c1d1f23e"; dest="studio/Hot Design" } #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# Validation ordering note: contributor URL normalization must occur BEFORE requiring it when forks are provided.

# Normalize blank contributor URL to null (CI may pass empty string)
if ([string]::IsNullOrWhiteSpace($contributor_git_url)) { $contributor_git_url = $null }

# If forks are specified and non-empty, contributor_git_url must be present (non-null after normalization)
if ($forks_to_import -ne $null -and $forks_to_import.Count -gt 0 -and $contributor_git_url -eq $null) {
    throw "Parameter 'forks_to_import' requires 'contributor_git_url' to be specified."
}

# Validate fork names against configured repositories (only if array provided)
if ($forks_to_import -ne $null) {
    $configuredRepos = $external_docs.Keys | ForEach-Object { $_.ToLower() }
    $invalidForks = $forks_to_import |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Where-Object { $configuredRepos -notcontains $_.ToLower() }
    if ($invalidForks.Count -gt 0) {
        throw "The following repository names in forks_to_import are not configured in external_docs: $($invalidForks -join ', ')"
    }
}

# If a contributor git URL is provided, validate only when forks are specified; never fail CI when unused.
if ($contributor_git_url -ne $null) {
    if ($forks_to_import -eq $null -or $forks_to_import.Count -eq 0) {
        Write-Warning 'Parameter ''contributor_git_url'' was provided but ''forks_to_import'' is null or empty. The contributor URL will not be used.'
    }
    else {
        if ($contributor_git_url -notmatch '^https?://') {
            Write-Warning "Parameter 'contributor_git_url' is not a valid HTTP/HTTPS URL. Ignoring contributor URL and falling back to Uno repos."
            $contributor_git_url = $null
        }
        elseif (-not $contributor_git_url.EndsWith('/')) {
            $contributor_git_url += '/'
        }
    }
}

# If branches are passed, use them to override the default ones (ref, but not dest)
if ($branches -ne $null) {
    foreach ($repo in $branches.Keys) {
        $branch = $branches[$repo]
        if ($external_docs[$repo]) {
            $external_docs[$repo].ref = $branch
        }
        else {
            $external_docs[$repo] = @{ ref = $branch }     # default dest = external
        }
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Get-TargetRoot([string]$repo) {
    $cfg = $external_docs[$repo]
    $dest = if ($cfg.ContainsKey('dest') -and $cfg.dest) { $cfg.dest } else {
        Join-Path 'external' $repo
    }
    return Join-Path 'articles' $dest
}

function Assert-ExitCodeIsZero() {
    if ($LASTEXITCODE -ne 0) {
        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.Keys) {
    $repoCfg = $external_docs[$repoPath]
    $repoRef = $repoCfg.ref
    $targetRoot = Get-TargetRoot $repoPath
    $fullPath = $targetRoot

    if ($contributor_git_url -ne $null -and $forks_to_import -ne $null -and ($forks_to_import -contains $repoPath)) {
        # Fork override: use contributor-provided git URL base
        $repoUrl = "$contributor_git_url$repoPath"
    }
    else {
        $repoUrl = "$uno_git_url$repoPath"
    }

    Write-Output "Importing $repoPath from $repoUrl..."

    if (-Not (Test-Path $fullPath)) {

        Write-Host "Cloning $repoPath ($repoUrl@$repoRef) into $targetRoot..." -ForegroundColor Black -BackgroundColor Blue
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $fullPath) | Out-Null
        git clone --filter=blob:none --no-tags $repoUrl $fullPath
        Assert-ExitCodeIsZero
    }
    else {
        Write-Host "Skipping clone of $repoPath ($repoUrl@$repoRef) into $targetRoot (already exists)."-ForegroundColor Black -BackgroundColor DarkYellow
    }
    pushd $fullPath

    try {
        Write-Host "Checking out $repoUrl@$repoRef..." -ForegroundColor Black -BackgroundColor Blue
        git fetch --filter=blob:none origin $repoRef # fetch the latest commit for the specified ref
        Assert-ExitCodeIsZero
        git checkout --detach FETCH_HEAD # detach the HEAD to avoid issues with git status
        Assert-ExitCodeIsZero

        # if not detached
        if ((git symbolic-ref -q HEAD) -ne $null) {
            echo "Resetting to $repoUrl@$repoRef..."
            git reset --hard origin/$repoRef
            Assert-ExitCodeIsZero
        }
    }
    finally {
        popd
    }
}

git config advice.detachedHead $detachedHeadConfig

Set-PSDebug -Off
