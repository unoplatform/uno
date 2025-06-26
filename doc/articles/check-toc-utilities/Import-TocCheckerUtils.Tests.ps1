Describe "Import-TocCheckerUtils.ps1 Dot-Sourcing Functionality" {
    # Import the function to test
    BeforeAll {
        . $PSCommandPath.Replace('.Tests.ps1','.ps1')
    }

    It "should dot-source all .ps1 scripts in the folder except itself, excluded files and test scripts" {
        # Arrange
        $utilsFolder = $PSScriptRoot
        $testFileName = $MyInvocation.MyCommand.Path # TODO: Fit to docs for using this correctly
        $excludeFiles = @('Get-IrrelevantThings.ps1', 'Import-TocCheckerUtils.ps1', $testFileName)

        # Get all utility script files to be dot-sourced (excluding test and irrelevant ones)
        $allPs1Files = Get-ChildItem -Path $utilsFolder -Filter *.ps1 | Where-Object {
            $_.Name -notin $excludeFiles -and
            $_.Name -notmatch '\.Test\.ps1$'
        }

        Write-Host "PS1 Files to be dot-sourced:"
        $allPs1Files | ForEach-Object { Write-Host $_.FullName }

        # Act - Call the dot-sourcing function
        Import-TocCheckerUtils -UtilsFolder $utilsFolder -ExcludeFiles $excludeFiles

        # Capture all functions with source info
        $dotSourcedFunctions = Get-Command -CommandType Function | Where-Object {
            $_.ScriptBlock.File -like "$utilsFolder\*"
        }

        # Extract names and normalize casing
        $newFunctionNames = $dotSourcedFunctions |
            Select-Object -ExpandProperty Name |
            ForEach-Object { $_.ToLowerInvariant() }

        Write-Verbose ("Newly loaded functions:`n - " + ($newFunctionNames -join "`n - "))

        # Assert - Each file must have at least one declared function that is now available
        foreach ($file in $allPs1Files) {
            Write-Verbose "Checking file: $($file.FullName)"

            $declaredFunctions = @()
            $content = Get-Content -Path $file.FullName -Raw
            $functionMatches = [regex]::Matches($content, '(?m)^function\s+([\w-]+)')

            foreach ($match in $functionMatches) {
                $declaredFunctions += $match.Groups[1].Value.ToLowerInvariant()
            }

            if ($declaredFunctions.Count -eq 0) {
                Write-Host "No functions found in file: $($file.Name)"
            }

            foreach ($funcName in $declaredFunctions) {
                Write-Host "Expecting function: $funcName"
                if (-not ($newFunctionNames -contains $funcName)) {
                    Write-Warning "❌ Function '$funcName' was expected but not found in loaded function list."
                }
                $newFunctionNames | Should -Contain $funcName
            }
        }
    }

    It "should not attempt to load non-ps1 files like .md files or import any functions from them" {
        # Arrange - Get all .md files in the target folder
        $mdFiles = Get-ChildItem -Path $PSScriptRoot -Filter *.md

        # Act - Run the utility importer
        Import-TocCheckerUtils -UtilsFolder $PSScriptRoot

        # Get all currently available functions with known source files
        $allFunctions = Get-Command -CommandType Function | Where-Object {
            $_.ScriptBlock.File -ne $null
        }

        # Find functions that were (somehow) defined in .md files
        $mdBasedFunctions = $allFunctions | Where-Object {
            $_.ScriptBlock.File -like '*.md'
        }

        # Assert - No function should originate from a .md file
        $mdBasedFunctions | Should -BeNullOrEmpty

        # Additional safety check - No .md file should have been dot-sourced at all
        foreach ($mdFile in $mdFiles) {
            $mdFile.FullName | Should -Not -BeIn $allFunctions.ScriptBlock.File
        }
    }
}