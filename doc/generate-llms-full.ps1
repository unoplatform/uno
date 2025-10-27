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
        # Skip header file and .github folder
        if ($_.FullName -eq $headerResolved) { return $false }
        if ($_.FullName -match '[\\/]\.github[\\/]') { return $false }

        # Normalize path to forward slashes for easier matching
        $norm = ($_.FullName -replace '\\\\', '/')

        # If not under external/<repo>/..., allow
        $rx = '^(.*/external/[^/]+)(?:/([^/]+))?/?.*$'
        $m = [regex]::Match($norm, $rx)
        if (-not $m.Success) { return $true }

        # Matched external/<repo>; determine the first directory under external/<repo>
        $base = $m.Groups[1].Value
        $rest = $norm.Substring($base.Length).TrimStart('/')
        $firstSegment = if ($rest) { ($rest -split '/')[0] } else { $null }

        if ($firstSegment) {
            # Only consider the first segment a directory if it actually is a directory on disk
            # Build a candidate path and test for being a directory
            $candidate = "$base/$firstSegment"
            $candidateWindows = $candidate -replace '/', [IO.Path]::DirectorySeparatorChar
            if (Test-Path $candidateWindows -PathType Container) {
                $firstParentWindows = $candidateWindows
            } else {
                # If the first segment is not a directory (likely a file), fall back to the repo base
                $firstParentWindows = ($base -replace '/', [IO.Path]::DirectorySeparatorChar)
            }
        } else {
            $firstParentWindows = ($base -replace '/', [IO.Path]::DirectorySeparatorChar)
        }

        # Only include the file if that first parent contains a toc.yml
        return (Test-Path (Join-Path $firstParentWindows 'toc.yml'))
    } |  # skip header if in tree and skip .github folder
    Sort-Object FullName |
    ForEach-Object {
        # Defensive re-check: ensure files under external/<repo>/ are only processed
        # when the first parent contains a toc.yml. This duplicates the Where-Object
        # logic to avoid accidental inclusion from earlier pipeline quirks.
        $norm = ($_.FullName -replace '\\', '/')
        $rx = '^(.*/external/[^/]+)(?:/([^/]+))?/?.*$'
        $m = [regex]::Match($norm, $rx)
        if ($m.Success) {
            $base = $m.Groups[1].Value
            $rest = $norm.Substring($base.Length).TrimStart('/')
            $firstSegment = if ($rest) { ($rest -split '/')[0] } else { $null }
            if ($firstSegment) {
                $candidate = "$base/$firstSegment"
                $candidateWindows = $candidate -replace '/', [IO.Path]::DirectorySeparatorChar
                if (Test-Path $candidateWindows -PathType Container) { $firstParentWindows = $candidateWindows } else { $firstParentWindows = ($base -replace '/',[IO.Path]::DirectorySeparatorChar) }
            } else {
                $firstParentWindows = ($base -replace '/',[IO.Path]::DirectorySeparatorChar)
            }

            if (-not (Test-Path (Join-Path $firstParentWindows 'toc.yml'))) {
                # Skip this file - first parent does not contain a toc.yml
                return
            }
        }

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
