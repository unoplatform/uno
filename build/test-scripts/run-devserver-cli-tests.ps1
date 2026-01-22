# Set-PSDebug -Trace 1

[CmdletBinding()]
param(
    [string]$DevServerCliDllPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Log([string]$Message){ Write-Host "[devserver-cli-test] $Message" }

$script:DevServerHostExecutable = "dotnet"
$script:DevServerCliEntryPoint = "uno-devserver"
$script:DevServerCliDisplayName = "$script:DevServerHostExecutable $script:DevServerCliEntryPoint"
$script:DevServerCliUsesDllPath = $false
$script:DevServerCliResolvedDllPath = $null

if (-not [string]::IsNullOrWhiteSpace($DevServerCliDllPath)) {
    if (-not (Test-Path -LiteralPath $DevServerCliDllPath)) {
        throw "Provided DevServerCliDllPath '$DevServerCliDllPath' was not found."
    }

    $script:DevServerCliResolvedDllPath = (Resolve-Path -LiteralPath $DevServerCliDllPath -ErrorAction Stop).Path
    $script:DevServerCliEntryPoint = $script:DevServerCliResolvedDllPath
    $script:DevServerCliDisplayName = "$script:DevServerHostExecutable $script:DevServerCliResolvedDllPath"
    $script:DevServerCliUsesDllPath = $true
}

function Get-DevserverCliArguments {
    param([string[]]$Arguments = @())

    return @($script:DevServerCliEntryPoint) + $Arguments
}

function Get-DevserverExternalCommand {
    param([string[]]$Arguments = @())

    return @($script:DevServerHostExecutable) + (Get-DevserverCliArguments -Arguments $Arguments)
}

function Invoke-DevserverCli {
    param([string[]]$Arguments = @())

    $fullArgs = Get-DevserverCliArguments -Arguments $Arguments
    & $script:DevServerHostExecutable @fullArgs
}

if ($script:DevServerCliUsesDllPath -and $script:DevServerCliResolvedDllPath) {
    Write-Log "Using devserver CLI from $script:DevServerCliResolvedDllPath"
}

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

function Wait-ForDevserverListEntry([int]$Port, [string]$SolutionDirectory, [int]$MaxAttempts = 30, [int]$DelaySeconds = 2) {
    $normalizedDirectory = $SolutionDirectory
    try {
        $normalizedDirectory = (Resolve-Path -LiteralPath $SolutionDirectory -ErrorAction Stop).Path
    }
    catch {
        Write-Log "Unable to normalize solution directory '$SolutionDirectory' for list validation: $($_.Exception.Message)"
    }

    $escapedDirectory = [Regex]::Escape($normalizedDirectory)
    $portPattern = "Port\s*:\s*$Port"
    $endpointPattern = ":$Port(\b|/)"

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Write-Log "Checking devserver list for directory $normalizedDirectory (attempt $attempt/$MaxAttempts)..."

        $listOutput = ""
        try {
            $listOutput = Invoke-DevserverCli -Arguments @('list') 2>&1
        }
        catch {
            $listOutput = $_.Exception.Message
        }

        $listText = ($listOutput | Out-String)

        if ($LASTEXITCODE -eq 0 -and $listText -match $escapedDirectory -and ($listText -match $portPattern -or $listText -match $endpointPattern)) {
            return $true
        }

        Start-Sleep -Seconds $DelaySeconds
    }

    Write-Log "Devserver list did not report directory $normalizedDirectory with port $Port after $MaxAttempts attempts."
    return $false
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

        $unoAppArguments += @("--") + (Get-DevserverExternalCommand -Arguments @("--mcp-app", "-l", "trace"))

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

function Test-DevserverStartStop {
    param(
        [string]$SlnDir,
        [string]$CsprojDir,
        [string]$CsprojPath,
        [int]$DefaultPort,
        [int]$MaxAttempts
    )

    $port = $DefaultPort

    Invoke-DevserverCli -Arguments @('start', '--httpPort', $port.ToString([System.Globalization.CultureInfo]::InvariantCulture), '-l', 'trace')

    $csprojUserPath = Join-Path $CsprojDir ((Split-Path $CsprojPath -Leaf) + '.user')

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
            Write-Log "No .csproj.user file found. Using default port $DefaultPort"
        }
    }

    $success = Wait-ForHttpPortOpen -Port $port -Path '/' -MaxAttempts $MaxAttempts -ConnectTimeoutMs 2000

    if (-not $success) {
        throw "Devserver did not open HTTP port $port after $MaxAttempts attempts."
    }

    Write-Log "Devserver HTTP port $port is open at $DevServerBaseUrl"

    Stop-DevserverInDirectory -Directory $SlnDir

    return $port
}

function Test-DevserverSolutionDirSupport {
    param(
        [string]$SlnDir,
        [int]$DefaultPort,
        [int]$BaselinePort,
        [int]$MaxAttempts
    )

    Write-Log "Validating --solution-dir support from outside the solution root"

    $solutionDirTestPort = if ($BaselinePort -ge 65535) { $DefaultPort + 1 } else { $BaselinePort + 1 }
    $outerDirectory = Split-Path $SlnDir -Parent
    if ([string]::IsNullOrWhiteSpace($outerDirectory)) {
        $outerDirectory = [System.IO.Path]::GetTempPath()
    }

    Push-Location $outerDirectory
    $solutionDirStarted = $false
    try {
        Invoke-DevserverCli -Arguments @('start', '--solution-dir', $SlnDir, '--httpPort', $solutionDirTestPort.ToString([System.Globalization.CultureInfo]::InvariantCulture), '-l', 'trace')
        $solutionDirStarted = $true

        $solutionDirSuccess = Wait-ForHttpPortOpen -Port $solutionDirTestPort -Path '/' -MaxAttempts $MaxAttempts -ConnectTimeoutMs 2000
        if (-not $solutionDirSuccess) {
            throw "Devserver did not open HTTP port $solutionDirTestPort via --solution-dir after $MaxAttempts attempts."
        }

        Write-Log "Devserver started successfully using --solution-dir at $DevServerBaseUrl"
    }
    finally {
        if ($solutionDirStarted) {
            Stop-DevserverInDirectory -Directory $SlnDir
        }

        Pop-Location
        Set-Location $SlnDir
    }

    return $solutionDirTestPort
}

function Stop-DevserverProcessCapture {
    param($Context)

    if (-not $Context) {
        return
    }

    $process = $Context.Process

    if ($Context.HasStarted -and $process -and -not $process.HasExited) {
        try {
            Stop-Process -Id $process.Id -ErrorAction Stop
        }
        catch {
            Write-Log "Graceful devserver termination failed: $($_.Exception.Message)"
        }

        if (-not $process.WaitForExit(5000)) {
            try {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
            catch {
                Write-Log "Failed to force-terminate devserver process: $($_.Exception.Message)"
            }
        }
    }

    if ($Context.HasStarted -and $process) {
        try { $process.WaitForExit(2000) | Out-Null } catch {}
    }

    if ($Context.CancellationSource) {
        try { $Context.CancellationSource.Cancel() } catch {}
        try { $Context.CancellationSource.Dispose() } catch {}
        $Context.CancellationSource = $null
    }

    foreach ($task in @($Context.StdoutTask, $Context.StderrTask)) {
        if ($task) {
            try {
                if (-not $task.Wait(5000)) {
                    Write-Log "Background log capture task did not complete within timeout."
                }
            }
            catch {
                # swallow cancellation/aggregate errors
            }
        }
    }

    foreach ($stream in @($Context.StdoutStream, $Context.StderrStream)) {
        if ($stream) {
            try { $stream.Flush() } catch {}
            $stream.Dispose()
        }
    }

    $Context.HasStarted = $false
}

function Start-DevserverProcessCapture {
    param(
        [string]$WorkingDirectory,
        [string]$Executable,
        [string[]]$Arguments
    )

    $stdoutLogPath = [System.IO.Path]::GetTempFileName()
    $stderrLogPath = [System.IO.Path]::GetTempFileName()

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Executable
    foreach ($arg in $Arguments) {
        [void]$startInfo.ArgumentList.Add($arg)
    }
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardInput = $true
    $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo

    $context = [pscustomobject]@{
        Process = $process
        StdoutStream = $null
        StderrStream = $null
        StdoutTask = $null
        StderrTask = $null
        CancellationSource = $null
        StdoutLogPath = $stdoutLogPath
        StderrLogPath = $stderrLogPath
        HasStarted = $false
    }

    try {
        $context.StdoutStream = New-Object System.IO.FileStream($stdoutLogPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
        $context.StderrStream = New-Object System.IO.FileStream($stderrLogPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)

        if (-not $process.Start()) {
            throw "Failed to start devserver process for MCP validation."
        }

        $context.HasStarted = $true

        $context.CancellationSource = New-Object System.Threading.CancellationTokenSource
        $token = $context.CancellationSource.Token

        $context.StdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($context.StdoutStream, 81920, $token)
        $context.StderrTask = $process.StandardError.BaseStream.CopyToAsync($context.StderrStream, 81920, $token)
    }
    catch {
        Stop-DevserverProcessCapture -Context $context
        throw
    }

    return $context
}

function Test-McpModeWithoutSolutionDir {
    param(
        [string]$SlnDir,
        [int]$DefaultPort,
        [int]$PrimaryPort,
        [int]$SolutionDirPort,
        [int]$MaxAttempts
    )

    Write-Log "Validating MCP mode from solution root without --solution-dir"

    $maxAllocatedPort = [Math]::Max($PrimaryPort, $SolutionDirPort)
    $mcpTestPort = if ($maxAllocatedPort -ge 65534) { $DefaultPort + 2 } else { $maxAllocatedPort + 1 }

    $processContext = $null
    $devserverStarted = $false

    $mcpArguments = Get-DevserverCliArguments -Arguments @("--mcp-app", "--port", $mcpTestPort.ToString([System.Globalization.CultureInfo]::InvariantCulture), "-l", "trace")
    $processContext = Start-DevserverProcessCapture -WorkingDirectory $SlnDir -Executable $script:DevServerHostExecutable -Arguments $mcpArguments
    $devserverStarted = $true

    try {
        $mcpStarted = Wait-ForDevserverListEntry -Port $mcpTestPort -SolutionDirectory $SlnDir -MaxAttempts $MaxAttempts -DelaySeconds 2
        if (-not $mcpStarted) {
            $stdoutLog = if ($processContext -and (Test-Path $processContext.StdoutLogPath)) { Get-Content $processContext.StdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if ($processContext -and (Test-Path $processContext.StderrLogPath)) { Get-Content $processContext.StderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver MCP mode (no --solution-dir) did not appear in 'uno-devserver list' output after $MaxAttempts attempts.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        Write-Log "Devserver MCP mode without --solution-dir registered successfully in 'uno-devserver list'"
    }
    finally {
        if ($devserverStarted) {
            Stop-DevserverInDirectory -Directory $SlnDir
        }

        Stop-DevserverProcessCapture -Context $processContext

        if ($processContext) {
            foreach ($logPath in @($processContext.StdoutLogPath, $processContext.StderrLogPath)) {
                if ($logPath -and (Test-Path $logPath)) {
                    Remove-Item $logPath -ErrorAction SilentlyContinue
                }
            }
        }
    }
}

function Stop-DevserverInDirectory {
    param([string]$Directory)

    if ([string]::IsNullOrWhiteSpace($Directory) -or -not (Test-Path $Directory)) {
        return
    }

    Push-Location $Directory
    try {
        Write-Log "Ensuring no lingering devserver instances remain in $Directory"
        Invoke-DevserverCli -Arguments @('stop', '-l', 'trace')
    }
    catch {
        Write-Log "Cleanup devserver stop failed: $($_.Exception.Message)"
    }
    finally {
        Pop-Location
    }
}

function Test-CodexIntegration {
    param([string]$SlnDir)

    $CodexAPIKey = $env:CODEX_API_KEY
    $isForkPr = $env:SYSTEM_PULLREQUEST_ISFORK -eq 'True'

    if ($isForkPr) {
        Write-Log "Forked pull request detected. Skipping Codex MCP integration test for security."
        return
    }
    elseif ([string]::IsNullOrWhiteSpace($CodexAPIKey)) {
        Write-Log "CODEX_API_KEY not provided. Skipping Codex MCP integration test."
        return
    }

    Write-Log "CODEX_API_KEY detected. Starting Codex MCP integration test."
    if (Ensure-CodexCli) {
        Register-UnoCodexMcps -WorkingDirectory $SlnDir
        Invoke-CodexToolEnumerationTest -WorkingDirectory $SlnDir
    }
    else {
        throw "Codex CLI unavailable after installation attempt."
    }
}

$finalExitCode = 1
$devserverCleanupDirectory = $null

try {
    cd $env:BUILD_SOURCESDIRECTORY/src/SolutionTemplate
    & $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

    Write-Log "Starting devserver CLI test"

    if (-not $script:DevServerCliUsesDllPath) {
        Write-Log "Installing devserver CLI tool: uno.devserver"
        dotnet tool install uno.devserver --version $env:NBGV_SemVer2
    }
    else {
        Write-Log "Using provided devserver CLI at $script:DevServerCliResolvedDllPath. Skipping tool installation."
    }

    # Use the project path (project directory) so we can look for the .csproj.user file
    $csprojPath = "$env:BUILD_SOURCESDIRECTORY/src/SolutionTemplate/5.6/uno56netcurrent/uno56netcurrent/uno56netcurrent.csproj"
    $csprojDir = Split-Path $csprojPath -Parent
    $slnDir = Split-Path $csprojDir -Parent
    $workDir = $slnDir
    $devserverCleanupDirectory = $slnDir

    if (-not (Test-Path $csprojDir)) {
        throw "Project directory not found: $csprojDir"
    }

    Setup-UnoStudioLicenses -WorkDirectory $workDir

    Set-Location $slnDir

    # Force a restore to prime the dependencies
    & dotnet restore

    $defaultPort = 5042
    $maxAttempts = 30

    $primaryPort = Test-DevserverStartStop -SlnDir $slnDir -CsprojDir $csprojDir -CsprojPath $csprojPath -DefaultPort $defaultPort -MaxAttempts $maxAttempts
    $solutionDirTestPort = Test-DevserverSolutionDirSupport -SlnDir $slnDir -DefaultPort $defaultPort -BaselinePort $primaryPort -MaxAttempts $maxAttempts
    Test-McpModeWithoutSolutionDir -SlnDir $slnDir -DefaultPort $defaultPort -PrimaryPort $primaryPort -SolutionDirPort $solutionDirTestPort -MaxAttempts $maxAttempts
    Test-CodexIntegration -SlnDir $slnDir

    $finalExitCode = 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    try { Pop-Location -ErrorAction SilentlyContinue; Pop-Location -ErrorAction SilentlyContinue } catch {}
    $finalExitCode = 1
}
finally {
    Stop-DevserverInDirectory -Directory $devserverCleanupDirectory
}

exit $finalExitCode
