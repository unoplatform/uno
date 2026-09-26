# parse-results.ps1 — Summarize the NUnit XML produced by a runtime-test run.
#
# Usage: pwsh -File parse-results.ps1 -ResultsFile <path> [-ShowPassed]
#
# The file is UTF-16 encoded; reading it with the wrong encoding yields garbled output.

param(
	[Parameter(Mandatory = $true)]
	[string]$ResultsFile,

	[switch]$ShowPassed
)

$ErrorActionPreference = 'Stop'

[xml]$doc = Get-Content $ResultsFile -Raw -Encoding Unicode
$run = $doc.SelectSingleNode('//test-run')
if (-not $run) {
	throw "No <test-run> element in $ResultsFile."
}

Write-Host ("TOTAL: {0}  PASSED: {1}  FAILED: {2}  INCONCLUSIVE: {3}  SKIPPED: {4}" -f `
		$run.total, $run.passed, $run.failed, $run.inconclusive, $run.skipped)
Write-Host ""

foreach ($case in $doc.SelectNodes('//test-case')) {
	if ($case.result -eq 'Passed' -and -not $ShowPassed) {
		continue
	}
	$marker = switch ($case.result) {
		'Passed' { '  PASS' }
		'Failed' { '**FAIL' }
		'Error' { '**ERR ' }
		'Inconclusive' { '  INCO' }
		default { '  SKIP' }
	}
	Write-Host "$marker  $($case.fullname)"

	$message = $case.SelectSingleNode('.//message')
	if ($message -and $case.result -ne 'Passed') {
		$text = ($message.InnerText -replace '\s+', ' ').Trim()
		if ($text.Length -gt 400) {
			$text = $text.Substring(0, 400) + '…'
		}
		Write-Host "         $text"
	}
	$stack = $case.SelectSingleNode('.//stack-trace')
	if ($stack -and $case.result -in 'Failed', 'Error') {
		($stack.InnerText -split "`n" | Select-Object -First 3) | ForEach-Object { Write-Host "         $($_.Trim())" }
	}
}

# Inconclusive theme tests are expected when the OS theme does not match what the test needs:
# WinUI cannot switch Application.RequestedTheme at runtime.
if ([int]$run.inconclusive -gt 0) {
	Write-Host ""
	Write-Host "Inconclusive results are usually theme tests needing the opposite OS theme (WinUI cannot switch app theme at runtime)."
}
