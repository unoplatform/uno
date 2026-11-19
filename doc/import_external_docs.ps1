param(
  [Parameter(ValueFromPipelineByPropertyName = $true)]
  $branches = $null,
  [switch]$ListRepos
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'? }
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="dce9bf4aab0d3c1ab3857cec3bb520f9167d3060" }  #latest main commit
    "uno.themes"         = @{ ref="c2b4d8fca3b08832c773a5916696537d5bf8dd34" }  #latest master commit
    "uno.toolkit.ui"     = @{ ref="b587c5573ffc5f06b32ec1ff284e5a3b7f72a0a0" }  #latest main commit
    "uno.check"          = @{ ref="c8733e9ab63ff6fb7e90a9503c7158d370c18242" }  #latest main commit
    "uno.xamlmerge.task" = @{ ref="081dcfa44b5ce24ac0948675e5ee6b781e2107bc" }  #latest main commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" }  #latest main commit
    "uno.resizetizer"    = @{ ref="3ed33f839c58ee6e9a6e4baeba88b94b6a2e2ab7" }  #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" }  #latest master commit
    "uno.extensions"     = @{ ref="87ccada786d23832cf169f39c8041938e266f6dd" }  #latest main commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" }  #latest master commit
    "uno.samples"        = @{ ref="da93d84026032f3ad07dc41ab5e044c88d232027" }  #latest master commit
    "uno.chefs"          = @{ ref="d3591606dae2e5f4892ab4872a8dd94c2fc9e211" }  #latest main commit
    "hd-docs"            = @{ ref="46b346981684f702d475dc8fb6a5a273a7fc4cc6"; dest="studio/Hot Design" } #latest main commit
    "studio-docs"        = @{ ref="63d500cb865d71b39c375370d9d4db69b0b29815" }  #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# If -ListRepos flag is set, output the repository names and exit
if ($ListRepos) {
    Set-PSDebug -Off
    $external_docs.Keys | ForEach-Object { Write-Output $_ }
    exit 0
}

# If branches are passed, use them to override the default ones (ref, but not dest)
if ($branches) {
    foreach ($repo in $branches.Keys) {
        $branch = $branches[$repo]
        if ($external_docs[$repo]) {
            $external_docs[$repo].ref = $branch
        } else {
            $external_docs[$repo] = @{ ref = $branch }     # default dest = external
        }
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Get-TargetRoot([string]$repo) {
    $cfg  = $external_docs[$repo]
    $dest = if ($cfg.ContainsKey('dest') -and $cfg.dest) { $cfg.dest } else { Join-Path 'external' $repo }
    return Join-Path 'articles' $dest
}

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.Keys)
{
    $repoCfg = $external_docs[$repoPath]
    $repoRef = $repoCfg.ref
    $targetRoot = Get-TargetRoot $repoPath
    $fullPath   = $targetRoot
    $repoUrl = "$uno_git_url$repoPath"
        
    if (-Not (Test-Path $fullPath))
    {        
        Write-Host "Cloning $repoPath ($repoUrl@$repoRef) into $targetRoot..." -ForegroundColor Black -BackgroundColor Blue
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $fullPath) | Out-Null
        git clone --filter=blob:none --no-tags $repoUrl $fullPath
        Assert-ExitCodeIsZero
    }
    else
    {
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
        if ((git symbolic-ref -q HEAD) -ne $null)
        {
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
