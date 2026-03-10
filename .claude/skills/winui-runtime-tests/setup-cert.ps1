# setup-cert.ps1 — Generate (if needed) and trust a local signing certificate
# Run this ONCE per machine. Subsequent runs skip if already set up.
# Works across all worktrees — thumbprint is stored in the user's home directory.
#
# Usage: pwsh -File setup-cert.ps1
#
# SECURITY: This generates a UNIQUE private key per machine via certreq.
# The private key never leaves the local cert store and is NOT committed to
# source control.
#
# Outputs the thumbprint on the last line of stdout.

$ErrorActionPreference = 'Stop'

$certSubject = "CN=Uno Platform"
# Machine-global thumbprint file — shared across all worktrees
$thumbprintFile = Join-Path $env:USERPROFILE ".uno-dev-cert-thumbprint"
$cerExportPath = Join-Path $env:TEMP "uno-samplesapp-cert.cer"

# ────────────────────────────────────────────────────────────
# Step 1: Check if we already have a saved, fully-trusted thumbprint
# ────────────────────────────────────────────────────────────
if (Test-Path $thumbprintFile) {
    $saved = (Get-Content $thumbprintFile -Raw).Trim()
    if ($saved) {
        # Check both: in user store (for signing) AND in Root (for MSIX install)
        & certutil -user -verifystore My $saved 2>&1 | Out-Null
        $inUserStore = ($LASTEXITCODE -eq 0)
        & certutil -verifystore Root $saved 2>&1 | Out-Null
        $inRoot = ($LASTEXITCODE -eq 0)

        if ($inUserStore -and $inRoot) {
            Write-Host "Certificate already set up and trusted."
            Write-Host "Thumbprint: $saved"
            exit 0
        }
        if ($inUserStore) {
            # Cert exists but not yet trusted — skip generation, go straight to trust
            $thumbprint = $saved
            Write-Host "Certificate exists in user store but not yet trusted in Root."
        }
    }
}

# ────────────────────────────────────────────────────────────
# Step 2: Generate a new self-signed cert via certreq (if needed)
# ────────────────────────────────────────────────────────────
if (-not $thumbprint) {
    Write-Host "Generating new self-signed code-signing certificate..."
    Write-Host "Subject: $certSubject"

    $infContent = @"
[Version]
Signature="`$Windows NT`$"

[NewRequest]
Subject = "$certSubject"
KeyLength = 2048
KeySpec = 1
KeyUsage = 0x80
MachineKeySet = FALSE
ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider"
RequestType = Cert

[EnhancedKeyUsageExtension]
OID=1.3.6.1.5.5.7.3.3
"@

    $infPath = Join-Path $env:TEMP "uno-samplesapp-cert.inf"
    $cerReqPath = Join-Path $env:TEMP "uno-samplesapp-certreq.cer"

    Set-Content -Path $infPath -Value $infContent -Encoding ASCII
    if (Test-Path $cerReqPath) { Remove-Item $cerReqPath -Force }

    $reqOutput = & certreq -new $infPath $cerReqPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($reqOutput -join "`n")
        throw "certreq failed to create certificate. Exit code: $LASTEXITCODE"
    }

    # Extract thumbprint from certreq output or cert store
    # certreq prints "Thumbprint: <hex>" — try that first
    $thumbprintFromReq = $reqOutput | Select-String "Thumbprint:\s*([0-9a-fA-F]+)"
    if ($thumbprintFromReq) {
        $thumbprint = $thumbprintFromReq.Matches[0].Groups[1].Value.Trim()
    } else {
        # Fallback: list ALL certs, find CN=Uno Platform entries, take the last hash
        $certutilOutput = & certutil -user -store My 2>&1
        # Filter to lines near "Subject: CN=Uno Platform" and extract hash
        $allHashes = $certutilOutput | Select-String "Cert Hash\(sha1\):"
        if (-not $allHashes -or $allHashes.Count -eq 0) {
            throw "Could not find any certificates in the user store."
        }
        # Take the most recently added (last entry)
        $thumbprintLine = $allHashes[-1].ToString()
        $thumbprint = ($thumbprintLine -replace '.*:\s*', '').Trim()
    }

    Write-Host "Certificate created. Thumbprint: $thumbprint"

    # Cleanup temp files
    Remove-Item $infPath -Force -ErrorAction SilentlyContinue
    Remove-Item $cerReqPath -Force -ErrorAction SilentlyContinue
}

# Save thumbprint to user home (shared across worktrees)
Set-Content -Path $thumbprintFile -Value $thumbprint -NoNewline
Write-Host "Thumbprint saved to: $thumbprintFile"

# ────────────────────────────────────────────────────────────
# Step 3: Export public cert and add to LocalMachine\Root
# ────────────────────────────────────────────────────────────

# Check if already trusted (may have been trusted by a previous partial run)
& certutil -verifystore Root $thumbprint 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Certificate already trusted in LocalMachine\Root."
    Write-Host "Thumbprint: $thumbprint"
    exit 0
}

Write-Host "Exporting public certificate for trust..."
if (Test-Path $cerExportPath) { Remove-Item $cerExportPath -Force }

& certutil -user -store My $thumbprint $cerExportPath 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $cerExportPath)) {
    throw "Failed to export certificate for thumbprint: $thumbprint"
}

Write-Host ""
Write-Host "=== ADMIN ELEVATION REQUIRED (first time only) ==="
Write-Host "Adding certificate to Trusted Root CAs so MSIX packages can be installed."
Write-Host "You may see a UAC prompt - please approve it."
Write-Host ""

$proc = Start-Process -FilePath "certutil.exe" `
    -ArgumentList "-addstore", "Root", $cerExportPath `
    -Verb RunAs -Wait -PassThru

Remove-Item $cerExportPath -Force -ErrorAction SilentlyContinue

if ($proc.ExitCode -ne 0) {
    Write-Host ""
    Write-Host "WARNING: Failed to add to LocalMachine\Root (exit code: $($proc.ExitCode))."
    Write-Host "MSIX installation will fail without this trust."
    Write-Host "Manual fix: Run elevated: certutil -addstore Root <exported-cert.cer>"
    exit 1
}

Write-Host ""
Write-Host "Certificate setup complete!"
Write-Host "Thumbprint: $thumbprint"
