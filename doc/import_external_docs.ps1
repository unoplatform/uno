param(
  [Parameter(ValueFromPipelineByPropertyName = $true)]
  $branches = $null,
  [switch]$ListRepos
)

Set-PSDebug -Trace 1

# --- CONFIGURATION ----------------------------------------------------------
# Each entry: repo name -> @{ ref = '<commit|branch>'; dest = '<sub-folder>'?; track = 'release'? }
#
#   track = 'release' : this repo is versioned in lockstep with Uno releases. When the updater runs
#                       against a release/stable/* branch, pin it to the repo's HIGHEST
#                       release/stable/* branch (falling back to the default branch if none exist).
#   track omitted     : the repo's docs are not versioned per Uno release; ALWAYS pin to its default
#                       branch (main/master), in both default (master) and release modes.
#
# Only annotate 'release' repos here. Everything else must keep tracking its default branch — a repo
# like uno.uitest / uno.resizetizer has release/stable/* branches of its own that are unrelated to
# the Uno release line, so following them would pin stale/wrong (even years-old) docs.
$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = @{ ref="1e094106842f6e7f43075f06d2a0354077da3a0f"; track="release" }  #latest main commit
    "uno.themes"         = @{ ref="09187371ab8a3c3c90739b3ac4c336b49e5edff4"; track="release" }  #latest master commit
    "uno.toolkit.ui"     = @{ ref="030026c023aa8ea3e82ec9f4dab264bd50086440"; track="release" }  #latest main commit
    "uno.check"          = @{ ref="1eb2b63f57511ba78b40a82f7f1c245bc9c9aaf1"; track="release" }  #latest main commit
    "uno.xamlmerge.task" = @{ ref="081dcfa44b5ce24ac0948675e5ee6b781e2107bc"; track="release" }  #latest main commit
    "figma-docs"         = @{ ref="842a2792282b88586a337381b2b3786e779973b4" }  #latest main commit
    "uno.resizetizer"    = @{ ref="e422ad9f26cf21ed02c339e717e0dd0189bb566e" }  #latest main commit
    "uno.uitest"         = @{ ref="94d027295b779e28064aebf99aeaee2b393ad558" }  #latest master commit
    "uno.extensions"     = @{ ref="f3a5f9de7a2a5dccbf20a295270940dcca154ad8"; track="release" }  #latest main commit
    "workshops"          = @{ ref="3515c29e03dea36cf2206d797d1bf9f8620370e3" }  #latest master commit
    "uno.samples"        = @{ ref="2c8e58853a3795fe4250d39787225e55db18499d" }  #latest master commit
    "uno.chefs"          = @{ ref="4bbc0569dc7ac0ddefe8b0de4be31beb3706a90b" }  #latest main commit
    "hd-docs"            = @{ ref="c4c2f891544ae17b846b8fd24a90edd974ae6938"; dest="studio/Hot Design" } #latest main commit
    "studio-docs"        = @{ ref="3b86af4dac3e7a21c41b619960d14848ce95e2fd" }  #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

# --- END OF CONFIGURATION --------------------------------------------------

# If -ListRepos flag is set, output the repositories (name + tracking policy) and exit.
# Emits one object per repo so consumers (the updater) know whether a repo follows release
# branches or always its default branch. Track defaults to 'default' when not annotated.
if ($ListRepos) {
    Set-PSDebug -Off
    $external_docs.Keys | ForEach-Object {
        $cfg = $external_docs[$_]
        $track = if ($cfg.ContainsKey('track') -and $cfg.track) { $cfg.track } else { 'default' }
        [pscustomobject]@{ Name = $_; Track = $track }
    }
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
