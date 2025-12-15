# Set-PSDebug -Trace 1

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Log([string]$Message){ Write-Host "[devserver-cli-test] $Message" }

# Wait for a TCP port to be opened on localhost. Accepts a Path parameter (e.g. '/', 'subpath') and sets $Global:DevServerBaseUrl. Returns $true when the port is open, $false otherwise.
function Wait-ForHttpPortOpen([int]$Port, [string]$Path = '/', [int]$MaxAttempts = 30, [int]$ConnectTimeoutMs = 2000, [string]$TargetHost = '127.0.0.1', [string]$Scheme = 'http') {
    $attempt = 0
    $success = $false

    # Normalize path
    if ([string]::IsNullOrEmpty($Path) -or $Path -eq '/') {
        $normalizedPath = ''
    }
    else {
        if ($Path.StartsWith('/')) { $normalizedPath = $Path.Substring(1) } else { $normalizedPath = $Path }
    }

    if ($normalizedPath -ne '') {
        $url = "${Scheme}://${TargetHost}:$Port/$normalizedPath"
    }
    else {
        $url = "${Scheme}://${TargetHost}:$Port/"
    }

    Write-Log "Waiting for devserver to open HTTP port $Port (url: $url)"

    while (-not $success -and $attempt -lt $MaxAttempts) {
        $attempt++
        Write-Log "Checking HTTP port $Port (attempt $attempt/$MaxAttempts)..."
        try {
            $tcp = New-Object System.Net.Sockets.TcpClient
            $async = $tcp.BeginConnect($TargetHost, $Port, $null, $null)
            $connected = $async.AsyncWaitHandle.WaitOne($ConnectTimeoutMs)
            if ($connected -and $tcp.Connected) {
                $tcp.EndConnect($async) | Out-Null
                $success = $true
            }
        }
        catch {
            # ignore and retry
        }
        finally {
            if ($tcp) { $tcp.Close() }
        }

        if (-not $success) { Start-Sleep -Seconds 10 }
    }

    # Expose the base URL for further checks
    $Global:DevServerBaseUrl = $url

    return $success
}

# Helper: try to extract a port number from a .csproj.user XML file
# This now looks specifically for the property 'UnoRemoteControlPort' with the format '1234#'
# It will wait up to 60 seconds for the file/property to appear, polling once per second.
function Get-PortFromCsprojUser([string]$UserFilePath) {
    $maxWaitSeconds = 60
    $elapsed = 0

    Write-Log "Waiting up to $maxWaitSeconds seconds for $UserFilePath to contain UnoRemoteControlPort..."

    while ($elapsed -lt $maxWaitSeconds) {
        try {
            if (-not (Test-Path $UserFilePath)) {
                Start-Sleep -Seconds 10
                $elapsed += 10
                continue
            }

            $content = Get-Content $UserFilePath -Raw -ErrorAction Stop

            # Match element form: <UnoRemoteControlPort>1234#</UnoRemoteControlPort>
            $m = [regex]::Match($content, '<\s*(?:\w+:)?UnoRemoteControlPort\s*>\s*(\d+)\#\s*<', 'IgnoreCase')
            if ($m.Success) { return [int]$m.Groups[1].Value }
        }
        catch {
            Write-Log "Attempt to read/parse $UserFilePath failed: $($_.Exception.Message)"
            # fallthrough to wait and retry
        }

        Start-Sleep -Seconds 10
        $elapsed += 10
    }

    Write-Log "Timed out waiting for UnoRemoteControlPort in $UserFilePath after $maxWaitSeconds seconds."
    return $null
}

function Ensure-CodexCli {
    $codexCommand = Get-Command codex -ErrorAction SilentlyContinue
    if ($codexCommand) {
        Write-Log "Codex CLI already available at $($codexCommand.Source)"
        return $true
    }

    Write-Log "Codex CLI not detected. Attempting installation via npm."

    $npmCommand = Get-Command npm -ErrorAction SilentlyContinue
    if (-not $npmCommand) {
        Write-Log "npm command not found. Cannot install Codex CLI automatically."
        return $false
    }

    $packageName = '@openai/codex'
    $npmArgs = @('install', '-g', $packageName)

    & npm @npmArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Log "npm could not install Codex CLI package $packageName (exit code $LASTEXITCODE)."
        return $false
    }

    $codexCommand = Get-Command codex -ErrorAction SilentlyContinue
    if (-not $codexCommand) {
        Write-Log "Codex CLI still unavailable after npm install attempt."
        return $false
    }

    Write-Log "Codex CLI installed via npm at $($codexCommand.Source)"
    return $true
}

function Invoke-CodexMcpAdd {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    Write-Log "Registering Codex MCP '$Name'"
    & codex mcp add $Name @Arguments
    if ($LASTEXITCODE -ne 0) {
        Write-Log "codex mcp add '$Name' exited with code $LASTEXITCODE. Continuing."
    }
}

function Register-UnoCodexMcps {
    param([string]$WorkingDirectory)

    Push-Location $WorkingDirectory
    try {
        Invoke-CodexMcpAdd -Name "uno" -Arguments @("--url", "https://mcp.platform.uno/v1")
        $envNames = @('DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_X86', 'XDG_DATA_HOME', 'XDG_CONFIG_HOME', 'XDG_STATE_HOME', 'XDG_CACHE_HOME')
        $unoAppArguments = @()
        foreach ($name in $envNames) {
            $value = [System.Environment]::GetEnvironmentVariable($name)
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                $unoAppArguments += @("--env", "$name=$value")
            }
        }

        $unoAppArguments += @("--", "dotnet", "uno-devserver", "--mcp-app", "-l", "trace")

        Invoke-CodexMcpAdd -Name "uno-app" -Arguments $unoAppArguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-CodexToolEnumerationTest {
    param([string]$WorkingDirectory)

    $toolsFile = Join-Path $WorkingDirectory "codex-tools.json"
    if (Test-Path $toolsFile) {
        Remove-Item $toolsFile -Force
    }

    $toolsFileName = Split-Path $toolsFile -Leaf
    $instructions = @"
You are running inside an automated CI validation for the Uno Platform devserver CLI.

Tasks:
1. Create a list of all MCP tools available to you
2. Create or overwrite a JSON file named '$toolsFileName' in the current working directory.
    The JSON must be an object that contains a property ``"tools"`` whose value is an alphabetically sorted array listing every tool identifier you discovered.
3. Confirm completion and exit when finished.

Begin now.
"@

    $stdOutFile = [System.IO.Path]::GetTempFileName()
    $stdErrFile = [System.IO.Path]::GetTempFileName()
    $instructionsFile = [System.IO.Path]::GetTempFileName()

    Set-Content -Path $instructionsFile -Value $instructions -Encoding utf8

    $model = if (-not [string]::IsNullOrWhiteSpace($env:CODEX_MODEL)) { $env:CODEX_MODEL } else { "gpt-5-mini" }

    Write-Log "Invoking Codex CLI to enumerate MCP tools."
    $sandboxMode = if ($IsWindows) { 'danger-full-access' } else { 'workspace-write' }
    $codexArgs = @(
        '--ask-for-approval','never',
        'exec',
        '-m',$model,
        '--sandbox',$sandboxMode,
        '-c','mcp_servers."uno-app".startup_timeout_sec=120',
        '-c','features.web_search_request=true',
        '-c','features.rmcp_client=true',
        '-c','sandbox_workspace_write.network_access=true'
    )

    $codexExecutable = "codex"
    $codexArguments = $codexArgs
    if ($IsWindows) {
        # npm exposes Codex CLI through a .cmd shim on Windows, so run it via cmd.exe
        $codexExecutable = "cmd.exe"
        $codexArguments = @("/c", "codex") + $codexArgs
    }

    $previousRustLog = $env:RUST_LOG
    $env:RUST_LOG = 'debug'
    try {
        $process = Start-Process -FilePath $codexExecutable -ArgumentList $codexArguments -WorkingDirectory $WorkingDirectory -RedirectStandardOutput $stdOutFile -RedirectStandardError $stdErrFile -RedirectStandardInput $instructionsFile -PassThru

        $timeoutMs = 180000 # 180 seconds
        if (-not $process.WaitForExit($timeoutMs)) {
            try { $process.Kill() } catch {}
            $stdErrContent = Get-Content $stdErrFile -ErrorAction SilentlyContinue -Raw
            throw "Codex CLI timed out after $($timeoutMs / 1000) seconds. STDERR:`n$stdErrContent"
        }

        $stdout = Get-Content $stdOutFile -ErrorAction SilentlyContinue -Raw
        $stderr = Get-Content $stdErrFile -ErrorAction SilentlyContinue -Raw

        Remove-Item $stdOutFile -ErrorAction SilentlyContinue
        Remove-Item $stdErrFile -ErrorAction SilentlyContinue
        Remove-Item $instructionsFile -ErrorAction SilentlyContinue

        if ($process.ExitCode -ne 0) {
            throw "Codex CLI exited with code $($process.ExitCode).`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }
        else {
            Write-Log "Codex CLI completed successfully."
            Write-Log "STDOUT:`n$stdout`nSTDERR:`n$stderr"
        }
    }
    finally {
        if ($null -ne $previousRustLog) {
            $env:RUST_LOG = $previousRustLog
        }
        else {
            Remove-Item Env:RUST_LOG -ErrorAction SilentlyContinue
        }
    }

    if (-not (Test-Path $toolsFile)) {
        throw "Codex CLI did not create $toolsFile.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
    }

    $jsonText = Get-Content $toolsFile -Raw -ErrorAction Stop
    try {
        $json = $jsonText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "codex-tools.json is not valid JSON: $($_.Exception.Message)"
    }

    if (-not $json.tools -or $json.tools.Count -eq 0) {
        throw "codex-tools.json does not contain a non-empty 'tools' array."
    }

    Write-Log "Codex CLI reported $($json.tools.Count) tools via codex-tools.json:"
    foreach ($tool in $json.tools) {
        Write-Log " - $tool"
    }

    $screenshotTool = $json.tools | Where-Object { $_ -like '*uno_app_get_screenshot*' } | Select-Object -First 1
    if (-not $screenshotTool) {
        throw "Codex CLI did not report the required 'uno_app_get_screenshot' tool."
    }
}

function Setup-UnoStudioLicenses {
    param([string]$WorkDirectory)

    if ($IsWindows) {
        $dataRoot = if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) { $env:LOCALAPPDATA } else { Join-Path $WorkDirectory '.local' }
    }
    elseif ($IsMacOS) {
        $homePath = if (-not [string]::IsNullOrWhiteSpace($env:HOME)) { $env:HOME } else { $WorkDirectory }
        $dataRoot = Join-Path $homePath 'Library/Application Support'
    }
    else {
        $dataRoot = Join-Path $WorkDirectory '.local'
        $env:XDG_DATA_HOME = $dataRoot
        Write-Log "Set XDG_DATA_HOME to $dataRoot"
    }

    if (-not (Test-Path $dataRoot)) {
        try { New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null; Write-Log "Ensured application data folder: $dataRoot" }
        catch { Write-Warning "Failed to create application data folder '$dataRoot': $_" }
    }

    $unoDir = Join-Path $dataRoot 'Uno Platform'
    if (-not (Test-Path $unoDir)) {
        try { New-Item -ItemType Directory -Path $unoDir -Force | Out-Null; Write-Log "Created Uno data folder: $unoDir" }
        catch { Write-Warning "Failed to create Uno data folder '$unoDir': $_" }
    }

    # Setting up license files this way is temporary until we get API keys.
    if ($env:UNO_STUDIO_LICENSES_BIN) {
        $licensesPath = Join-Path $unoDir 'licenses.bin'
        try {
            $env:UNO_STUDIO_LICENSES_BIN | Out-File -FilePath $licensesPath -Encoding utf8 -Force
            Write-Log "Wrote UNO_STUDIO_LICENSES_BIN to: $licensesPath"
        }
        catch {
            Write-Warning "Failed to write UNO_STUDIO_LICENSES_BIN to $licensesPath : $_"
        }
    }
    else {
        Write-Log "UNO_STUDIO_LICENSES_BIN environment variable not set. Skipping license setup."
    }

    if ($env:UNO_STUDIO_USER_JSON) {
        $userJsonPath = Join-Path $unoDir 'user.json'
        try {
            $env:UNO_STUDIO_USER_JSON | Out-File -FilePath $userJsonPath -Encoding utf8 -Force
            Write-Log "Wrote UNO_STUDIO_USER_JSON to: $userJsonPath"
        }
        catch {
            Write-Warning "Failed to write UNO_STUDIO_USER_JSON to $userJsonPath : $_"
        }
    }
    else {
        Write-Log "UNO_STUDIO_USER_JSON environment variable not set. Skipping user.json setup."
    }

    return $unoDir
}

try {
    cd $env:BUILD_SOURCESDIRECTORY/src/SolutionTemplate
    & $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

    Write-Log "Starting devserver CLI test"

    Write-Log "Installing devserver CLI tool: uno.devserver";
    dotnet tool install uno.devserver --version $env:NBGV_SemVer2

    # Use the project path (project directory) so we can look for the .csproj.user file
    $csprojPath = "$env:BUILD_SOURCESDIRECTORY/src/SolutionTemplate/5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj"
    $csprojDir = Split-Path $csprojPath -Parent
    $slnDir = Split-Path $csprojDir -Parent
    $workDir = $slnDir

    if (-not (Test-Path $csprojDir)) {
        throw "Project directory not found: $csprojDir"
    }

    Setup-UnoStudioLicenses -WorkDirectory $workDir

    Set-Location $slnDir

    # Force a restore to prime the dependencies
    & dotnet restore

    # Default port
    $defaultPort = 5042
    $port = $defaultPort

    # Start the devserver in the current directory
    & dotnet uno-devserver start --httpPort $port -l trace

    # Look for a .csproj.user file next to the project file (e.g. 'uno56netcurrent.csproj.user')
    $csprojUserPath = Join-Path $csprojDir ((Split-Path $csprojPath -Leaf) + '.user')

    $userPort = Get-PortFromCsprojUser -UserFilePath $csprojUserPath
    if ($userPort) {
        Write-Log "Port extracted from .csproj.user (UnoRemoteControlPort): $userPort"

        if ($userPort -lt 1 -or $userPort -gt 65535) {
            throw "Port number in $csprojUserPath is out of range: $userPort"
        }

        $port = $userPort
    }
    else {
        if (Test-Path $csprojUserPath) {
            throw "Could not find a valid UnoRemoteControlPort in $csprojUserPath"
        }
        else {
            Write-Log "No .csproj.user file found. Using default port $defaultPort"
        }
    }

    $maxAttempts=30

    # Validate that the HTTP port is open by attempting to connect to it with a TCP client.
    $success = Wait-ForHttpPortOpen -Port $port -Path '/' -MaxAttempts $maxAttempts -ConnectTimeoutMs 2000

    if (-not $success) {
        throw "Devserver did not open HTTP port $port after $maxAttempts attempts."
    }

    Write-Log "Devserver HTTP port $port is open at $DevServerBaseUrl"

    & dotnet uno-devserver stop -l trace

    $CodexAPIKey = $env:CODEX_API_KEY
    $isForkPr = $env:SYSTEM_PULLREQUEST_ISFORK -eq 'True'

    if ($isForkPr) {
        Write-Log "Forked pull request detected. Skipping Codex MCP integration test for security."
    }
    elseif ([string]::IsNullOrWhiteSpace($CodexAPIKey)) {
        Write-Log "CODEX_API_KEY not provided. Skipping Codex MCP integration test."
    }
    else {
        Write-Log "CODEX_API_KEY detected. Starting Codex MCP integration test."
        if (Ensure-CodexCli) {
            Register-UnoCodexMcps -WorkingDirectory $slnDir
            Invoke-CodexToolEnumerationTest -WorkingDirectory $slnDir
        }
        else {
            throw "Codex CLI unavailable after installation attempt."
        }
    }

    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    try { Pop-Location -ErrorAction SilentlyContinue; Pop-Location -ErrorAction SilentlyContinue } catch {}
    exit 1
}
