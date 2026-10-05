#Requires -Version 7
#
# Refreshes src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml from a microsoft-ui-xaml tag.
# The file must stay byte-identical to the WinUI source; Uno-specific changes go in UnoGenericOverlay.xaml.
#
# Usage: .\Update-WinUIGenericXaml.ps1 -Tag winui3/release/2.5.1 -WinUIRepoPath D:\Work\microsoft-ui-xaml2
#
param(
    [Parameter(Mandatory = $true)] [string] $Tag,
    [Parameter(Mandatory = $true)] [string] $WinUIRepoPath
)

$ErrorActionPreference = 'Stop'

$sourcePath = 'src/dxaml/xcp/dxaml/themes/generic.xaml'
$targetDir = Join-Path $PSScriptRoot '..\src\Uno.UI\UI\Xaml\Style\Generic' -Resolve
$targetFile = Join-Path $targetDir 'Generic.xaml'
$provenanceFile = Join-Path $targetDir 'Generic.xaml.provenance.json'

$commit = (git -C $WinUIRepoPath rev-parse "$Tag^{commit}").Trim()
if ($LASTEXITCODE -ne 0) { throw "Cannot resolve $Tag in $WinUIRepoPath" }
$blob = (git -C $WinUIRepoPath rev-parse "${Tag}:${sourcePath}").Trim()
if ($LASTEXITCODE -ne 0) { throw "Cannot resolve $sourcePath at $Tag in $WinUIRepoPath" }

# Redirect through git itself so PowerShell never re-encodes the bytes.
$tempFile = [System.IO.Path]::GetTempFileName()
try {
    $gitDir = (Resolve-Path $WinUIRepoPath).Path
    $p = Start-Process git -ArgumentList @('-C', "`"$gitDir`"", 'cat-file', 'blob', $blob) `
        -RedirectStandardOutput $tempFile -NoNewWindow -Wait -PassThru
    if ($p.ExitCode -ne 0) { throw "git cat-file failed" }
    Copy-Item $tempFile $targetFile -Force
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

$sha256 = (Get-FileHash $targetFile -Algorithm SHA256).Hash.ToLowerInvariant()

$provenance = [ordered]@{
    repo       = 'https://github.com/microsoft/microsoft-ui-xaml'
    tag        = $Tag
    commit     = $commit
    path       = $sourcePath
    blobSha    = $blob
    sha256     = $sha256
    importedOn = (Get-Date -Format 'yyyy-MM-dd')
}
($provenance | ConvertTo-Json) + "`n" | Set-Content $provenanceFile -Encoding utf8NoBOM -NoNewline

Write-Host "Generic.xaml updated to $Tag ($commit), sha256 $sha256"
