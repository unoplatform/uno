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
# DocFX include: [!include[optional label](path/to/file.md)] -- we drop the label and inline the file
# Case-insensitive include regex: allow optional space, optional label, and consume trailing closing bracket(s)
$includeX = '(?i)\[!include\s*(?:\[[^\]]*\])?\s*\(([^)]+)\)\]+'

$parts = [System.Collections.Generic.List[string]]::new()

# Recursive include expander: expands include directives inside content, resolving paths relative to current base directory.
function Expand-Includes {
    param(
        [string] $content,
        [string] $currentBase,
        [string] $sourceFile = "",
        [int] $depth = 0
    )

    if ($null -eq $content -or $content -eq '') { return $content }
    if ($depth -ge 12) { return "<!-- Max include depth reached -->`n$content" }

    return [regex]::Replace($content, $includeX, {
        param($match)
        $rel = $match.Groups[1].Value.Trim()

        # Build a list of candidate absolute paths to try resolving the include
        $candidates = [System.Collections.Generic.List[string]]::new()

        # 1) As provided (may be absolute)
        $candidates.Add($rel)

        # 2) Relative to current file base
        if ($currentBase) { $candidates.Add((Join-Path $currentBase $rel)) }

        # 3) Relative to InputFolder root (if available)
        if ($InputFolder) {
            $rootResolved = Resolve-Path -Path $InputFolder -ErrorAction SilentlyContinue
            if ($rootResolved) { $candidates.Add((Join-Path $rootResolved.Path $rel)) }
        }

        # 4) Relative to current working directory
        $candidates.Add((Join-Path (Get-Location).Path $rel))

        # For each candidate, also try swapping slashes to handle mixed separators
        $expandedCandidates = [System.Collections.Generic.List[string]]::new()
        foreach ($c in $candidates) {
            if (-not $c) { continue }
            $expandedCandidates.Add($c)
            $swap1 = $c -replace '/', '\\'
            $swap2 = $c -replace '\\', '/'
            if ($swap1 -and $swap1 -ne $c) { $expandedCandidates.Add($swap1) }
            if ($swap2 -and $swap2 -ne $c -and $swap2 -ne $swap1) { $expandedCandidates.Add($swap2) }
        }

        # Try each candidate and pick the first that exists (Resolve-Path to handle relative ..)
        $found = $null
        foreach ($cand in $expandedCandidates) {
            if (-not $cand) { continue }
            $res = Resolve-Path -Path $cand -ErrorAction SilentlyContinue
            if ($res) { $found = $res.Path; break }
            if (Test-Path $cand) { $found = (Get-Item $cand).FullName; break }
        }

        if (-not $found) { return "<!-- Include not found: $rel -->" }

        try {
            $inc = Get-Content $found -Raw
            $inc = [regex]::Replace($inc, $yamlRx, '')
            $inc = Expand-Includes $inc (Split-Path -Path $found -Parent) $found ($depth + 1)
            return $inc.TrimEnd()
        } catch {
            return "<!-- Error including: $rel -->"
        }
    })
}

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
        
        # Skip output files (combined.md, out_*.md, etc.)
        if ($_.Name -match '^(combined|out_).*\.md$') { return $false }
        
        # Skip inline include files (meant to be included in other files)
        if ($_.Name -match '-inline') { return $false }
        if ($_.FullName -match '[\\/]inline[\\/]') { return $false }

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

        # expand DocFX include directives (recursively)
        $baseDir = Split-Path -Path $_.FullName -Parent
        $text = Expand-Includes $text $baseDir $_.FullName 0

        # section header + source comment
        $parts.Add("<!-- Source: $relPath -->`n## $uid`n")
        $parts.Add($text.TrimEnd() + "`n`n")
    }

# ── 3) Write final document ─────────────────────────────────────────────────────
$parts | Set-Content -NoNewline $OutputFile
Write-Host "Done → $OutputFile"
