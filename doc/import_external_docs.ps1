Set-PSDebug -Trace 1

$external_docs =
@(
    @("https://github.com/unoplatform/uno.wasm.bootstrap", "uno.wasm.bootstrap", "3e7cc247cb2851ef9d8293071ed54433e20bfac2"),
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "d0180c7c14d4b60ec9dab05e20b89da9c175070e"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "f495d77996012331653b13bbfb953e67a61be43c"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "1a2049ae6c232c5e7dafdc2b3f0ca55a7a180d58"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "f6e3343a7ea5ba863438bf7566991664d7600ffa"),
    @("https://github.com/unoplatform/uno.extensions", "uno.extensions", "1142ce0c872d3f422f113cfe0f98c681dfd8b616")
)

$ErrorActionPreference = 'Stop'

function Assert-ExitCodeIsZero()
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "Exit code must be zero."
	}
}

mkdir articles\external -ErrorAction Continue
pushd articles\external

# ensure long paths are supported on Windows
git config --global core.longpaths true

# Heads - Release
for($i = 0; $i -lt $external_docs.Length; $i++)
{
    $repoUrl=$external_docs[$i][0]
    $repoPath=$external_docs[$i][1]
    $repoBranch=$external_docs[$i][2]

    echo "Cloning $repoPath ($repoUrl@$repoBranch)"

    git clone $repoUrl $repoPath
    Assert-ExitCodeIsZero

    pushd $repoPath
    git checkout $repoBranch
    Assert-ExitCodeIsZero
    popd
}

popd
