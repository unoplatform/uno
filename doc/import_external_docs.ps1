param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "abcb066159e3a089019032fbd4befda836336296" #latest main commit
    "uno.themes"         = "a61ecde0885adc8ca616e9de456a58f4f974a9da" #latest release branch commit
    "uno.toolkit.ui"     = "9bb4e48504983634a96eb8314bfcc982d16a90fb" #latest release branch commit
    "uno.check"          = "27a06fd34c4744d07d48249daae1bcdf21a8a005" #latest main commit
    "uno.xamlmerge.task" = "21f02c98702b875a9942047ca042e41810b6fe56" #latest main commit
    "figma-docs"         = "842a2792282b88586a337381b2b3786e779973b4" #latest main commit
    "uno.resizetizer"    = "dce3eabef076e37b38fe058b427eb84f1d19b881" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "9dd31137f4aeed1f004613387f4edf2b5f08e609" #latest release branch commit
    "workshops"          = "df05737f9e3088a4e60179f6fa2cd2fc6866d2c7" #latest master commit
    "uno.samples"        = "ef30b74b84bd470521ae1383b49ced4afcfeab1d" #latest master commit
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
