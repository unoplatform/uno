param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "a422451cc4c12377ec8a93e2a0f2f2ae1085a9e3" #latest release/stable/9.0 branch commit
    "uno.themes"         = "8923df90fa1336117bafabf1498c3ddab2030d05" #latest release/stable/5.5 branch commit
    "uno.toolkit.ui"     = "8c4630ef6a7292e5f44ee0cdb6eb6a80e0adf03d" #latest release/stable/7.0 branch commit
    "uno.check"          = "650e0310bb6b44bdfc647f9d39197be587b2dcb5" #latest release/stable/1.31 branch commit
    "uno.xamlmerge.task" = "377ce2d9fdeab0d4f0b94a61e008731a40b10220" #latest release/stable/1.33 branch commit
    "figma-docs"         = "842a2792282b88586a337381b2b3786e779973b4" #latest main commit
    "uno.resizetizer"    = "559897ecfa8b509ec67746bd51d35f44eed56160" #latest main commit
    "uno.uitest"         = "a375f3dc1898e3d34bb03477bb45d2fd86fea60a" #latest master commit
    "uno.extensions"     = "e3021a6b082a2bb1871190ad6a1429a1c06e3ab4" #latest release/stable/5.3 branch commit
    "workshops"          = "e3c2a11a588b184d8cd3a6f88813e5615cca891d" #latest master commit
    "uno.samples"        = "8261367edffd836512f9b37fbb526f620d0a381e" #latest master commit
    "uno.chefs"          = "25da718f7aba5719b7053b9c53e1f4c8abce2193" #latest main commit
}

$uno_git_url = "https://github.com/unoplatform/"

if($branches -ne $null)
{
    foreach ($repo in $branches.keys)
    {
        $branch = $branches[$repo]

        $external_docs[$repo] = $branch
    }
}

echo "Current setup:"
$external_docs

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        popd
        popd

        Set-PSDebug -Off

        throw "Exit code must be zero."
    }
}

if (-Not (Test-Path articles\external))
{
    mkdir articles\external -ErrorAction Continue
}

pushd articles\external

# ensure long paths are supported on Windows
git config --global core.longpaths true

$detachedHeadConfig = git config --get advice.detachedHead
git config advice.detachedHead false

# Heads - Release
foreach ($repoPath in $external_docs.keys)
{
    $repoUrl = "$uno_git_url$repoPath"
    $repoBranch = $external_docs[$repoPath]
        
    if (-Not (Test-Path $repoPath))
    {        
        echo "Cloning $repoPath ($repoUrl@$repoBranch)..."
        git clone $repoUrl $repoPath
        Assert-ExitCodeIsZero
    }

    pushd $repoPath

    echo "Checking out $repoUrl@$repoBranch..."
    git fetch
    git checkout --force $repoBranch
    Assert-ExitCodeIsZero

    # if not detached
    if ((git symbolic-ref -q HEAD) -ne $null)
    {
        echo "Resetting to $repoUrl@$repoBranch..."
        git reset --hard origin/$repoBranch
        Assert-ExitCodeIsZero
    }

    popd
}

git config advice.detachedHead $detachedHeadConfig

popd

Set-PSDebug -Off
