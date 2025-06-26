function Get-UnlinkedFiles {
    [CmdletBinding()]
    param (
        [string[]]$allMdFiles,
        [string[]]$allMdLinks,
        [string[]]$expectedUnlinked,
        [string[]]$allXrefLinksInToc
    )

    Write-Verbose 'Searching for unlinked md files that are not listed in TOC'
    Write-Verbose "All Markdown Files: $($allMdFiles -join ', ')"
    Write-Verbose "All Markdown Links: $($allMdLinks -join ', ')"
    Write-Verbose "Expected Unlinked Files: $($expectedUnlinked -join ', ')"
    Write-Verbose "All Xref Links in TOC: $($allXrefLinksInToc -join ', ')"

    $allMdFiles | 
    Where-Object {
        -not ($allMdLinks -contains $_) -and
        -not ($expectedUnlinked -contains $_)
    } |
    ForEach-Object {
        $file = $_
        Write-Verbose "Processing file: $file"

        $uid = Get-Content $file -TotalCount 3  | 
        Where-Object { $_ -match 'uid:\s*(\S+)' } |
        ForEach-Object { $matches[1] }
        Write-Verbose "Extracted UID: $uid"

        if (-not $uid) {
            Write-Verbose "No UID found in $file"
            $file # Return the file if no UID is found
        }
        elseif ($allXrefLinksInToc -notcontains $uid) {
            Write-Verbose "UID $uid found in $file, but not in TOC"
            $file # Return the file if the UID is not in the TOC
        }
        else {
            Write-Verbose "File $file is referred by $uid in TOC"
        }
    }
    Write-Verbose 'Done!'
}