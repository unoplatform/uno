$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if ([string]::IsNullOrEmpty($env:SignClientSecret)) {
    Write-Host "Client Secret not found, not signing packages"
    return;
}

dotnet tool install --tool-path . SignClient

# Setup Variables we need to pass into the sign client tool
$appSettings = "$currentDirectory\SignClient.json"

$filesToSign = Get-ChildItem -Recurse $Env:ArtifactDirectory\* -Include *.nupkg | Select-Object -ExpandProperty FullName

foreach ($fileToSign in $filesToSign) {
    Write-Host "Submitting $fileToSign for signing"
    .\SignClient 'sign' -c $appSettings -i $fileToSign -r $env:SignClientUser -s $env:SignClientSecret -n "$env:SignPackageName" -d "$env:SignPackageDescription" -u "$env:build_repository_uri"

    if ($LASTEXITCODE -ne 0) {
		Write-Error "Failed to sign $fileToSign"
		exit $LASTEXITCODE
	}

    Write-Host "Finished signing $fileToSign"
}

Write-Host "Sign-package complete"
