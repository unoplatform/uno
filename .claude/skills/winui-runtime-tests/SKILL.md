---
description: Build, install, and run runtime tests against the WinUI (WinAppSDK) SamplesApp on Windows. Use when testing against native WinUI to validate parity with Uno.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WinUI Runtime Tests Skill**. This skill builds the WinAppSDK SamplesApp as an MSIX package, installs it, and runs runtime tests via the app execution alias. This is used to validate behavior against native WinUI — the reference implementation that Uno Platform targets.

**Requirements**: Windows only. Requires MSBuild (Visual Studio) and PowerShell (`pwsh` preferred, `powershell.exe` works too).

**Helper scripts** are in the same directory as this SKILL.md (`.claude/skills/winui-runtime-tests/`):
- `setup-cert.ps1` — One-time certificate generation + trust (requires admin elevation once)
- `install-msix.ps1` — Remove old package + install built MSIX
- `run-tests.ps1` — Launch app and wait for test results
- `cleanup.ps1` — Uninstall package

---

## Critical Pitfalls (Read First)

These are real issues encountered in practice — not theoretical:

1. **MSBuild switch syntax in bash**: Forward-slash switches (`/r`, `/p:`) are interpreted as Unix paths by bash. **Always use dash syntax**: `-restore`, `-t:Publish`, `-p:Configuration=Release`.

2. **PowerShell from bash**: Complex PowerShell with `$()`, `$_`, `.Property` gets mangled by bash escaping. **Always write a `.ps1` file and run with `pwsh -NoProfile -File script.ps1`** instead of inline `-Command` strings. The helper scripts in this skill directory handle this for you.

3. **`Cert:` PowerShell drive may not work**: On some environments the `Cert:\` PSDrive and PKI module are unavailable (even in Windows PowerShell 5.1). **Always use `certutil`** command-line tool instead of `New-SelfSignedCertificate`, `Import-PfxCertificate`, `Export-Certificate`, etc. The `certutil` tool works everywhere.

4. **Signing certificate**: CI uses a secret cert. Locally, `setup-cert.ps1` **generates a unique self-signed cert per machine** via `certreq`. The private key never leaves the local cert store and is **not committed to source control**. The thumbprint is saved to `~/.uno-dev-cert-thumbprint` (user home — shared across all worktrees).

5. **Certificate trust for MSIX install**: The self-signed cert must be in `LocalMachine\Root` (Trusted Root CAs) before `Add-AppxPackage` will accept it. This requires **admin elevation on first run only** — `setup-cert.ps1` handles this via `Start-Process -Verb RunAs`. On subsequent runs it's already trusted.

6. **Use `PackageCertificateThumbprint` for signing**: Always build with `-p:PackageCertificateThumbprint=<thumbprint>` (NOT `PackageCertificateKeyFile`). The thumbprint approach is the most reliable across environments. The `PackageCertificateKeyFile` approach often fails with `APPX0105: Cannot import the key file`.

7. **Existing package conflict**: If a SamplesApp is already installed with the same version, `Add-AppxPackage` fails with `0x80073CFB`. **Always remove existing packages first** — `install-msix.ps1` handles this.

8. **crosstargeting_override.props**: The SamplesApp.Windows project targets `$(NetPreviousWinAppSDK)` = `net9.0-windows10.0.19041.0`. The override must either not exist or be set to `net9.0-windows10.0.19041.0`. **If it's set to `net10.0-...`**, dependency projects will target net10.0 while the app targets net9.0, causing NU1201 restore failures.

9. **MAX_PATH (260 chars)**: The PRI resource generator uses Win32 APIs with the 260-char path limit. If you see `PRI175`/`PRI252` errors, shorten the repo path or use `subst` drive mapping.

---

## Execution Workflow

### Phase 0: Parse User Input

Determine what to run from the user's input:
- **All tests**: No filter needed
- **Specific test class**: e.g., `Given_Button` → resolve to fully qualified name
- **Specific test method**: e.g., `Given_Button.When_ContentSet` → resolve to fully qualified name
- **Multiple tests**: Pipe-separated list of fully qualified names

If the user provides partial names, search `src/Uno.UI.RuntimeTests/Tests/` to resolve fully qualified test names (namespace + class + method).

### Phase 1: Prerequisites

Run all prerequisite checks/setup in sequence.

#### 1a. Detect PowerShell

Determine which PowerShell to use for helper scripts:
```bash
if command -v pwsh &>/dev/null; then
    PS_CMD="pwsh"
else
    PS_CMD="powershell.exe"
fi
```

Use `$PS_CMD -NoProfile -ExecutionPolicy Bypass -File script.ps1` for all script invocations.

#### 1b. Find MSBuild

```bash
MSBUILD=$("/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe" \
    -latest -requires Microsoft.Component.MSBuild \
    -find "MSBuild\\**\\Bin\\MSBuild.exe" 2>/dev/null)
```

If `vswhere` is not found, check `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`.

#### 1c. Verify crosstargeting_override.props

Read `src/crosstargeting_override.props`. It must either:
- Not exist (fine)
- Contain `net9.0-windows10.0.19041.0` as the `UnoTargetFrameworkOverride`

**If it's set to anything else**, the build will fail. Fix it before proceeding.

#### 1d. Setup signing certificate (first time)

Run the setup script. It's idempotent — skips if already set up:
```bash
SKILL_DIR=".claude/skills/winui-runtime-tests"
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/setup-cert.ps1"
```

**On first run on a new machine**, this will:
1. Generate a new self-signed cert with subject `CN=Uno Platform` via `certreq`
2. Save the thumbprint to `~/.uno-dev-cert-thumbprint` (shared across worktrees)
3. Prompt UAC elevation to trust the cert in `LocalMachine\Root`

On subsequent runs (including from other worktrees), it detects the cert is already set up and exits immediately.

#### 1e. Read the thumbprint

After `setup-cert.ps1` runs, read the thumbprint for the build step:
```bash
THUMBPRINT=$(cat ~/.uno-dev-cert-thumbprint)
```

### Phase 2: Build the MSIX Package

**CRITICAL**: Set bash timeout to **600000** (10 minutes). **NEVER cancel builds.**

```bash
"$MSBUILD" "src/SamplesApp/SamplesApp.Windows/SamplesApp.Windows.csproj" \
    -restore -t:Publish -m -v:m \
    -p:Configuration=Release \
    -p:Platform=x64 \
    -p:RuntimeIdentifier=win-x64 \
    -p:GenerateAppxPackageOnBuild=true \
    -p:PackageCertificateThumbprint=$THUMBPRINT
```

**Key points:**
- Use **dash syntax** (`-restore`, not `/r`) — forward slashes are eaten by bash
- Use **`PackageCertificateThumbprint`** — most reliable signing method
- The cert must be in the user's cert store already (Phase 1d handles this)

#### Build failure diagnostics

| Error | Cause | Fix |
|-------|-------|-----|
| `NU1201: not compatible with net9.0-...` | `crosstargeting_override.props` TFM mismatch | Set to `net9.0-windows10.0.19041.0` or remove |
| `PRI175` / `PRI252: .xbf not found` | MAX_PATH >= 260 chars | Shorten repo path or `subst` drive |
| `APPX0101: signing key required` | No cert in store | Run `setup-cert.ps1` (Phase 1d) |
| `APPX0105: Cannot import key file` | Used `PackageCertificateKeyFile` instead of thumbprint | Switch to `PackageCertificateThumbprint` |
| `MSB1008: Only one project` | Bash mangled `/r` as path | Use dash syntax: `-restore` |

### Phase 3: Install the MSIX Package

Run the install helper script:
```bash
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/install-msix.ps1" \
    -RepoRoot "."
```

This handles removing existing packages and finding/installing the bundle.

#### Install failure diagnostics

| Error | Cause | Fix |
|-------|-------|-----|
| `0x800B0109: root certificate must be trusted` | Cert not in LocalMachine\Root | Run `setup-cert.ps1` |
| `0x80073CFB: same identity already installed` | Old package present | Script handles this automatically |
| `0x80073D2C: publisher not in unsigned namespace` | MSIX was built without signing | Rebuild with thumbprint (Phase 2) |
| `0x80070057: E_INVALIDARG` | MSIX built with signing disabled has structural issues | Rebuild with signing enabled |

### Phase 4: Construct the Filter

If running specific tests (not all tests):

1. **Format the filter string**: Fully qualified test names, pipe-separated
   - Single: `Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_Scenario`
   - Multiple: `Test1|Test2|Test3`

2. **Base64 encode in bash**:
   ```bash
   FILTER=$(echo -n "fully.qualified.TestName" | base64 -w 0)
   ```

   Or for PowerShell:
   ```powershell
   $filter = [Convert]::ToBase64String(
       [System.Text.Encoding]::UTF8.GetBytes("fully.qualified.TestName"))
   ```

### Phase 5: Run Tests

Run the test helper script:
```bash
RESULTS_FILE="$(pwd)/winui-test-results.xml"

# Without filter (all tests):
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/run-tests.ps1" \
    -ResultsFile "$RESULTS_FILE"

# With filter:
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/run-tests.ps1" \
    -ResultsFile "$RESULTS_FILE" -Filter "$FILTER"
```

Set bash timeout to **600000** (10 minutes).

Results are output in NUnit XML format.

### Phase 6: Parse Results and Cleanup

1. **Read** the NUnit XML results file with the Read tool
2. **Report summary**: total tests, passed, failed, skipped
3. **For failures**: extract failure messages and stack traces
4. **Cleanup** — uninstall the MSIX package:
   ```bash
   $PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/cleanup.ps1"
   ```

**Interpreting WinUI failures**: Tests that fail on WinUI represent the native WinUI behavior. If a test passes on Uno but fails on WinUI (or vice versa), this reveals a parity gap. Use the `[PlatformCondition]` attribute to exclude tests from WinUI:
```csharp
[TestMethod]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public void When_Test_That_Diverges_On_WinUI() { ... }
```

---

## Quick Reference: Complete Bash Flow

This is the **exact sequence** to execute. Copy-paste each step:

```bash
# --- Config ---
SKILL_DIR=".claude/skills/winui-runtime-tests"
PS_CMD="pwsh"  # or "powershell.exe" if pwsh unavailable
MSBUILD=$("/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe" \
    -latest -requires Microsoft.Component.MSBuild \
    -find "MSBuild\\**\\Bin\\MSBuild.exe" 2>/dev/null)

# --- Phase 1: Setup cert (idempotent, first time prompts UAC) ---
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/setup-cert.ps1"
THUMBPRINT=$(cat ~/.uno-dev-cert-thumbprint)

# --- Phase 2: Build MSIX (timeout: 600000ms) ---
"$MSBUILD" "src/SamplesApp/SamplesApp.Windows/SamplesApp.Windows.csproj" \
    -restore -t:Publish -m -v:m \
    -p:Configuration=Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 \
    -p:GenerateAppxPackageOnBuild=true \
    -p:PackageCertificateThumbprint=$THUMBPRINT

# --- Phase 3: Install MSIX ---
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/install-msix.ps1" -RepoRoot "."

# --- Phase 4+5: Run tests (timeout: 600000ms) ---
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/run-tests.ps1" \
    -ResultsFile "$(pwd)/winui-test-results.xml"

# --- Phase 6: Parse results (use Read tool on winui-test-results.xml) ---
# Then cleanup:
$PS_CMD -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/cleanup.ps1"
```

---

## Technical Reference

### App Execution Alias
- **Alias**: `unosamplesapp.exe`
- **Registered by**: MSIX package via `Package.appxmanifest` (`uap5:AppExecutionAlias`)
- **Requires**: MSIX package installed via `Add-AppxPackage`

### Command-Line Arguments
| Argument | Description |
|----------|-------------|
| `--runtime-tests=<path>` | Absolute path for NUnit XML results output |
| `--runtime-test-filter=<base64>` | Base64-encoded, pipe-separated test filter |
| `--runtime-tests-group=<n>` | CI shard index (not typically used locally) |
| `--runtime-tests-group-count=<n>` | CI total shards (not typically used locally) |

### Filter Encoding
- **Format**: Base64-encoded UTF-8 string
- **Separator**: `|` (pipe) between multiple fully qualified test names
- **Delivery**: Via `--runtime-test-filter` CLI argument OR `UITEST_RUNTIME_TESTS_FILTER` env var

### Key Files
- **Project**: `src/SamplesApp/SamplesApp.Windows/SamplesApp.Windows.csproj`
- **App manifest**: `src/SamplesApp/SamplesApp.Windows/Package.appxmanifest`
- **CI YAML**: `build/ci/tests/.azure-devops-tests-winappsdk.yml`
- **CI test script**: `build/test-scripts/run-winui-runtime-tests.ps1`
- **Entry point**: `src/SamplesApp/SamplesApp.Shared/App.Tests.cs`
- **Test location**: `src/Uno.UI.RuntimeTests/Tests/`
- **Local thumbprint**: `~/.uno-dev-cert-thumbprint` (user home, shared across worktrees)

### Build Details
| Property | Value |
|----------|-------|
| **Build tool** | MSBuild via **dash syntax** (not `dotnet build`, not `/slash` switches) |
| **Target framework** | `net9.0-windows10.0.19041.0` (`$(NetPreviousWinAppSDK)`) |
| **Platform** | x64 |
| **Output** | MSIX bundle in `AppPackages/` |
| **WinAppSDK version** | 1.8 (check csproj for exact version) |
| **Manifest publisher** | `CN=Uno Platform` (cert subject must match) |
| **Cert generation** | `certreq` (per-machine, private key stays local) |
| **Runtime installer** | `https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe` |
| **MAX_PATH budget** | Keep full repo path under ~200 chars |

### Signing: What Works vs What Doesn't

| Approach | Reliability | Notes |
|----------|-------------|-------|
| `PackageCertificateThumbprint` | **Best** | Cert must be in user store. Use this. |
| `PackageCertificateKeyFile` + `Password` | Fragile | `APPX0105` errors in many environments |
| Build unsigned + `signtool` post-sign | Works | More steps, but viable fallback |
| `AppxPackageSigningEnabled=false` | **Broken** | Produces unsigned MSIX that can't install (publisher namespace mismatch) |

### Certificate Management: What Works vs What Doesn't

| Approach | Reliability | Notes |
|----------|-------------|-------|
| `certreq -new` (generate) | **Best** | Works everywhere, unique key per machine |
| `certutil -user -importPFX` | **Best** | For importing existing PFX |
| `certutil -addstore Root` (elevated) | **Best** | For trusting; needs admin once |
| `Cert:\` PSDrive + PKI module | **Broken** | `Cert:` drive missing in some environments |
| `New-SelfSignedCertificate` | **Broken** | Depends on `Cert:` drive |
| `Import-PfxCertificate` | **Broken** | Depends on PKI module |
| Committing PFX to repo | **Security risk** | Private key exposed to all repo users |
