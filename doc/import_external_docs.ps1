param(
  [Parameter(ValueFromPipeLineByPropertyName = $true)]
  $branches = $null
)

Set-PSDebug -Trace 1

$external_docs = @{
    # use either commit, or branch name to use its latest commit
    "uno.wasm.bootstrap" = "abcb066159e3a089019032fbd4befda836336296" #latest release/stable/8.0 branch commit
    "uno.themes"         = "1da7240824a1bc5ddaf5efc1ed6e22ea5a41abd9" #latest release/stable/5.2 branch commit
    "uno.toolkit.ui"     = "a79e83cb260c17f59be6a843ce90038cb2479a61" #latest release/stable/6.2 branch commit
    "uno.check"          = "27e686d9205654375fd2c7861c3ebe5f2ad69e93" #latest main commit
    "uno.xamlmerge.task" = "74c832124dad9a979f8d6318b6c18be1125467e5" #latest main commit
    "figma-docs"         = "842a2792282b88586a337381b2b3786e779973b4" #latest main commit
    "uno.resizetizer"    = "b17e1d54f4375f79da24ae5eaaf3f738851786eb" #latest main commit
    "uno.uitest"         = "9669fd2783187d06c36dd6a717c1b9f08d1fa29c" #latest master commit
    "uno.extensions"     = "f2871cf4aa93bce96d8a19ede2b1417776589093" #latest release/stable/5.0 branch commit
    "workshops"          = "e3c2a11a588b184d8cd3a6f88813e5615cca891d" #latest master commit
    "uno.samples"        = "cf96230ed902e18335411683cb07321eae125df2" #latest master commit
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
