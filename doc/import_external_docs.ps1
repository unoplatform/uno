param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "9b559fc9a8b5e939fd99fa119a243e760ae3770d" #latest main commit
    "uno.themes"         = "98eb4b4aebaf19ad1ce1c67220d7a7beaa772959" #latest release branch commit
    "uno.toolkit.ui"     = "c7af29b7f352fbed1c83bd119e9d10d14736cf26" #latest release branch commit
    "uno.check"          = "4a7dd7290daf0aabfbb8efabcd2b067898b7f45e" #latest main commit
    "uno.xamlmerge.task" = "21f02c98702b875a9942047ca042e41810b6fe56" #latest main commit
    "figma-docs"         = "08aea8467414390673e8f4a917e29bf07ef96680" #latest main commit
    "uno.resizetizer"    = "55c9bac2b19c0bf39f8794684396db3cc9a4e9d0" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "71b3bcc76bc599996dfa3329d4bb311a1aeba330" #latest release branch commit
    "workshops"          = "bb4a34a533b1cd7617cc7d1a3911dbd4fb23c70a" #latest master commit
    "uno.samples"        = "4599f4e09d12cfef46f5f70e65347e93600af499" #latest master commit
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
