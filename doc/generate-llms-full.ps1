<#
.SYNOPSIS
    Generates llms.txt and llms-full.txt files from documentation.
    - llms.txt: Base content + table of contents with raw GitHub URLs
    - llms-full.txt: llms.txt content + all markdown documentation concatenated

.EXAMPLE
    .\generate-llms-full.ps1 -InputFolder .\articles -LlmsTxtOutput .\llms.txt -LlmsFullTxtOutput .\llms-full.txt -BaseContentFile .\articles\llms\llms.txt -TocYmlPath .\articles\toc.yml
#>

param(
    [Parameter(Mandatory)] [string] $InputFolder,
    [Parameter(Mandatory)] [string] $LlmsTxtOutput,      # Output path for llms.txt
    [Parameter(Mandatory)] [string] $LlmsFullTxtOutput,  # Output path for llms-full.txt
    [Parameter(Mandatory)] [string] $BaseContentFile,    # Base content file (trimmed llms.txt from repo)
    [Parameter(Mandatory)] [string] $TocYmlPath          # Path to root toc.yml file
)

function Get-RelativePath ($Parent, $Child) {
    try {
        $parentPath = (Resolve-Path $Parent -ErrorAction SilentlyContinue).Path
        $childPath = (Resolve-Path $Child -ErrorAction SilentlyContinue).Path
        
        if (-not $parentPath -or -not $childPath) {
            # Fallback to basic path manipulation
            return $Child -replace [regex]::Escape($Parent), ''
        }
        
        $parentUri = [Uri]($parentPath + [IO.Path]::DirectorySeparatorChar)
        $childUri  = [Uri]$childPath
        return $parentUri.MakeRelativeUri($childUri).OriginalString -replace '/', [IO.Path]::DirectorySeparatorChar
    }
    catch {
        # Fallback: use .NET's GetRelativePath if available
        try {
            return [System.IO.Path]::GetRelativePath($Parent, $Child)
        }
        catch {
            # Last resort: return child path as-is
            return $Child
        }
    }
}

# Parse YAML toc file and extract items
function Parse-TocYml {
    param(
        [string] $TocPath,
        [int] $IndentLevel = 0,
        [string] $BaseDir,
        [string] $GenerateType = 'Full'
    )

    if (-not (Test-Path $TocPath)) {
        Write-Warning "TOC file not found: $TocPath"
        return @()
    }

    $tocContent = Get-Content $TocPath -Raw
    
    # Parse YAML into a structured format
    $yamlLines = $tocContent -split "`r?`n"
    $items = @()
    $stack = New-Object 'System.Collections.Generic.Stack[object]'
    
    for ($i = 0; $i -lt $yamlLines.Count; $i++) {
        $line = $yamlLines[$i]
        
        # Skip empty lines and comments
        if ($line -match '^\s*$' -or $line -match '^\s*#') {
            continue
        }
        
        # Match list items with "- name:"
        if ($line -match '^(\s*)- name:\s*(.+)$') {
            $indent = $Matches[1].Length
            $name = $Matches[2].Trim()
            
            $item = @{
                Name = $name
                Href = $null
                TopicHref = $null
                Items = @()
                Indent = $indent
                Parent = $null
            }
            
            # Find the parent based on indentation
            while ($stack.Count -gt 0 -and $stack.Peek().Indent -ge $indent) {
                [void]$stack.Pop()
            }
            
            if ($stack.Count -gt 0) {
                $parent = $stack.Peek()
                $item.Parent = $parent
                $parent.Items += $item
            }
            else {
                # Root level item
                $items += $item
            }
            
            $stack.Push($item)
        }
        # Match href property
        elseif ($line -match '^\s+href:\s*(.+)$') {
            $href = $Matches[1].Trim()
            if ($stack.Count -gt 0) {
                $currentItem = $stack.Peek()
                
                # Check if it's a nested toc.yml reference
                if ($href -match '\.yml$') {
                    $nestedTocPath = Join-Path (Split-Path $TocPath -Parent) $href
                    if (Test-Path $nestedTocPath) {
                        $nestedItems = Parse-TocYml -TocPath $nestedTocPath -BaseDir $BaseDir -GenerateType $GenerateType -IndentLevel 0
                        # Add nested items as children
                        foreach ($nestedItem in $nestedItems) {
                            if ($nestedItem -is [string]) {
                                # String result from nested parse - skip for now
                            }
                            else {
                                $currentItem.Items += $nestedItem
                            }
                        }
                    }
                }
                else {
                    $currentItem.Href = $href
                }
            }
        }
        # Match topicHref property
        elseif ($line -match '^\s+topicHref:\s*(.+)$') {
            if ($stack.Count -gt 0) {
                $stack.Peek().TopicHref = $Matches[1].Trim()
            }
        }
    }
    
    # Convert parsed items to output lines
    function Convert-ItemToLines {
        param($item, $depth)
        
        $lines = @()
        # Use bullet points for all levels
        $indent = '  ' * $depth
        $bullet = if ($depth -eq 0) { '-' } else { '-' }
        $prefix = "${indent}${bullet} "
        
        # Determine which href to use
        $href = if ($item.TopicHref) { $item.TopicHref } else { $item.Href }
        
        if ($href) {
            $url = $null
            
            if ($href -match '^xref:(.+?)(?:#.*)?$') {
                $xrefId = $Matches[1]
                
                if ($GenerateType -eq 'Llms') {
                    # For llms.txt, try to resolve xref to actual file
                    # Skip for now as xrefs need a complex resolution mechanism
                    # We'll handle this in the main docs section
                }
                else {
                    # For llms-full.txt, use anchor format
                    $url = "#${xrefId}"
                }
            }
            elseif ($href -match '^https?://') {
                # External URL
                $url = $href
            }
            elseif ($href -match '\.md$') {
                # Relative markdown file
                $filePath = Join-Path (Split-Path $TocPath -Parent) $href
                $filePath = [System.IO.Path]::GetFullPath($filePath)
                
                if ($GenerateType -eq 'Llms') {
                    # Convert to raw GitHub URL
                    if (Test-Path $filePath) {
                        # Get the path relative to the doc/ folder (parent of articles)
                        $docRoot = Split-Path $BaseDir -Parent
                        $relativePath = [System.IO.Path]::GetRelativePath($docRoot, $filePath) -replace '\\', '/'
                        $url = "https://raw.githubusercontent.com/unoplatform/uno/refs/heads/master/doc/$relativePath"
                    }
                }
                else {
                    # For llms-full.txt, extract uid
                    if (Test-Path $filePath) {
                        $content = Get-Content $filePath -Raw -ErrorAction SilentlyContinue
                        if ($content -and $content -match '(?m)^\s*uid:\s*(.+)$') {
                            $uid = $Matches[1].Trim()
                            $url = "#${uid}"
                        }
                        else {
                            # Use filename as fallback
                            $uid = [System.IO.Path]::GetFileNameWithoutExtension($filePath)
                            $url = "#${uid}"
                        }
                    }
                }
            }
            
            if ($url) {
                $lines += "${prefix}[$($item.Name)]($url)"
            }
            elseif ($item.Items.Count -gt 0) {
                # Has children but no valid URL - use as section header
                $lines += "${prefix}**$($item.Name)**"
            }
        }
        else {
            # No href - use as section header if has children
            if ($item.Items.Count -gt 0) {
                $lines += "${prefix}**$($item.Name)**"
            }
        }
        
        # Process child items
        foreach ($childItem in $item.Items) {
            $lines += Convert-ItemToLines -item $childItem -depth ($depth + 1)
        }
        
        return $lines
    }
    
    $outputLines = @()
    foreach ($item in $items) {
        $outputLines += Convert-ItemToLines -item $item -depth $IndentLevel
    }
    
    return $outputLines
}

# Generate table of contents from toc.yml files
function Generate-TableOfContents {
    param(
        [string] $TocYmlPath,
        [string] $BaseDir,
        [string] $GenerateType = 'Full'
    )
    
    if (-not $TocYmlPath -or -not (Test-Path $TocYmlPath)) {
        Write-Warning "TOC file not specified or not found: $TocYmlPath"
        return ""
    }
    
    $tocLines = Parse-TocYml -TocPath $TocYmlPath -BaseDir $BaseDir -GenerateType $GenerateType
    
    if ($tocLines.Count -eq 0) {
        return ""
    }
    
    $result = "## Table of Contents`n`n"
    $result += ($tocLines -join "`n")
    $result += "`n`n"
    
    return $result
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

# ── 1) Generate llms.txt (base content + TOC) ───────────────────────────────────
Write-Host "Generating llms.txt..." -ForegroundColor Cyan

if (-not (Test-Path $BaseContentFile)) { 
    throw "Base content file '$BaseContentFile' not found." 
}

$baseContent = Get-Content $BaseContentFile -Raw
$llmsTxtParts = [System.Collections.Generic.List[string]]::new()
$llmsTxtParts.Add($baseContent.TrimEnd() + "`n`n")

# Generate TOC with raw GitHub URLs
$baseDir = (Resolve-Path $InputFolder).Path
$tocForLlms = Generate-TableOfContents -TocYmlPath $TocYmlPath -BaseDir $baseDir -GenerateType 'Llms'
if ($tocForLlms) {
    $llmsTxtParts.Add($tocForLlms)
}

# Write llms.txt
$llmsTxtContent = $llmsTxtParts -join ''
$llmsTxtContent | Set-Content -NoNewline $LlmsTxtOutput
Write-Host "✓ llms.txt written → $LlmsTxtOutput" -ForegroundColor Green

# ── 2) Generate llms-full.txt (llms.txt + all docs) ─────────────────────────────
Write-Host "Generating llms-full.txt..." -ForegroundColor Cyan

$parts = [System.Collections.Generic.List[string]]::new()

# Start with llms.txt content
$parts.Add($llmsTxtContent)
$parts.Add("`n`n")

# Add TOC with xref anchors for navigation within the full document
$tocForFull = Generate-TableOfContents -TocYmlPath $TocYmlPath -BaseDir $baseDir -GenerateType 'Full'
if ($tocForFull) {
    $parts.Add($tocForFull)
}

$baseContentResolved = (Resolve-Path $BaseContentFile).Path

# ── 3) Process all markdown files for llms-full.txt ─────────────────────────────
Get-ChildItem $InputFolder -Recurse -Filter '*.md' |
    Where-Object {
        # Skip base content file and .github folder
        if ($_.FullName -eq $baseContentResolved) { return $false }
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

# ── 4) Write llms-full.txt ──────────────────────────────────────────────────────
$parts | Set-Content -NoNewline $LlmsFullTxtOutput
Write-Host "✓ llms-full.txt written → $LlmsFullTxtOutput" -ForegroundColor Green

Write-Host "`nGeneration complete!" -ForegroundColor Green
