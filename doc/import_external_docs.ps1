param(
    [Parameter(ValueFromPipeLineByPropertyName = $true)]$branches = $null,
    [Parameter(ValueFromPipeLineByPropertyName = $true)][string]$contributor_git_url = $null,
    [Parameter(ValueFromPipeLineByPropertyName = $true)][string[]]$forks_to_import = $null
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="a422451cc4c12377ec8a93e2a0f2f2ae1085a9e3" } #latest release/stable/9.0 branch commit
    "uno.themes"         = @{ ref="8923df90fa1336117bafabf1498c3ddab2030d05" } #latest release/stable/5.5 branch commit
    "uno.toolkit.ui"     = @{ ref="8c4630ef6a7292e5f44ee0cdb6eb6a80e0adf03d" } #latest release/stable/7.0 branch commit
    "uno.check"          = @{ ref="650e0310bb6b44bdfc647f9d39197be587b2dcb5" } #latest release/stable/1.31 branch commit
    "uno.xamlmerge.task" = @{ ref="377ce2d9fdeab0d4f0b94a61e008731a40b10220" } #latest release/stable/1.33 branch commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" } #latest main commit
    "uno.resizetizer"    = @{ ref="559897ecfa8b509ec67746bd51d35f44eed56160" } #latest main commit
    "uno.uitest"         = @{ ref="a375f3dc1898e3d34bb03477bb45d2fd86fea60a" } #latest master commit
    "uno.extensions"     = @{ ref="e4439ed1c4bb616852d8b57508c8766e5257e022" } #latest release/stable/6.0 branch commit
    "workshops"          = @{ ref="e3c2a11a588b184d8cd3a6f88813e5615cca891d" } #latest master commit
    "uno.samples"        = @{ ref="8261367edffd836512f9b37fbb526f620d0a381e" } #latest master commit
    "uno.chefs"          = @{ ref="25da718f7aba5719b7053b9c53e1f4c8abce2193" } #latest main commit
    "hd-docs"            = @{ ref="683cd1525524c85812e820c100f8c651c788cadb"; dest="studio/Hot Design" } #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# If branches are passed, use them to override the default ones (ref, but not dest)

if ($contributor_git_url -ne $null -and -not ($contributor_git_url -is [string])) {
    throw "The parameter 'contributor_git_url' must be a string or null."
}
if ($forks_to_import -ne $null -and -not ($forks_to_import -is [string[]])) {
    throw "The parameter 'forks_to_import' must be a array of string or null."
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

    if ($forks_to_import -ne $null -and $forks_to_import.Contains("$repoPath")) {
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
