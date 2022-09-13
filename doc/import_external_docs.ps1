Set-PSDebug -Trace 1

$external_docs =
@(
    @("https://github.com/unoplatform/uno.wasm.bootstrap", "uno.wasm.bootstrap", "fbc462e7c6af3d83a989b957798a30cd9ba458ac"),
    @("https://github.com/unoplatform/uno.themes", "uno.themes", "2b197004df3dbf8b49cec431edd7da7f55b469db"),
    @("https://github.com/unoplatform/uno.toolkit.ui", "uno.toolkit.ui", "7ecfbffb13543c5ce83e44df8a9ab655ea512055"),
    @("https://github.com/unoplatform/uno.check", "uno.check", "34b1a60f5c1c51604b47362781969dde46979fd5"),
    @("https://github.com/unoplatform/uno.xamlmerge.task", "uno.xamlmerge.task", "a6d2efa69e24e8280c38300b5c1b7a8f2033f9f9"),
    @("https://github.com/unoplatform/figma-docs", "figma-docs", "1c39985a3823bcf40a82683422086342e249a00b"),
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
