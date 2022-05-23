Set-PSDebug -Trace 1

$external_docs =
@(
    @("https://github.com/unoplatform/uno.wasm.bootstrap", "uno.wasm.bootstrap", "3e7cc247cb2851ef9d8293071ed54433e20bfac2"),
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "30b865ecbe55fdfcd9a8068503b7eebb24366523"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "7c31073584d54d306c4001d0905699d47473890a"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "479b01d10240e2c506912c4af46197c1497c9971"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "a6d2efa69e24e8280c38300b5c1b7a8f2033f9f9"),
    @("https://github.com/unoplatform/figma-docs", "figma-docs", "9bf27b6d629c1b8f27a8350010eebcaf39ea8baa"),
    @("https://github.com/unoplatform/uno.extensions", "uno.extensions", "87c21e1d94e05a1adb7712d601aa47ee4da38f3c")
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
