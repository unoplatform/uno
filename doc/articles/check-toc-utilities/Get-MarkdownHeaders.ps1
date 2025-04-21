function Get-MarkdownHeader {
    [CmdletBinding()]
    param (
        [string[]]$Lines
    )

    $inComment = $false
    # Write-Verbose "Processing lines: $Lines"

    # Find the first non-comment line that starts with 1-6 # characters
    foreach ($line in $Lines) {
        # Write-Verbose "Checking line: $line"
        if ($line -match '<!--') { $inComment = $true }

        if (-not $inComment -and $line -match '^\s*#{1,6}[ \t]+(.*)(?:\s*#*\s*)?$') {
           # Write-Verbose "Matched header: $($matches[1])"
            return $matches[1].TrimEnd(' ', '#')
        }

        if ($line -match '-->') { $inComment = $false }
    }

    # TODO: Uncomment the following code if at any point, the title is transferred as header in Docfx output

    ## If no header is found, return the first line that starts with 'title:'
    # foreach ($line in $Lines) {
      ##  Write-Verbose "Checking for title in line: $line"
    #    if ($line -match '^title:\s*(.+)') {
       ##     Write-Verbose "Matched title: $matches[1]"
    #        return $matches[1].Trim()
    #    }
    #}

    # If no header is found, return $null
  #  Write-Verbose "No header or title found"
    return $null
}
