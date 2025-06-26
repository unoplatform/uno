# Import the function to test
# dot source does not always work when using with Invoke-Pester -Output Detailed .\Get-MarkdownHeader.Test.ps1
# so run . .\Get-MarkdownHeader.ps1 manually before.
BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1','.ps1')
}

Describe 'Get-MarkdownHeader' {
    It 'Extracts the first valid Markdown header' {
        # Arrange
        $lines = @(
            '---',
            #'title: Example Title', # TODO: Uncomment this line if the title is transferred correctly as header in the future from Docfx
            '---',
            '<!-- TODO: ## Some commented Title which should not be included in TOC -->',
            '<!--',
            '## Another commented Title which should not be included in TOC',
            '-->',
            '# Header 1',
            'Some content',
            'title: Example Content with title prefix'
        )

        # Act
        $header = Get-MarkdownHeader -Lines $lines -Verbose

        # Assert
        $header | Should -Be 'Header 1'
    }
    It 'Extracts a header with multiple trailing #' {
        # Arrange
        $lines = @(
            '## Header 2 ##',
            'Some content'
        )

        # Act
        $header = Get-MarkdownHeader -Lines $lines -Verbose

        # Assert
        $header | Should -Be 'Header 2'
    }
    # TODO: Uncomment this Test if the title is transferred correctly as header in the future from Docfx
    # It 'Returns the title if no Markdown header is found' {
    #     # Arrange
    #     $lines = @(
    #         '---',
    #         'title: Example Title',
    #         '---',
    #         '<!-- TODO: ## Some commented Title which should not be included in TOC -->',
    #         '<!--',
    #         '## Another commented Title which should not be included in TOC',
    #         '-->'
    #     )

    #     # Act
    #     $header = Get-MarkdownHeader -Lines $lines -Verbose

    #     # Assert
    #     $header | Should -Be 'Example Title'
    # }

    It 'Returns null if no header or title is found' {
        # Arrange
        $lines = @(
            '---',
            '<!-- TODO: ## Some commented Title which should not be included in TOC -->',
            '<!--',
            '## Another commented Title which should not be included in TOC',
            '-->'
        )

        # Act
        $header = Get-MarkdownHeader -Lines $lines -Verbose

        # Assert
        $header | Should -Be $null
    }
}