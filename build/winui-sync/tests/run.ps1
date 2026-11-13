# Runs the WinUI sync tooling tests. Requires Pester 5.5+.
param([string]$Path = $PSScriptRoot)

Import-Module Pester -MinimumVersion 5.5
$result = Invoke-Pester $Path -PassThru -Output None
foreach ($failed in $result.Failed) {
	"FAILED: $($failed.ExpandedPath)"
	"  $(($failed.ErrorRecord | ForEach-Object { $_.Exception.Message }) -join "`n  ")"
}
foreach ($container in $result.Containers | Where-Object Result -EQ 'Failed') {
	if ($container.ErrorRecord) { "CONTAINER FAILED: $($container.Item) :: $($container.ErrorRecord[0].Exception.Message)" }
}
"passed=$($result.PassedCount) failed=$($result.FailedCount) skipped=$($result.SkippedCount)"
if ($result.FailedCount -gt 0 -or $result.Result -ne 'Passed') { exit 1 }
