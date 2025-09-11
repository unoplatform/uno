<#
.SYNOPSIS
    Concatenate all .md files under a folder (recursively), rewrite DocFX xrefs,
    and optionally prepend a header file.

.EXAMPLE
    .\generate-llms-full.ps1 -InputFolder .\docs -OutputFile combined.md `
                   -Llmstxt .\header.md
#>

param(
    [Parameter(Mandatory)] [string] $InputFolder,
    [string] $OutputFile = 'combined.md',
    [string] $Llmstxt                # optional header file to place first
)

function Get-RelativePath ($Parent, $Child) {
    $parentUri = [Uri]((Resolve-Path $Parent).Path + [IO.Path]::DirectorySeparatorChar)
    $childUri  = [Uri](Resolve-Path $Child).Path
    $parentUri.MakeRelativeUri($childUri).OriginalString -replace '/', [IO.Path]::DirectorySeparatorChar
}

# ── Regexes ─────────────────────────────────────────────────────────────────────
$yamlRx  = '(?ms)^---\s*.*?^---\s*'                 # YAML front-matter
$uidLine = '^\s*uid:\s*(.+)$'                       # uid: XYZ
$inlineX = '<xref:([\w\.\-]+)(?:#[\w\.\-]+)?>'      # <xref:Id#frag>
$linkX   = '\]\(xref:([\w\.\-]+)(?:#[\w\.\-]+)?\)'  # [txt](xref:Id#frag)

$parts = [System.Collections.Generic.List[string]]::new()

# ── 1) Optional header file ─────────────────────────────────────────────────────
$headerResolved = $null
if ($Llmstxt) {
    if (-not (Test-Path $Llmstxt)) { throw "Header file '$Llmstxt' not found." }
    $headerResolved = (Resolve-Path $Llmstxt).Path
    $headerText = Get-Content $headerResolved -Raw
    $parts.Add($headerText.TrimEnd() + "`n`n")
}

# ── 2) Process all markdown files ───────────────────────────────────────────────
Get-ChildItem $InputFolder -Recurse -Filter '*.md' |
    Where-Object {
        $_.FullName -ne $headerResolved -and
        $_.FullName -notmatch '[\\/]\.github[\\/]'
    } |  # skip header if in tree and skip .github folder
    Sort-Object FullName |
    ForEach-Object {
        $text = Get-Content $_.FullName -Raw

        # uid or fallback to filename (without extension)
        $uid = if ($text -match '(?m)' + $uidLine) { $Matches[1].Trim() } else { $_.BaseName }

        # relative path for comment
        $relPath = Get-RelativePath -Parent $InputFolder -Child $_.FullName

        # strip YAML
        $text = [regex]::Replace($text, $yamlRx, '')

        # rewrite xrefs → in-file anchors
        $text = $text -replace $inlineX, '[${1}](#${1})'
        $text = $text -replace $linkX,   '](#${1})'

        # section header + source comment
        $parts.Add("<!-- Source: $relPath -->`n## $uid`n")
        $parts.Add($text.TrimEnd() + "`n`n")
    }

# ── 3) Write final document ─────────────────────────────────────────────────────
$parts | Set-Content -NoNewline $OutputFile
Write-Host "Done → $OutputFile"
