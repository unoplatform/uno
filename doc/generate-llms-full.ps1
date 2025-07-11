param(
    [Parameter(Mandatory)][string]$InputFolder,
    [string]$OutputFile = "combined.md"
)

# Returns the path of $Child relative to $Parent
function Get-RelativePath ($Parent, $Child) {
    $parentUri = [Uri]((Resolve-Path $Parent).Path + [IO.Path]::DirectorySeparatorChar)
    $childUri  = [Uri](Resolve-Path $Child).Path
    $relative  = $parentUri.MakeRelativeUri($childUri).OriginalString
    # Uri uses / even on Windows – convert if needed
    return $relative -replace '/', [IO.Path]::DirectorySeparatorChar
}

# ── Regexes ─────────────────────────────────────────────────────────────────────
$yamlRx  = '(?ms)^---\s*.*?^---\s*'                 # YAML front-matter
$uidLine = '^\s*uid:\s*(.+)$'                       # uid: XYZ
$inlineX = '<xref:([\w\.\-]+)(?:#[\w\.\-]+)?>'      # <xref:Id#frag>
$linkX   = '\]\(xref:([\w\.\-]+)(?:#[\w\.\-]+)?\)'  # [txt](xref:Id#frag)

$parts = [System.Collections.Generic.List[string]]::new()

Get-ChildItem $InputFolder -Recurse -Filter '*.md' |
    Sort-Object FullName |
    ForEach-Object {
        $text = Get-Content $_.FullName -Raw

        # uid or fallback to filename
        $uid = if ($text -match '(?m)' + $uidLine) { $Matches[1].Trim() } else { $_.BaseName }

        # relative path
        $relPath = Get-RelativePath -Parent $InputFolder -Child $_.FullName

        # strip YAML
        $text = [regex]::Replace($text, $yamlRx, '')

        # rewrite xrefs → in-file anchors
        $text = $text -replace $inlineX, '[${1}](#${1})'
        $text = $text -replace $linkX,   '](#${1})'

        # header + source comment
        $parts.Add("## $uid`n<!-- Source: $relPath -->`n")
        $parts.Add($text.TrimEnd() + "`n`n")
    }

# write combined file
$parts | Set-Content -NoNewline $OutputFile
Write-Host "Done → $OutputFile"
