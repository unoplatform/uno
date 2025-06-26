#
# Reports dead links in the table of contents used by Docfx and .md files that aren't linked anywhere in the TOC
#
function Invoke-TocCheck {
    [Alias('Check-Toc', 'Toc_Checker')]
	[CmdletBinding(SupportsShouldProcess = $true), ConfirmImpact = "Medium"]
    param (
        [string]$TocFilePath = ".\toc.yml",
        [string[]]$ExpectedMissingLinks = @('.\implemented-views.md'),
        [string[]]$ExpectedUnlinked = @('.\roadmap.md'),
        [string]$TempFileName = "toc_additions.yml.tmp"
    )

    begin {
        Write-Verbose "Loading utility scripts from Import-TocCheckerUtils.ps1"
        . .\check-toc-utilities\Import-TocCheckerUtils.ps1
        Import-TocCheckerUtils -UtilsFolder $PSCommandPath.Replace('check-toc.ps1', 'check-toc-utilities') -ExcludeFiles @(Get-IrrelevantFiles)
        Write-Host "Dot-sourced Functions in Scope:" -ForegroundColor Cyan
        Get-Command -CommandType Function |
            Where-Object { $_.Name -match 'Get-MarkdownHeader|Get-UnlinkedFiles' } |
            Select-Object -ExpandProperty Name |
            ForEach-Object { Write-Verbose $_ }

        Write-Information "Reports dead links in the table of contents used by DocFx and .md files that aren't linked anywhere in the TOC"
        Write-Information "See the output file $TempFileName for a list of .md files that are not linked in the TOC"
        Write-Information "Use -Verbose to see more details"
    }

    process {
		Write-Host "The current directory is: $PWD"
        Write-Verbose 'Collecting all Markdown filenames recursively below the execution directory'
        $allMdFiles = Get-ChildItem -Recurse -Filter *.md | Resolve-Path -Relative | Where-Object {
            $_.IndexOf('\implemented\') -lt 0 -and
            $_.IndexOf('\included\') -lt 0
        }
        Write-Verbose "Done! Found $($allMdFiles.Count) .md files"

        Write-Verbose "Reading the content of the TOC file: $TocFilePath"
        $toc = Get-Content -Path $TocFilePath
        Write-Verbose "Done!"

        Write-Verbose 'Extracting all .md links from the TOC file'
        $allMdLinks = $toc |
            Where-Object { $_ -match 'href:\s*(?!xref:|https?://)(\S+\.md)(#[\w\-]+)?' } |
            ForEach-Object { '.\' + ($matches[1] -replace '/', '\') } |
            Sort-Object -Unique
        Write-Verbose 'Done!'

        Write-Verbose 'Extracting all xref links from the TOC file'
        $allXrefLinksInToc = $toc |
            Where-Object { $_ -match 'href: xref:(\S+)' } |
            ForEach-Object { $matches[1] } |
            Sort-Object -Unique
        Write-Verbose 'Done!'

        Write-Verbose "Checking for any link in TOC that points to a non-existing file"
        $badLinks = $allMdLinks |
            Where-Object {
                -not (Test-Path -Path $_) -and
                -not ($ExpectedMissingLinks -contains $_)
            }
        if ($badLinks.Count -gt 0) {
            Write-Host "Bad links found in toc.yml: $badLinks" -ForegroundColor Red
        } else {
            Write-Host 'No bad links found in toc.yml'
        }

        Write-Verbose 'Checking for unlinked files'
        $unlinkedFiles = Get-UnlinkedFiles -allMdFiles $allMdFiles -allMdLinks $allMdLinks -allXrefLinksInToc $allXrefLinksInToc -expectedUnlinked $ExpectedUnlinked -Verbose

        if ($unlinkedFiles.Count -gt 0) {
            Write-Host ".md files not linked in toc.yml: $unlinkedFiles"
            '# UNLINKED .MD FILES: Add to toc.yml in appropriate category' | Out-File $TempFileName
            $unlinkedFiles | ForEach-Object {
                $lines = Get-Content $_ -TotalCount 10
                $header = Get-MarkdownHeader -Lines $lines

                "    - name: $header" | Out-File $TempFileName -Append
                $link = $_.TrimStart('.', '\')
                "      href: $link" | Out-File $TempFileName -Append
            }
            Write-Host "Missing links added to $TempFileName" -ForegroundColor Green
        } else {
            Write-Host 'No unlinked files found'
            if (Test-Path $TempFileName) {
                '# No unlinked .md files' | Out-File $TempFileName
            }
        }
    }

    end {
        Write-Verbose "TOC check completed."
    }
}
