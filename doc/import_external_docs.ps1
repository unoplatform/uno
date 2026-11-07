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
    "uno.wasm.bootstrap" = @{ ref="c1d7eb6cd8d569d7db07030f5b5c5dba0389ffa1" }  #latest main commit
    "uno.themes"         = @{ ref="f770bb03f0550512768a089db57891c0bf6ae333" }  #latest master commit
    "uno.toolkit.ui"     = @{ ref="b256fb2568360e8c31f78acd14116957ac30956f" }  #latest main commit
    "uno.check"          = @{ ref="547612c893fedfaaacf0dc0b4ce951d48e28629b" }  #latest main commit
    "uno.xamlmerge.task" = @{ ref="081dcfa44b5ce24ac0948675e5ee6b781e2107bc" }  #latest main commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" }  #latest main commit
    "uno.resizetizer"    = @{ ref="cd95e3592296259e4417af48761f86e34c7c1e59" }  #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" }  #latest master commit
    "uno.extensions"     = @{ ref="fc87e68de121f25316a48f7ac5670af041362261" }  #latest main commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" }  #latest master commit
    "uno.samples"        = @{ ref="cace5177e64749bf39409df9e24a2f5f04bfdc78" }  #latest master commit
    "uno.chefs"          = @{ ref="d3591606dae2e5f4892ab4872a8dd94c2fc9e211" }  #latest main commit
    "hd-docs"            = @{ ref="2b17758ce3665f347e2df1bc835dab48986bae06"; dest="studio/Hot Design" } #latest main commit
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
