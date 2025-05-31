# Import the function to test
BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1','.ps1')
}

Describe "Get-UnlinkedFiles" {
    It "1. Returns unlinked files not in TOC or expected unlinked list" {
        # Arrange
        $allMdFiles = @("file1.md", "file2.md", "file3.md")
        $allMdLinks = @("file1.md")
        $expectedUnlinked = @("file3.md")
        $allXrefLinksInToc = @("uid: My.Mocked.UID")

        # Mock file content for UID extraction
        Mock -CommandName Get-Content -MockWith {
            Write-Host "Mocking Get-Content for $($args[0])"
            if ($args[0] -eq "file2.md") { "uid: My.Mocked.UID" }
            else { "" }
        }

        # Act
        $result = Get-UnlinkedFiles -allMdFiles $allMdFiles -allMdLinks $allMdLinks -expectedUnlinked $expectedUnlinked -allXrefLinksInToc $allXrefLinksInToc

        # Assert
        $result | Should -Be @("file2.md")
    }

    It "2. Returns empty array if all files are linked or expected unlinked" {
        # Arrange
        $allMdFiles = @("file1.md", "file2.md")
        $allMdLinks = @("file1.md", "file2.md")
        $expectedUnlinked = @()
        $allXrefLinksInToc = @("uid: My.Mocked.UID", "uid: My.Other.Mocked.Uid")

        # Mock file content for UID extraction
        Mock -CommandName Get-Content -MockWith {
            if ($args[0] -eq "file2.md") { "uid: My.Mocked.UID" }
            else { "" }
        }

        # Act
        $result = Get-UnlinkedFiles -allMdFiles $allMdFiles -allMdLinks $allMdLinks -expectedUnlinked $expectedUnlinked -allXrefLinksInToc $allXrefLinksInToc

        # Assert
        $result | Should -Be @()
    }

    It "3. Handles files with no UID gracefully" {
        # Arrange
        $allMdFiles = @("file1.md", "file2.md")
        $allMdLinks = @("file1.md")
        $expectedUnlinked = @()
        $allXrefLinksInToc = @("uid: Some.Uid")

        # Mock file content for UID extraction
        Mock -CommandName Get-Content -MockWith {
            if ($args[0] -eq "file2.md") { "" }
            else { "" }
        }

        # Act
        $result = Get-UnlinkedFiles -allMdFiles $allMdFiles -allMdLinks $allMdLinks -expectedUnlinked $expectedUnlinked -allXrefLinksInToc $allXrefLinksInToc

        # Assert
        $result | Should -Be @("file2.md")
    }

    It "4. Identifies multiple unlinked files correctly"{
        # Test Input
        $allMdFiles = @("file1.md", "file2.md", "file3.md")
        $allMdLinks = @("file1.md")
        $expectedUnlinked = @("file3.md")
        $allXrefLinksInToc = @("uid1")

        # Mock Get-Content for testing
        Mock -CommandName Get-Content -MockWith {
            if ($args[0] -eq "file2.md") { "uid: uid2" }
            elseif ($args[0] -eq "file3.md") { "" }
            else { "" }
        }

        # Run the function
        $result = Get-UnlinkedFiles -allMdFiles $allMdFiles -allMdLinks $allMdLinks -expectedUnlinked $expectedUnlinked -allXrefLinksInToc $allXrefLinksInToc -Verbose

        # Output the result
        Write-Host "Unlinked Files: $($result -join ', ')"
    }
}