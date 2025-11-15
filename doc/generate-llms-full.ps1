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
    [Parameter(Mandatory)] [string] $TocYmlPath,         # Path to root toc.yml file
    [string] $GitHubBranch = "master"                    # GitHub branch for raw URLs (default: master)
)

function Get-RelativePath ($Parent, $Child) {
    # Normalize paths
    $parentPath = $Parent
    $childPath = $Child
    
    # Resolve to full paths if they exist
    if (Test-Path $Parent) {
        $parentPath = (Resolve-Path $Parent).Path
    }
    if (Test-Path $Child) {
        $childPath = (Resolve-Path $Child).Path
    }
    
    # Ensure parent path ends with separator
    if (-not $parentPath.EndsWith([IO.Path]::DirectorySeparatorChar)) {
        $parentPath += [IO.Path]::DirectorySeparatorChar
    }
    
    # Use URI-based relative path calculation
    try {
        $parentUri = [Uri]$parentPath
        $childUri = [Uri]$childPath
        $relativeUri = $parentUri.MakeRelativeUri($childUri)
        return [Uri]::UnescapeDataString($relativeUri.ToString()) -replace '/', [IO.Path]::DirectorySeparatorChar
    }
    catch {
        # Fallback: simple string replacement (use Ordinal for case-sensitive file systems)
        if ($childPath.StartsWith($parentPath, [StringComparison]::Ordinal)) {
            return $childPath.Substring($parentPath.Length)
        }
        return $childPath
    }
}

# Build a cache of xref uid to markdown file path mappings
function Build-XrefCache {
    param([string] $BaseDir)
    
    $cache = @{}
    $docRoot = Split-Path $BaseDir -Parent
    
    Get-ChildItem -Path $docRoot -Filter "*.md" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
            if ($content -and $content -match '(?s)^---\s*\n.*?uid:\s*(.+?)\s*\n.*?^---') {
                $uid = $Matches[1].Trim()
                if ($uid) {
                    $cache[$uid] = $_.FullName
                }
            }
            # Also check for uid outside YAML frontmatter (some files have it)
            elseif ($content -and $content -match '(?m)^\s*uid:\s*(.+)$') {
                $uid = $Matches[1].Trim()
                if ($uid) {
                    $cache[$uid] = $_.FullName
                }
            }
        }
        catch {
            Write-Warning "Could not process file '$($_.FullName)': $_"
        }
    }
    
    return $cache
}

# Parse YAML toc file and extract items
# Note: Uses a simple hand-rolled YAML parser. Known limitations:
# - Does not handle quoted strings with special characters
# - Does not handle multiline values or escape sequences
# - Assumes well-formed YAML structure from docfx toc.yml files
function Parse-TocYml {
    param(
        [string] $TocPath,
        [int] $IndentLevel = 0,
        [string] $BaseDir,
        [string] $GenerateType = 'Full',
        [hashtable] $XrefCache = @{},
        [System.Collections.Generic.HashSet[string]] $VisitedTocs = (New-Object 'System.Collections.Generic.HashSet[string]')
    )

    # Resolve to absolute path and check for circular references
    $absoluteTocPath = (Resolve-Path $TocPath -ErrorAction SilentlyContinue).Path
    if (-not $absoluteTocPath) {
        Write-Warning "TOC file not found: $TocPath"
        return @()
    }
    
    if ($VisitedTocs.Contains($absoluteTocPath)) {
        Write-Warning "Circular reference detected: $absoluteTocPath already visited. Skipping to avoid infinite recursion."
        return @()
    }
    
    $null = $VisitedTocs.Add($absoluteTocPath)

    $tocContent = Get-Content $TocPath -Raw
    
    # Parse YAML into a structured format - using a simple state machine
    $yamlLines = $tocContent -split "`r?`n"
    $items = @()
    $itemStack = New-Object 'System.Collections.Generic.Stack[object]'
    
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
            }
            
            # Determine parent by finding the item with the highest indent that's less than current
            # Pop items that have equal or greater indentation
            while ($itemStack.Count -gt 0 -and $itemStack.Peek().Indent -ge $indent) {
                [void]$itemStack.Pop()
            }
            
            # The top of stack (if any) is the parent
            if ($itemStack.Count -gt 0) {
                $parent = $itemStack.Peek()
                $parent.Items += $item
            }
            else {
                # Root level item
                $items += $item
            }
            
            # Push the new item onto the stack
            $itemStack.Push($item)
            
            # Now parse immediate properties of this item (href, topicHref)
            # Look ahead for properties until we hit "items:" or another "- name:"
            $j = $i + 1
            while ($j -lt $yamlLines.Count) {
                $propLine = $yamlLines[$j]
                
                # Skip empty lines
                if ($propLine -match '^\s*$' -or $propLine -match '^\s*#') {
                    $j++
                    continue
                }
                
                # Check if this is the items: keyword - stop here, children will be parsed in main loop
                if ($propLine -match '^\s+items:\s*$') {
                    break
                }
                
                # Check if this is another list item - stop processing properties
                if ($propLine -match '^(\s*)- name:') {
                    break
                }
                
                # Check for href property
                if ($propLine -match '^\s+href:\s*(.+)$') {
                    $href = $Matches[1].Trim()
                    
                    # Check if it's a nested toc.yml reference
                    if ($href -match '\.yml$') {
                        $nestedTocPath = Join-Path (Split-Path $absoluteTocPath -Parent) $href
                        if (Test-Path $nestedTocPath) {
                            $nestedItems = Parse-TocYml -TocPath $nestedTocPath -BaseDir $BaseDir -GenerateType $GenerateType -IndentLevel 0 -XrefCache $XrefCache -VisitedTocs $VisitedTocs
                            foreach ($nestedItem in $nestedItems) {
                                if (-not ($nestedItem -is [string])) {
                                    $item.Items += $nestedItem
                                }
                            }
                        }
                    }
                    else {
                        $item.Href = $href
                    }
                }
                # Check for topicHref property
                elseif ($propLine -match '^\s+topicHref:\s*(.+)$') {
                    $item.TopicHref = $Matches[1].Trim()
                }
                
                $j++
            }
        }
    }
    
    # Convert parsed items to output lines
    function Convert-ItemToLines {
        param($item, $depth)
        
        $lines = @()
        # Use dash bullet points for all nesting levels
        $indent = '  ' * $depth
        $bullet = '-'
        $prefix = "${indent}${bullet} "
        
        # Determine which href to use
        $href = if ($item.TopicHref) { $item.TopicHref } else { $item.Href }
        
        if ($href) {
            $url = $null
            
            if ($href -match '^xref:(.+?)(#.*)?$') {
                $xrefId = $Matches[1]
                $anchor = if ($Matches[2]) { $Matches[2] } else { '' }
                
                if ($GenerateType -eq 'Llms') {
                    # For llms.txt, try to resolve xref to actual file using cache
                    if ($XrefCache.ContainsKey($xrefId)) {
                        $filePath = $XrefCache[$xrefId]
                        if (Test-Path $filePath) {
                            $docRoot = Split-Path $BaseDir -Parent
                            $relativePath = Get-RelativePath -Parent $docRoot -Child $filePath
                            $relativePath = $relativePath -replace '\\', '/'
                            $url = "https://raw.githubusercontent.com/unoplatform/uno/refs/heads/$GitHubBranch/doc/$relativePath"
                        }
                    }
                    if (-not $url) {
                        Write-Warning "Could not resolve xref: $xrefId"
                    }
                }
                else {
                    # For llms-full.txt, use anchor format with preserved anchor
                    $url = "#${xrefId}${anchor}"
                }
            }
            elseif ($href -match '^https?://') {
                # External URL
                $url = $href
            }
            elseif ($href -match '\.md$') {
                # Relative markdown file
                $filePath = Join-Path (Split-Path $absoluteTocPath -Parent) $href
                $filePath = [System.IO.Path]::GetFullPath($filePath)
                
                if ($GenerateType -eq 'Llms') {
                    # Convert to raw GitHub URL
                    if (Test-Path $filePath) {
                        # Get the path relative to the doc/ folder (parent of articles)
                        $docRoot = Split-Path $BaseDir -Parent
                        $relativePath = Get-RelativePath -Parent $docRoot -Child $filePath
                        $relativePath = $relativePath -replace '\\', '/'
                        $url = "https://raw.githubusercontent.com/unoplatform/uno/refs/heads/$GitHubBranch/doc/$relativePath"
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
                            # Use sanitized relative path as fallback anchor
                            $docRoot = Split-Path $BaseDir -Parent
                            $relativePath = Get-RelativePath -Parent $docRoot -Child $filePath
                            # Remove extension and replace directory separators with dashes
                            $anchorBase = $relativePath -replace '\.md$', ''
                            $anchorBase = $anchorBase -replace '[\\/]', '-'
                            # Sanitize: keep only alphanumeric, dash, underscore
                            $sanitizedAnchor = $anchorBase -replace '[^a-zA-Z0-9\-_]', ''
                            Write-Warning "No UID found in file '$filePath'. Using fallback anchor: '$sanitizedAnchor'"
                            $url = "#${sanitizedAnchor}"
                        }
                    }
                }
            }
            
            if ($url) {
                $lines += "${prefix}[$($item.Name)]($url)"
            }
            else {
                # No valid URL generated (unresolved xref or missing file)
                if ($item.Items.Count -gt 0) {
                    # Has children - render as bold to visually distinguish as section header without link
                    # This indicates a parent node that groups related items
                    $lines += "${prefix}**$($item.Name)**"
                }
                else {
                    # No children - render as plain text to show terminal node without link
                    # This indicates a leaf node that couldn't be resolved
                    $lines += "${prefix}$($item.Name)"
                }
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
    
    # Build xref cache if generating for Llms mode
    $xrefCache = @{}
    if ($GenerateType -eq 'Llms') {
        Write-Host "Building xref cache..." -ForegroundColor Cyan
        $xrefCache = Build-XrefCache -BaseDir $BaseDir
        Write-Host "  Found $($xrefCache.Count) xref mappings" -ForegroundColor Gray
    }
    
    $tocLines = Parse-TocYml -TocPath $TocYmlPath -BaseDir $BaseDir -GenerateType $GenerateType -XrefCache $xrefCache
    
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

# Start with base content (without the Llms TOC)
$parts.Add($baseContent.TrimEnd() + "`n`n")

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
