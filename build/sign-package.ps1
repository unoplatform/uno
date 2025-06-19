$currentDirectory = split-path $MyInvocation.MyCommand.Definition

# See if we have the ClientSecret available
if ([string]::IsNullOrEmpty($env:VaultSignClientSecret)) {
    Write-Host "Client Secret not found, not signing packages"
    return;
}

dotnet tool install --tool-path . sign --version 0.9.1-beta.25278.1

$filesToSign = Get-ChildItem -Recurse $Env:ArtifactDirectory\* -Include *.nupkg | Select-Object -ExpandProperty FullName

foreach ($fileToSign in $filesToSign)
{
    Write-Host "Submitting $fileToSign for signing"

    .\sign code azure-key-vault `
        $fileToSign `
        --publisher-name "$env:SignPackageName" `
        --description "$env:SignPackageDescription" `
        --description-url "$env:build_repository_uri" `
        --azure-key-vault-tenant-id "$env:VaultSignTenantId" `
        --azure-key-vault-client-id "$env:VaultSignClientId" `
        --azure-key-vault-client-secret "$env:VaultSignClientSecret" `
        --azure-key-vault-certificate "$env:VaultSignCertificate" `
        --azure-key-vault-url "$env:VaultSignUrl" `
        --verbosity information

    if ($LASTEXITCODE -ne 0) {
		Write-Error "Failed to sign $fileToSign"
		exit $LASTEXITCODE
	}

    Write-Host "Finished signing $fileToSign"
}