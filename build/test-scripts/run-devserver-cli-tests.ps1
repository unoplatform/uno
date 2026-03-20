# Set-PSDebug -Trace 1

[CmdletBinding()]
param(
    [string]$DevServerCliDllPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Log([string]$Message){ Write-Host "[devserver-cli-test] $Message" }

function Write-TimelineEvent {
    param(
        [string]$Component,
        [string]$Stage,
        [long]$ElapsedMilliseconds,
        [string]$Details = ''
    )

    $suffix = if ([string]::IsNullOrWhiteSpace($Details)) { '' } else { " :: $Details" }
    Write-Host ("[devserver-cli-test][timeline] {0} | {1} | t+{2}ms{3}" -f $Component, $Stage, $ElapsedMilliseconds, $suffix)
}

function Write-TimelineEventsFromText {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return 0
    }

    $count = 0
    $Text -split "`r?`n" |
        Where-Object { $_ -like '*TIMELINE|*' } |
        ForEach-Object {
            $markerIndex = $_.IndexOf('TIMELINE|', [System.StringComparison]::Ordinal)
            if ($markerIndex -lt 0) {
                return
            }

            $payload = $_.Substring($markerIndex)
            $parts = $payload.Split('|', 5)
            if ($parts.Length -lt 5) {
                return
            }

            $elapsed = 0L
            [void][long]::TryParse($parts[3], [ref]$elapsed)
            Write-TimelineEvent -Component $parts[1] -Stage $parts[2] -ElapsedMilliseconds $elapsed -Details $parts[4]
            $count++
        }

    return $count
}

$script:DevServerHostExecutable = "uno-devserver"
$script:DevServerCliEntryPoint = $null
$script:DevServerCliDisplayName = "$script:DevServerHostExecutable $script:DevServerCliEntryPoint"
$script:DevServerCliUsesDllPath = $false
$script:DevServerCliResolvedDllPath = $null
$script:DevServerCliToolPath = $null
$script:CodexUnoAppLogFile = $null

function Get-ConfiguredIntValue {
    param(
        [string]$EnvironmentVariableName,
        [int]$DefaultValue
    )

    $rawValue = [System.Environment]::GetEnvironmentVariable($EnvironmentVariableName)
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return $DefaultValue
    }

    $parsedValue = 0
    if ([int]::TryParse($rawValue, [ref]$parsedValue) -and $parsedValue -gt 0) {
        return $parsedValue
    }

    Write-Log "Environment variable '$EnvironmentVariableName' had invalid value '$rawValue'. Falling back to $DefaultValue."
    return $DefaultValue
}

function Get-ConfiguredBooleanValue {
    param(
        [string]$EnvironmentVariableName,
        [bool]$DefaultValue = $false
    )

    $rawValue = [System.Environment]::GetEnvironmentVariable($EnvironmentVariableName)
    if ([string]::IsNullOrWhiteSpace($rawValue)) {
        return $DefaultValue
    }

    switch -Regex ($rawValue.Trim()) {
        '^(1|true|yes|on)$' { return $true }
        '^(0|false|no|off)$' { return $false }
        default {
            Write-Log "Environment variable '$EnvironmentVariableName' had invalid boolean value '$rawValue'. Falling back to $DefaultValue."
            return $DefaultValue
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($DevServerCliDllPath)) {
    if (-not (Test-Path -LiteralPath $DevServerCliDllPath)) {
        throw "Provided DevServerCliDllPath '$DevServerCliDllPath' was not found."
    }

    $script:DevServerCliResolvedDllPath = (Resolve-Path -LiteralPath $DevServerCliDllPath -ErrorAction Stop).Path
    $script:DevServerHostExecutable = "dotnet"
    $script:DevServerCliEntryPoint = $script:DevServerCliResolvedDllPath
    $script:DevServerCliDisplayName = "$script:DevServerHostExecutable $script:DevServerCliResolvedDllPath"
    $script:DevServerCliUsesDllPath = $true
}

function Get-DevserverCliArguments {
    param([string[]]$Arguments = @())

    if ($script:DevServerCliUsesDllPath) {
        return @($script:DevServerCliEntryPoint) + $Arguments
    }

    return $Arguments
}

function Resolve-DevserverToolShimPath {
    param([string]$ToolDirectory)

    $candidates = if ($IsWindows) {
        @(
            (Join-Path $ToolDirectory 'uno-devserver.exe'),
            (Join-Path $ToolDirectory 'uno-devserver.cmd'),
            (Join-Path $ToolDirectory 'uno-devserver')
        )
    }
    else {
        @(
            (Join-Path $ToolDirectory 'uno-devserver'),
            (Join-Path $ToolDirectory 'uno-devserver.exe')
        )
    }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path -LiteralPath $candidate -ErrorAction Stop).Path
        }
    }

    throw "Unable to locate the installed uno-devserver shim in $ToolDirectory."
}

function Get-DevserverExternalCommand {
    param([string[]]$Arguments = @())

    return @($script:DevServerHostExecutable) + (Get-DevserverCliArguments -Arguments $Arguments)
}

function Invoke-DevserverCli {
    param([string[]]$Arguments = @())

    [string[]]$fullArgs = @(Get-DevserverCliArguments -Arguments $Arguments)
    & $script:DevServerHostExecutable @fullArgs
}

if ($script:DevServerCliUsesDllPath -and $script:DevServerCliResolvedDllPath) {
    Write-Log "Using devserver CLI from $script:DevServerCliResolvedDllPath"
}

# Wait for a TCP port to be opened on localhost. Accepts a Path parameter (e.g. '/', 'subpath') and sets $Global:DevServerBaseUrl. Returns $true when the port is open, $false otherwise.
function Wait-ForHttpPortOpen([int]$Port, [string]$Path = '/', [int]$MaxAttempts = 30, [int]$ConnectTimeoutMs = 2000, [string]$TargetHost = '127.0.0.1', [string]$Scheme = 'http') {
    $attempt = 0
    $success = $false
    $delaySeconds = Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_HTTP_DELAY_SECONDS' -DefaultValue 10

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

        if (-not $success) { Start-Sleep -Seconds $delaySeconds }
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

        if ($attempt -le 3 -or $attempt -eq $MaxAttempts) {
            Write-Log "  list output (attempt $attempt, exitCode=$LASTEXITCODE): $($listText.Trim())"
        }

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
    $pollSeconds = Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_USERFILE_POLL_SECONDS' -DefaultValue 10

    Write-Log "Waiting up to $maxWaitSeconds seconds for $UserFilePath to contain UnoRemoteControlPort..."

    while ($elapsed -lt $maxWaitSeconds) {
        try {
            if (-not (Test-Path $UserFilePath)) {
                Start-Sleep -Seconds $pollSeconds
                $elapsed += $pollSeconds
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

        Start-Sleep -Seconds $pollSeconds
        $elapsed += $pollSeconds
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

function Test-CodexAuthenticationAvailable {
    if (-not (Get-Command codex -ErrorAction SilentlyContinue)) {
        return $false
    }

    $statusOutput = & codex login status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Codex login status check failed: $($statusOutput | Out-String)"
        return $false
    }

    $statusText = ($statusOutput | Out-String)
    if ($statusText -match 'Logged in') {
        Write-Log "Detected ambient Codex authentication."
        return $true
    }

    Write-Log "Codex CLI is available, but no ambient authentication was detected."
    return $false
}

function Get-DefaultCodexHome {
    if (-not [string]::IsNullOrWhiteSpace($env:CODEX_HOME)) {
        return $env:CODEX_HOME
    }

    if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        return Join-Path $env:USERPROFILE '.codex'
    }

    return Join-Path $HOME '.codex'
}

function New-IsolatedCodexHome {
    param([string]$WorkingDirectory)

    $isolatedCodexHome = Join-Path ([System.IO.Path]::GetTempPath()) ("codex-home-" + [Guid]::NewGuid().ToString("N"))
    if (Test-Path $isolatedCodexHome) {
        Remove-Item -LiteralPath $isolatedCodexHome -Recurse -Force
    }

    New-Item -ItemType Directory -Path $isolatedCodexHome -Force | Out-Null

    $defaultCodexHome = Get-DefaultCodexHome
    foreach ($fileName in @('auth.json', 'cap_sid')) {
        $sourcePath = Join-Path $defaultCodexHome $fileName
        if (Test-Path $sourcePath) {
            Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $isolatedCodexHome $fileName) -Force
        }
    }

    @'
model_reasoning_effort = "low"
'@ | Set-Content -Path (Join-Path $isolatedCodexHome 'config.toml') -Encoding utf8

    return $isolatedCodexHome
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

function Get-CodexProcessInvocation {
    $codexCommand = Get-Command codex -ErrorAction Stop

    if ($IsWindows -and $codexCommand.CommandType -eq [System.Management.Automation.CommandTypes]::ExternalScript) {
        return [pscustomobject]@{
            FilePath = 'pwsh'
            Arguments = @(
                '-NoLogo',
                '-NoProfile',
                '-NonInteractive',
                '-ExecutionPolicy', 'Bypass',
                '-File', $codexCommand.Source
            )
        }
    }

    return [pscustomobject]@{
        FilePath = $codexCommand.Source
        Arguments = @()
    }
}

function Invoke-ExternalProcessWithCapturedOutput {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [int]$TimeoutMs
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    foreach ($argument in $Arguments) {
        [void]$startInfo.ArgumentList.Add($argument)
    }
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo

    if (-not $process.Start()) {
        throw "Failed to start process '$FilePath'."
    }

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    if (-not $process.WaitForExit($TimeoutMs)) {
        try {
            if ($IsWindows) {
                & taskkill /PID $process.Id /T /F | Out-Null
            }
            else {
                $process.Kill($true)
            }
        }
        catch {}
        throw "Process '$FilePath' timed out after $($TimeoutMs / 1000) seconds."
    }

    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $stdoutTask.GetAwaiter().GetResult()
        StdErr = $stderrTask.GetAwaiter().GetResult()
    }
}

function Register-UnoCodexMcps {
    param(
        [string]$WorkingDirectory,
        [switch]$ForceRootsFallback
    )

    Push-Location $WorkingDirectory
    try {
        $envNames = @('DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_X86', 'XDG_DATA_HOME', 'XDG_CONFIG_HOME', 'XDG_STATE_HOME', 'XDG_CACHE_HOME')
        $unoAppArguments = @()
        foreach ($name in $envNames) {
            $value = [System.Environment]::GetEnvironmentVariable($name)
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                $unoAppArguments += @("--env", "$name=$value")
            }
        }

        $mcpArguments = @("--mcp-app", "--skip-git-repo-check", "-l", "trace")
        if (-not [string]::IsNullOrWhiteSpace($script:CodexUnoAppLogFile)) {
            $mcpArguments += @("-fl", $script:CodexUnoAppLogFile)
        }
        if ($ForceRootsFallback) {
            $mcpArguments += "--force-roots-fallback"
        }

        $packageVersion = [System.Environment]::GetEnvironmentVariable('UNO_DEVSERVER_PACKAGE_VERSION')
        $packageSource = [System.Environment]::GetEnvironmentVariable('UNO_DEVSERVER_PACKAGE_SOURCE')

        if (-not [string]::IsNullOrWhiteSpace($packageVersion) -and -not [string]::IsNullOrWhiteSpace($packageSource)) {
            $unoAppArguments += @(
                "--",
                "dotnet",
                "dnx",
                "Uno.DevServer@$packageVersion",
                "--source",
                $packageSource,
                "--no-http-cache",
                "--yes",
                "--"
            ) + $mcpArguments
        }
        else {
            $unoAppArguments += @("--") + (Get-DevserverExternalCommand -Arguments $mcpArguments)
        }

        Invoke-CodexMcpAdd -Name "unoapp" -Arguments $unoAppArguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-CodexToolEnumerationTest {
    param([string]$WorkingDirectory)

    $toolsFile = Join-Path $WorkingDirectory "codex-tools.json"
    $toolsFileName = Split-Path $toolsFile -Leaf

    $maxAttempts = 2
    $lastError = $null

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $lastError = $null

        if (Test-Path $toolsFile) {
            Remove-Item $toolsFile -Force
        }

        try {
            Invoke-SingleCodexToolEnumeration -WorkingDirectory $WorkingDirectory -ToolsFile $toolsFile -ToolsFileName $toolsFileName
            return # success
        }
        catch {
            $lastError = $_
            if ($attempt -lt $maxAttempts) {
                Write-Log "Codex tool enumeration attempt $attempt failed: $($_.Exception.Message). Retrying..."
                # Clean up stale DevServer before retry
                Stop-DevserverInDirectory -Directory $WorkingDirectory
            }
        }
    }

    throw "Codex tool enumeration failed after $maxAttempts attempts. Last error: $($lastError.Exception.Message)"
}

function Invoke-SingleCodexToolEnumeration {
    param(
        [string]$WorkingDirectory,
        [string]$ToolsFile,
        [string]$ToolsFileName
    )

    $instructions = @"
You are running inside an automated CI validation for the Uno Platform devserver CLI.

Your ONLY task: enumerate every MCP **tool** (callable function) and return the list as raw JSON.

Steps:
1. Look at the functions available to you whose names start with ``mcp__``.
   These are MCP tools. Each one has the form ``mcp__<server>__<tool_name>``.
   Extract just the ``<tool_name>`` part (e.g. ``mcp__uno-app__uno_app_get_screenshot`` → ``uno_app_get_screenshot``).
   IMPORTANT: Do NOT list MCP resources or resource URIs (like ``uno://health``). Only list callable tool function names.
2. Return ONLY this JSON object, nothing else:
   ``{"tools":["tool_a","tool_b","tool_c"]}``
   where the array is alphabetically sorted. Example with real names:
   ``{"tools":["uno_app_get_screenshot","uno_get_build_output","uno_health"]}``
   Return raw JSON only — no markdown fences, no trailing newline, no extra text.
3. Exit immediately after returning the JSON object.

Begin.
"@

    $stdOutFile = [System.IO.Path]::GetTempFileName()
    $stdErrFile = [System.IO.Path]::GetTempFileName()

    Write-Log "Invoking Codex CLI to enumerate MCP tools."
    $sandboxMode = if ($IsWindows) { 'danger-full-access' } else { 'workspace-write' }
    $codexArgs = @(
        '--ask-for-approval','never',
        'exec',
        '--skip-git-repo-check',
        '--output-last-message', $ToolsFile,
        '--sandbox',$sandboxMode,
        '-c','model_reasoning_effort="low"',
        '-c','mcp_servers.unoapp.startup_timeout_sec=120',
        '-c','sandbox_workspace_write.network_access=true'
    )

    $codexInvocation = Get-CodexProcessInvocation
    $codexExecutable = $codexInvocation.FilePath
    $codexArguments = $codexInvocation.Arguments + $codexArgs + @($instructions)

    $previousRustLog = $env:RUST_LOG
    $env:RUST_LOG = 'debug'
    try {
        $timeoutMs = (Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_CODEX_TIMEOUT_SECONDS' -DefaultValue 120) * 1000
        $execution = Invoke-ExternalProcessWithCapturedOutput -FilePath $codexExecutable -Arguments $codexArguments -WorkingDirectory $WorkingDirectory -TimeoutMs $timeoutMs
        $stdout = $execution.StdOut
        $stderr = $execution.StdErr

        if ($execution.ExitCode -ne 0) {
            throw "Codex CLI exited with code $($execution.ExitCode).`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
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

    if (-not (Test-Path $ToolsFile)) {
        throw "Codex CLI did not create $ToolsFile.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
    }

    $jsonText = (Get-Content $ToolsFile -Raw -ErrorAction Stop).Trim()
    # LLMs sometimes emit a literal \n after the JSON object — strip it
    $jsonText = $jsonText -replace '\\n$', ''
    try {
        $json = $jsonText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "codex-tools.json is not valid JSON (raw content: $jsonText): $($_.Exception.Message)"
    }

    if (-not $json.tools -or $json.tools.Count -eq 0) {
        throw "codex-tools.json does not contain a non-empty 'tools' array."
    }

    # The upstream DevServer + add-ins expose at least 10 tools; a much smaller
    # count means the model listed resources or only built-in tools.
    if ($json.tools.Count -lt 5) {
        throw "codex-tools.json contains only $($json.tools.Count) tool(s), expected at least 5. The model may have listed MCP resources instead of tools. Tools found: $($json.tools -join ', ')"
    }

    Write-Log "Codex CLI reported $($json.tools.Count) tools via codex-tools.json:"
    foreach ($tool in $json.tools) {
        Write-Log " - $tool"
    }

    $screenshotTool = $json.tools | Where-Object { $_ -like '*uno_app_get_screenshot*' } | Select-Object -First 1
    if (-not $screenshotTool) {
        throw "Codex CLI did not report the required 'uno_app_get_screenshot' tool. Tools found: $($json.tools -join ', ')"
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

    $startProcessContext = $null
    $devserverStarted = $false

    try {
        $startArguments = Get-DevserverCliArguments -Arguments @('start', '--httpPort', $port.ToString([System.Globalization.CultureInfo]::InvariantCulture), '-l', 'trace')
        $startProcessContext = Start-DevserverProcessCapture -WorkingDirectory $SlnDir -Executable $script:DevServerHostExecutable -Arguments $startArguments
        $devserverStarted = $true

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
            $stdoutLog = if ($startProcessContext -and (Test-Path $startProcessContext.StdoutLogPath)) { Get-Content $startProcessContext.StdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if ($startProcessContext -and (Test-Path $startProcessContext.StderrLogPath)) { Get-Content $startProcessContext.StderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver did not open HTTP port $port after $MaxAttempts attempts.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        Write-Log "Devserver HTTP port $port is open at $DevServerBaseUrl"
    }
    finally {
        if ($devserverStarted) {
            Stop-DevserverInDirectory -Directory $SlnDir
        }

        Stop-DevserverProcessCapture -Context $startProcessContext

        if ($startProcessContext) {
            foreach ($logPath in @($startProcessContext.StdoutLogPath, $startProcessContext.StderrLogPath)) {
                if ($logPath -and (Test-Path $logPath)) {
                    Remove-Item $logPath -ErrorAction SilentlyContinue
                }
            }
        }
    }

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
    $solutionDirProcessContext = $null
    try {
        $startArguments = Get-DevserverCliArguments -Arguments @('start', '--solution-dir', $SlnDir, '--httpPort', $solutionDirTestPort.ToString([System.Globalization.CultureInfo]::InvariantCulture), '-l', 'trace')
        $solutionDirProcessContext = Start-DevserverProcessCapture -WorkingDirectory $outerDirectory -Executable $script:DevServerHostExecutable -Arguments $startArguments
        $solutionDirStarted = $true

        $solutionDirSuccess = Wait-ForHttpPortOpen -Port $solutionDirTestPort -Path '/' -MaxAttempts $MaxAttempts -ConnectTimeoutMs 2000
        if (-not $solutionDirSuccess) {
            $stdoutLog = if ($solutionDirProcessContext -and (Test-Path $solutionDirProcessContext.StdoutLogPath)) { Get-Content $solutionDirProcessContext.StdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if ($solutionDirProcessContext -and (Test-Path $solutionDirProcessContext.StderrLogPath)) { Get-Content $solutionDirProcessContext.StderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver did not open HTTP port $solutionDirTestPort via --solution-dir after $MaxAttempts attempts.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        Write-Log "Devserver started successfully using --solution-dir at $DevServerBaseUrl"
    }
    finally {
        if ($solutionDirStarted) {
            Stop-DevserverInDirectory -Directory $SlnDir
        }

        Stop-DevserverProcessCapture -Context $solutionDirProcessContext

        if ($solutionDirProcessContext) {
            foreach ($logPath in @($solutionDirProcessContext.StdoutLogPath, $solutionDirProcessContext.StderrLogPath)) {
                if ($logPath -and (Test-Path $logPath)) {
                    Remove-Item $logPath -ErrorAction SilentlyContinue
                }
            }
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

function Wait-ForProcessLogMarker {
    param(
        [string]$LogPath,
        [string]$Marker,
        [int]$TimeoutSeconds = 120,
        [string]$Label = 'MCP process'
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    $pollMs = 500

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if (Test-Path $LogPath) {
            try {
                $reader = [System.IO.StreamReader]::new(
                    [System.IO.FileStream]::new($LogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite))
                try {
                    $content = $reader.ReadToEnd()
                }
                finally {
                    $reader.Dispose()
                }

                if ($content -match [Regex]::Escape($Marker)) {
                    Write-Log "$Label log marker '$Marker' detected."
                    return $true
                }
            }
            catch {
                # file may not be ready yet
            }
        }

        Start-Sleep -Milliseconds $pollMs
    }

    Write-Log "WARNING: $Label log marker '$Marker' not detected within ${TimeoutSeconds}s."
    return $false
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
        # Wait for the MCP process to actually register the DevServer in the ambient registry
        # before polling 'list'. On slow CI agents the .NET runtime + discovery + host startup
        # can take well over 60s, causing the list poll to time out before registration happens.
        $registrationDetected = Wait-ForProcessLogMarker `
            -LogPath $processContext.StderrLogPath `
            -Marker 'DevServer registered:' `
            -TimeoutSeconds 120 `
            -Label 'MCP stderr'

        if (-not $registrationDetected) {
            $stdoutLog = if ($processContext -and (Test-Path $processContext.StdoutLogPath)) { Get-Content $processContext.StdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if ($processContext -and (Test-Path $processContext.StderrLogPath)) { Get-Content $processContext.StderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver MCP mode (no --solution-dir) did not produce a 'DevServer registered:' log within 120s.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        # Verify the registry JSON file actually exists on disk before polling list
        try {
            $reader = [System.IO.StreamReader]::new(
                [System.IO.FileStream]::new($processContext.StderrLogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite))
            try { $stderrSnapshot = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $registryMatch = [Regex]::Match($stderrSnapshot, 'DevServer registered:\s*(.+)')
            if ($registryMatch.Success) {
                $registryJsonPath = $registryMatch.Groups[1].Value.Trim()
                Write-Log "Registry JSON path: $registryJsonPath (exists: $(Test-Path $registryJsonPath))"
                $registryDir = Split-Path $registryJsonPath -Parent
                if (Test-Path $registryDir) {
                    $jsonFiles = Get-ChildItem -Path $registryDir -Filter 'devserver-*.json' -ErrorAction SilentlyContinue
                    Write-Log "Registry dir contents: $($jsonFiles.Name -join ', ')"
                }
            }
        }
        catch {
            Write-Log "WARNING: registry diagnostic failed: $_"
        }

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

function Test-McpModeWithRootsFallback {
    param(
        [string]$SlnDir,
        [int]$DefaultPort,
        [int]$MaxAllocatedPort,
        [int]$CheckAttempts = 5,
        [int]$MaxAttempts = 30
    )

    Write-Log "Validating MCP mode with --force-roots-fallback: devserver should start only after SetRoots is called"

    $mcpTestPort = if ($MaxAllocatedPort -ge 65533) { $DefaultPort + 3 } else { $MaxAllocatedPort + 1 }

    $process = $null
    $mcpProcessStarted = $false
    $stdoutLogPath = [System.IO.Path]::GetTempFileName()
    $stderrLogPath = [System.IO.Path]::GetTempFileName()

    try {
        # Start MCP with --force-roots-fallback flag - devserver should NOT start until roots are provided
        $mcpArguments = Get-DevserverCliArguments -Arguments @("--mcp-app", "--force-roots-fallback", "--port", $mcpTestPort.ToString([System.Globalization.CultureInfo]::InvariantCulture), "-l", "trace")

        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $script:DevServerHostExecutable
        foreach ($arg in $mcpArguments) {
            [void]$startInfo.ArgumentList.Add($arg)
        }
        $startInfo.WorkingDirectory = $SlnDir
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.RedirectStandardInput = $true
        $startInfo.StandardOutputEncoding = [System.Text.Encoding]::UTF8
        $startInfo.StandardErrorEncoding = [System.Text.Encoding]::UTF8

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo

        if (-not $process.Start()) {
            throw "Failed to start MCP proxy process for roots-fallback test."
        }
        $mcpProcessStarted = $true

        # Start background tasks to capture stdout/stderr to files
        $stdoutStream = New-Object System.IO.FileStream($stdoutLogPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
        $stderrStream = New-Object System.IO.FileStream($stderrLogPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::ReadWrite)
        $cts = New-Object System.Threading.CancellationTokenSource
        $stdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($stdoutStream, 81920, $cts.Token)
        $stderrTask = $process.StandardError.BaseStream.CopyToAsync($stderrStream, 81920, $cts.Token)

        # Give the MCP proxy a moment to initialize
        Start-Sleep -Seconds 5

        # PHASE 1: Verify the devserver does NOT appear in the list (because roots-fallback is enabled and no roots were set)
        Write-Log "Phase 1: Verifying devserver did NOT auto-start..."
        $foundInList = $false
        for ($attempt = 1; $attempt -le $CheckAttempts; $attempt++) {
            Write-Log "Checking devserver list for absence of auto-started instance (attempt $attempt/$CheckAttempts)..."

            $listOutput = ""
            try {
                $listOutput = Invoke-DevserverCli -Arguments @('list') 2>&1
            }
            catch {
                $listOutput = $_.Exception.Message
            }

            $listText = ($listOutput | Out-String)
            $portPattern = "Port\s*:\s*$mcpTestPort"
            $endpointPattern = ":$mcpTestPort(\b|/)"

            if ($LASTEXITCODE -eq 0 -and ($listText -match $portPattern -or $listText -match $endpointPattern)) {
                $foundInList = $true
                break
            }

            Start-Sleep -Seconds 2
        }

        if ($foundInList) {
            $stdoutLog = if (Test-Path $stdoutLogPath) { Get-Content $stdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if (Test-Path $stderrLogPath) { Get-Content $stderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver with --force-roots-fallback should NOT have started automatically, but it appeared in 'uno-devserver list'.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        Write-Log "Phase 1 passed: Devserver did NOT auto-start (as expected)"

        # PHASE 2: Send MCP initialize and SetRoots call via STDIO, then verify devserver starts
        Write-Log "Phase 2: Sending MCP initialize and SetRoots via STDIO..."

        # MCP JSON-RPC initialize request
        $initializeRequest = @{
            jsonrpc = "2.0"
            id = 1
            method = "initialize"
            params = @{
                protocolVersion = "2024-11-05"
                capabilities = @{}
                clientInfo = @{
                    name = "test-client"
                    version = "1.0.0"
                }
            }
        } | ConvertTo-Json -Depth 10 -Compress

        # MCP JSON-RPC tools/call request for uno_app_initialize
        $normalizedSlnDir = $SlnDir -replace '\\', '/'
        $setRootsRequest = @{
            jsonrpc = "2.0"
            id = 2
            method = "tools/call"
            params = @{
                name = "uno_app_initialize"
                arguments = @{
                    workspaceDirectory = $normalizedSlnDir
                }
            }
        } | ConvertTo-Json -Depth 10 -Compress

        # Write requests to stdin (each message is a single line in MCP STDIO transport)
        $stdinWriter = $process.StandardInput
        $stdinWriter.WriteLine($initializeRequest)
        $stdinWriter.Flush()

        Start-Sleep -Seconds 2

        $stdinWriter.WriteLine($setRootsRequest)
        $stdinWriter.Flush()

        Write-Log "Sent SetRoots request with root: $normalizedSlnDir"

        # PHASE 3: Verify the devserver NOW appears in the list
        Write-Log "Phase 3: Verifying devserver started after SetRoots..."
        $mcpStarted = Wait-ForDevserverListEntry -Port $mcpTestPort -SolutionDirectory $SlnDir -MaxAttempts $MaxAttempts -DelaySeconds 2

        if (-not $mcpStarted) {
            $stdoutLog = if (Test-Path $stdoutLogPath) { Get-Content $stdoutLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            $stderrLog = if (Test-Path $stderrLogPath) { Get-Content $stderrLogPath -Raw -ErrorAction SilentlyContinue } else { "" }
            throw "Devserver with --force-roots-fallback did NOT start after SetRoots was called via MCP STDIO.`nSTDOUT:`n$stdoutLog`nSTDERR:`n$stderrLog"
        }

        Write-Log "Phase 3 passed: Devserver started after SetRoots call"
        Write-Log "Test-McpModeWithRootsFallback completed successfully"
    }
    finally {
        # Clean up
        if ($cts) {
            try { $cts.Cancel() } catch {}
            try { $cts.Dispose() } catch {}
        }

        if ($process -and $mcpProcessStarted -and -not $process.HasExited) {
            try { $process.Kill() } catch {}
            try { $process.WaitForExit(5000) } catch {}
        }

        if ($stdoutStream) {
            try { $stdoutStream.Flush() } catch {}
            try { $stdoutStream.Dispose() } catch {}
        }
        if ($stderrStream) {
            try { $stderrStream.Flush() } catch {}
            try { $stderrStream.Dispose() } catch {}
        }

        Stop-DevserverInDirectory -Directory $SlnDir

        if (Test-Path $stdoutLogPath) { Remove-Item $stdoutLogPath -ErrorAction SilentlyContinue }
        if (Test-Path $stderrLogPath) { Remove-Item $stderrLogPath -ErrorAction SilentlyContinue }
    }

    return $mcpTestPort
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
    $skipCodex = $env:UNO_SKIP_CODEX_INTEGRATION -eq 'true'

    if ($skipCodex) {
        Write-Log "UNO_SKIP_CODEX_INTEGRATION=true. Skipping Codex MCP integration test."
        return
    }
    elseif ($isForkPr) {
        Write-Log "Forked pull request detected. Skipping Codex MCP integration test for security."
        return
    }

    $canUseAmbientCodex = Test-CodexAuthenticationAvailable
    if ([string]::IsNullOrWhiteSpace($CodexAPIKey) -and -not $canUseAmbientCodex) {
        Write-Log "CODEX_API_KEY not provided and no ambient Codex authentication detected. Skipping Codex MCP integration test."
        return
    }

    if ([string]::IsNullOrWhiteSpace($CodexAPIKey)) {
        Write-Log "Starting Codex MCP integration test using ambient Codex authentication."
    }
    else {
        Write-Log "CODEX_API_KEY detected. Starting Codex MCP integration test."
    }

    if (-not (Ensure-CodexCli)) {
        throw "Codex CLI unavailable after installation attempt."
    }

    $previousCodexHome = $env:CODEX_HOME
    $isolatedCodexHome = New-IsolatedCodexHome -WorkingDirectory $SlnDir

    try {
        $env:CODEX_HOME = $isolatedCodexHome
        Write-Log "Using isolated CODEX_HOME at $isolatedCodexHome for Codex MCP validation."
        $codexTimeline = [System.Diagnostics.Stopwatch]::StartNew()
        Write-TimelineEvent -Component 'codex-script' -Stage 'isolated-home-ready' -ElapsedMilliseconds $codexTimeline.ElapsedMilliseconds -Details $isolatedCodexHome
        $script:CodexUnoAppLogFile = Join-Path $isolatedCodexHome ("unoapp-mcp-" + [Guid]::NewGuid().ToString("N") + ".log")
        Write-Log "Capturing unoapp MCP logs to $script:CodexUnoAppLogFile"

        $nestedRoot = Split-Path $SlnDir -Parent
        $solutionPath = Join-Path $SlnDir 'uno56netcurrent.sln'
        $mcpRegistrationStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        Write-TimelineEvent -Component 'codex-script' -Stage 'mcp-registration.start' -ElapsedMilliseconds $codexTimeline.ElapsedMilliseconds -Details $nestedRoot
        Register-UnoCodexMcps -WorkingDirectory $nestedRoot -ForceRootsFallback
        $mcpRegistrationStopwatch.Stop()
        Write-Log ("Codex MCP registration completed in {0} ms" -f $mcpRegistrationStopwatch.ElapsedMilliseconds)
        Write-TimelineEvent -Component 'codex-script' -Stage 'mcp-registration.complete' -ElapsedMilliseconds $codexTimeline.ElapsedMilliseconds -Details ("duration={0}ms" -f $mcpRegistrationStopwatch.ElapsedMilliseconds)

        $codexFlowStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        Write-TimelineEvent -Component 'codex-script' -Stage 'selection-flow.start' -ElapsedMilliseconds $codexTimeline.ElapsedMilliseconds -Details $solutionPath
        Invoke-CodexSelectionFlowTest -WorkingDirectory $nestedRoot -SolutionPath $solutionPath
        $codexFlowStopwatch.Stop()
        Write-Log ("Codex MCP selection flow completed in {0} ms" -f $codexFlowStopwatch.ElapsedMilliseconds)
        Write-TimelineEvent -Component 'codex-script' -Stage 'selection-flow.complete' -ElapsedMilliseconds $codexTimeline.ElapsedMilliseconds -Details ("duration={0}ms" -f $codexFlowStopwatch.ElapsedMilliseconds)
    }
    finally {
        if ($null -ne $previousCodexHome) {
            $env:CODEX_HOME = $previousCodexHome
        }
        else {
            Remove-Item Env:CODEX_HOME -ErrorAction SilentlyContinue
        }

        if (Test-Path $isolatedCodexHome) {
            Remove-Item -LiteralPath $isolatedCodexHome -Recurse -Force -ErrorAction SilentlyContinue
        }

        $script:CodexUnoAppLogFile = $null
    }
}

function Invoke-CodexSelectionFlowTest {
    param(
        [string]$WorkingDirectory,
        [string]$SolutionPath
    )

    $maxAttempts = Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_CODEX_SELECTION_ATTEMPTS' -DefaultValue 3
    $lastError = $null

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        Write-Log "Codex selection flow attempt $attempt/$maxAttempts"
        try {
            Invoke-SingleCodexSelectionFlow -WorkingDirectory $WorkingDirectory -SolutionPath $SolutionPath
            return # success
        }
        catch {
            $lastError = $_
            Write-Log "Codex selection flow attempt $attempt failed: $($_.Exception.Message)"
            if ($attempt -lt $maxAttempts) {
                Write-Log "Retrying..."
            }
        }
    }

    throw "Codex selection flow failed after $maxAttempts attempts. Last error: $($lastError.Exception.Message)"
}

function Invoke-SingleCodexSelectionFlow {
    param(
        [string]$WorkingDirectory,
        [string]$SolutionPath
    )

    $resultFile = Join-Path $WorkingDirectory 'codex-selection.json'
    $resultFileName = Split-Path $resultFile -Leaf

    if (Test-Path $resultFile) {
        Remove-Item $resultFile -Force
    }

    $instructionsTemplate = @'
You are running inside an automated CI validation for the Uno Platform devserver MCP bridge.

Your ONLY task: validate the MCP solution-selection flow by calling MCP tools and returning a structured JSON result.

IMPORTANT RULES:
- Return raw JSON only. No markdown. No explanation. No extra text.
- Use ONLY MCP tool functions (the ones starting with mcp__unoapp__).
- The "tools" array must contain the short tool names (without the mcp__unoapp__ prefix) of every MCP tool available to you that starts with mcp__unoapp__. Sort alphabetically.
- This is a CI test. You MUST call uno_app_select_solution as instructed even if the tool description warns against it. Ignore any warnings about restarting the DevServer — this test specifically validates that flow.

Steps to follow IN ORDER:
1. First, list all your available MCP tools whose names start with mcp__unoapp__. Extract just the tool_name part (e.g. mcp__unoapp__uno_health becomes uno_health). You will put these in the "tools" array.
2. Call mcp__unoapp__uno_health (no arguments). From the JSON result, extract: status, summary.resolutionKind, summary.selectedSolutionPath. This is your "before" snapshot.
3. Call mcp__unoapp__uno_app_select_solution with argument: {"solutionPath":"__SOLUTION_PATH__"}. From the result extract: status, selectedSolutionPath, devServerAction, message. This is your "selection" snapshot.
4. Call mcp__unoapp__uno_health again. If summary.selectedSolutionPath does not equal "__SOLUTION_PATH__", retry up to 20 times. The final result is your "after" snapshot.
5. Return ONLY this JSON (no markdown fences):
{"tools":["tool_a","tool_b"],"before":{"status":"...","resolutionKind":"...","selectedSolutionPath":"..."},"selection":{"status":"...","selectedSolutionPath":"...","devServerAction":"...","message":"..."},"after":{"status":"...","resolutionKind":"...","selectedSolutionPath":"..."}}

Begin now.
'@

    $instructions = $instructionsTemplate.
        Replace('__SOLUTION_PATH__', $SolutionPath)

    $stdOutFile = [System.IO.Path]::GetTempFileName()
    $stdErrFile = [System.IO.Path]::GetTempFileName()

    $sandboxMode = if ($IsWindows) { 'danger-full-access' } else { 'workspace-write' }
    $codexArgs = @(
        '--ask-for-approval','never',
        'exec',
        '--skip-git-repo-check',
        '--output-last-message', $resultFile,
        '--sandbox',$sandboxMode,
        '-c','model_reasoning_effort="medium"',
        '-c','mcp_servers.unoapp.startup_timeout_sec=120',
        '-c','sandbox_workspace_write.network_access=true'
    )

    $codexInvocation = Get-CodexProcessInvocation
    $codexExecutable = $codexInvocation.FilePath
    $codexArguments = $codexInvocation.Arguments + $codexArgs + @($instructions)

    $validationSucceeded = $false

    try {
        $timeoutMs = (Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_CODEX_TIMEOUT_SECONDS' -DefaultValue 120) * 1000
        $selectionTimeline = [System.Diagnostics.Stopwatch]::StartNew()
        Write-TimelineEvent -Component 'codex-script' -Stage 'codex-exec.start' -ElapsedMilliseconds $selectionTimeline.ElapsedMilliseconds -Details $WorkingDirectory
        $execution = Invoke-ExternalProcessWithCapturedOutput -FilePath $codexExecutable -Arguments $codexArguments -WorkingDirectory $WorkingDirectory -TimeoutMs $timeoutMs
        $stdout = $execution.StdOut
        $stderr = $execution.StdErr
        Set-Content -LiteralPath $stdOutFile -Value $stdout -Encoding utf8
        Set-Content -LiteralPath $stdErrFile -Value $stderr -Encoding utf8
        Write-TimelineEvent -Component 'codex-script' -Stage 'codex-exec.complete' -ElapsedMilliseconds $selectionTimeline.ElapsedMilliseconds -Details ("exitCode={0}" -f $execution.ExitCode)
        $stdoutTimelineCount = Write-TimelineEventsFromText -Text $stdout
        $stderrTimelineCount = Write-TimelineEventsFromText -Text $stderr
        Write-Log ("Codex exec captured stdoutLength={0}, stderrLength={1}, stdoutTimelineEvents={2}, stderrTimelineEvents={3}" -f $stdout.Length, $stderr.Length, $stdoutTimelineCount, $stderrTimelineCount)
        if (-not [string]::IsNullOrWhiteSpace($script:CodexUnoAppLogFile) -and (Test-Path $script:CodexUnoAppLogFile)) {
            $unoAppLogContent = Get-Content -LiteralPath $script:CodexUnoAppLogFile -Raw -ErrorAction SilentlyContinue
            $unoAppTimelineCount = Write-TimelineEventsFromText -Text $unoAppLogContent
            Write-Log ("unoapp MCP file log captured length={0}, timelineEvents={1}" -f $unoAppLogContent.Length, $unoAppTimelineCount)
        }
        elseif (-not [string]::IsNullOrWhiteSpace($script:CodexUnoAppLogFile)) {
            Write-Log "unoapp MCP file log was not created at $script:CodexUnoAppLogFile"
        }
        if ($execution.ExitCode -ne 0) {
            throw "Codex selection flow exited with code $($execution.ExitCode).`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        if (-not (Test-Path $resultFile)) {
            throw "Codex selection flow did not create $resultFile.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        $json = Get-Content $resultFile -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        foreach ($requiredProperty in @('tools', 'before', 'selection', 'after')) {
            if (-not $json.PSObject.Properties[$requiredProperty]) {
                throw "codex-selection.json missing required property '$requiredProperty'. Content: $(Get-Content $resultFile -Raw)"
            }
        }

        $reportedTools = @($json.tools | ForEach-Object { "$_".Trim() }) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        $hasUnoHealth = $reportedTools -contains 'uno_health' -or $reportedTools -contains 'mcp__unoapp__uno_health'
        $hasSelectSolution = $reportedTools -contains 'uno_app_select_solution' -or $reportedTools -contains 'mcp__unoapp__uno_app_select_solution'

        if (-not $hasUnoHealth -or -not $hasSelectSolution) {
            throw "codex-selection.json did not report both unoapp MCP tools. Reported tools: $($reportedTools -join ', '). Content: $(Get-Content $resultFile -Raw)"
        }

        if ($json.selection.selectedSolutionPath -ne $SolutionPath) {
            throw "codex-selection.json reported selectedSolutionPath '$($json.selection.selectedSolutionPath)' instead of '$SolutionPath'."
        }

        if ($json.after.selectedSolutionPath -ne $SolutionPath) {
            throw "codex-selection.json reported after.selectedSolutionPath '$($json.after.selectedSolutionPath)' instead of '$SolutionPath'."
        }

        $validationSucceeded = $true
        Write-TimelineEvent -Component 'codex-script' -Stage 'selection-result.validated' -ElapsedMilliseconds $selectionTimeline.ElapsedMilliseconds -Details $json.after.status
        Write-Log "Codex selection flow validated successfully."
    }
    finally {
        $pathsToDelete = @()
        if ($validationSucceeded) {
            $pathsToDelete += $resultFile
            Write-Log "Codex stdout artifact preserved at $stdOutFile"
            Write-Log "Codex stderr artifact preserved at $stdErrFile"
            if (-not [string]::IsNullOrWhiteSpace($script:CodexUnoAppLogFile)) {
                Write-Log "unoapp MCP log path was $script:CodexUnoAppLogFile"
            }
        }
        else {
            Write-Log "Codex validation artifacts preserved for debugging at $resultFile"
            Write-Log "Codex stdout artifact: $stdOutFile"
            Write-Log "Codex stderr artifact: $stdErrFile"
            if (-not [string]::IsNullOrWhiteSpace($script:CodexUnoAppLogFile)) {
                Write-Log "unoapp MCP log artifact: $script:CodexUnoAppLogFile"
            }
        }

        foreach ($path in $pathsToDelete) {
            if ($path -and (Test-Path $path)) {
                Remove-Item $path -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

function Install-DevserverCliTool {
    param([string]$PackagesDir)

    if ([string]::IsNullOrWhiteSpace($PackagesDir)) {
        $defaultPackagesDir = Join-Path $env:BUILD_SOURCESDIRECTORY 'src/PackageCache'
        if (Test-Path $defaultPackagesDir) {
            $PackagesDir = $defaultPackagesDir
        }
    }

    $toolDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("uno-devserver-tool-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $toolDirectory -Force | Out-Null

    Write-Log "Installing devserver CLI tool into isolated tool path $toolDirectory"

    if (-not [string]::IsNullOrWhiteSpace($PackagesDir) -and (Get-ChildItem -Path $PackagesDir -Filter "Uno.DevServer.*.nupkg" -ErrorAction SilentlyContinue | Select-Object -First 1)) {
        Write-Log "Installing devserver CLI tool from local package artifacts in $PackagesDir"
        & dotnet tool install --tool-path $toolDirectory uno.devserver --add-source $PackagesDir --ignore-failed-sources --prerelease
    }
    else {
        Write-Log "Installing devserver CLI tool from configured feeds (version $env:NBGV_SemVer2)"
        & dotnet tool install --tool-path $toolDirectory uno.devserver --version $env:NBGV_SemVer2
    }

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool install uno.devserver failed with exit code $LASTEXITCODE"
    }

    $script:DevServerCliToolPath = $toolDirectory
    $script:DevServerHostExecutable = Resolve-DevserverToolShimPath -ToolDirectory $toolDirectory
    $script:DevServerCliEntryPoint = $null
    $script:DevServerCliDisplayName = $script:DevServerHostExecutable
    $script:DevServerCliUsesDllPath = $false
    $script:DevServerCliResolvedDllPath = $null

    Write-Log "Using devserver CLI shim at $script:DevServerHostExecutable"
}

function Test-DiscoJsonOutput {
    param([string]$SlnDir)

    Write-Log "Validating disco --json output"

    $output = Invoke-DevserverCli -Arguments @('disco', '--json', '--solution-dir', $SlnDir) 2>&1
    if ($LASTEXITCODE -ne 0) { throw "disco --json exited with code $LASTEXITCODE" }

    $json = ($output | Out-String) | ConvertFrom-Json -ErrorAction Stop

    # Backward compat: existing fields must still be present
    $requiredFields = @('globalJsonPath', 'unoSdkPackage')
    foreach ($field in $requiredFields) {
        if (-not $json.PSObject.Properties[$field]) {
            throw "disco --json missing required field: $field"
        }
    }

    # New field: addIns array
    if (-not $json.PSObject.Properties['addIns']) {
        throw "disco --json missing new 'addIns' field"
    }

    if ($json.addIns.Count -eq 0) {
        Write-Log "WARNING: disco --json returned 0 add-ins (may be expected if no Uno project)"
    }

    foreach ($addIn in $json.addIns) {
        if (-not $addIn.entryPointDll -or -not (Test-Path $addIn.entryPointDll)) {
            throw "disco --json add-in entryPointDll does not exist: $($addIn.entryPointDll)"
        }
        if (-not $addIn.discoverySource) {
            throw "disco --json add-in missing discoverySource field"
        }
        Write-Log " Add-in: $($addIn.entryPointDll) (source: $($addIn.discoverySource))"
    }

    Write-Log "disco --json output validated successfully"
}

function Test-DiscoAddInsOnly {
    param([string]$SlnDir)

    Write-Log "Validating disco --addins-only output"

    # Text mode: semicolon-separated paths
    $textOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "disco --addins-only exited with code $LASTEXITCODE" }

    # Verify parseable as semicolon-separated paths
    $paths = $textOutput -split ';' | Where-Object { $_ -ne '' }
    foreach ($p in $paths) {
        if (-not (Test-Path $p)) { throw "disco --addins-only path not found: $p" }
    }

    # JSON mode: JSON array of paths
    $jsonOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--json', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { throw "disco --addins-only --json exited with code $LASTEXITCODE" }

    $jsonPaths = $jsonOutput | ConvertFrom-Json -ErrorAction Stop
    if ($jsonPaths.Count -ne $paths.Count) {
        throw "disco --addins-only text ($($paths.Count)) and JSON ($($jsonPaths.Count)) path counts differ"
    }

    Write-Log "disco --addins-only validated: $($paths.Count) paths"
}

function Test-CleanupCommand {
    param([string]$SlnDir)

    Write-Log "Validating cleanup command"

    Invoke-DevserverCli -Arguments @('cleanup', '-l', 'trace')
    if ($LASTEXITCODE -ne 0) {
        throw "cleanup command exited with code $LASTEXITCODE"
    }

    Write-Log "cleanup command completed successfully (exit code 0)"
}

function Test-HostAddInsFlag {
    param([string]$SlnDir)

    Write-Log "Validating Host --addins flag"

    # Step 1: Get add-in paths from disco
    $addInsOutput = (Invoke-DevserverCli -Arguments @('disco', '--addins-only', '--solution-dir', $SlnDir) 2>&1 | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($addInsOutput)) {
        Write-Log "No add-ins discovered — skipping --addins flag test"
        return
    }

    # Step 2: Resolve Host path from Uno SDK (mirrors DevServerProcessHelper logic)
    $discoJson = (Invoke-DevserverCli -Arguments @('disco', '--json', '--solution-dir', $SlnDir) 2>&1 | Out-String) | ConvertFrom-Json
    $hostPath = $discoJson.hostPath
    if (-not $hostPath -or -not (Test-Path $hostPath)) {
        Write-Log "Host not found at '$hostPath' — skipping --addins flag test"
        return
    }

    # Step 3: Launch Host with --addins, verify it starts (port 0 = auto-assign)
    # On non-Windows or when hostPath is a .dll, run via "dotnet <dll>".
    # On Windows with an .exe, run the .exe directly (it's a native AppHost).
    $stderrFile = [System.IO.Path]::GetTempFileName()
    $isExe = $hostPath.EndsWith('.exe', [System.StringComparison]::OrdinalIgnoreCase)
    $useDotnet = (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) -or (-not $isExe)
    if ($useDotnet -and $isExe) {
        # Swap .exe for .dll so dotnet can load it as a managed assembly
        $hostPath = [System.IO.Path]::ChangeExtension($hostPath, '.dll')
        if (-not (Test-Path $hostPath)) {
            Write-Log "Host DLL not found at '$hostPath' — skipping --addins flag test"
            return
        }
    }
    if ($useDotnet) {
        $hostProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
            $hostPath, '--httpPort', '0', '--addins', $addInsOutput
        ) -PassThru -NoNewWindow -RedirectStandardError $stderrFile
    } else {
        $hostProcess = Start-Process -FilePath $hostPath -ArgumentList @(
            '--httpPort', '0', '--addins', $addInsOutput
        ) -PassThru -NoNewWindow -RedirectStandardError $stderrFile
    }

    Start-Sleep -Seconds 5

    if ($hostProcess.HasExited -and $hostProcess.ExitCode -ne 0) {
        $stderrContent = Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue
        Remove-Item $stderrFile -ErrorAction SilentlyContinue
        throw "Host with --addins exited with error code $($hostProcess.ExitCode)`nSTDERR:`n$stderrContent"
    }

    if (-not $hostProcess.HasExited) {
        Stop-Process -Id $hostProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Remove-Item $stderrFile -ErrorAction SilentlyContinue

    Write-Log "Host --addins flag accepted successfully"
}

function Test-PackageContent {
    param([string]$PackagesDir)  # CI artifact directory containing .nupkg files

    Write-Log "Validating package contents"

    # Find Uno.WinUI.DevServer nupkg
    $devServerPkg = Get-ChildItem -Path $PackagesDir -Filter "Uno.WinUI.DevServer.*.nupkg" | Select-Object -First 1
    if (-not $devServerPkg) {
        Write-Log "Uno.WinUI.DevServer .nupkg not found in $PackagesDir — skipping package content test"
        return
    }

    $extractDir = Join-Path ([System.IO.Path]::GetTempPath()) "devserver-pkg-verify"
    if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }
    Expand-Archive -Path $devServerPkg.FullName -DestinationPath $extractDir

    # Verify both TFM Host binaries are present
    foreach ($tfm in @('net9.0', 'net10.0')) {
        $hostDll = Join-Path $extractDir "tools/rc/host/$tfm/Uno.UI.RemoteControl.Host.dll"
        if (-not (Test-Path $hostDll)) {
            throw "Package missing Host binary for $tfm at tools/rc/host/$tfm/"
        }
        Write-Log " Host binary present: tools/rc/host/$tfm/ ($(((Get-Item $hostDll).Length / 1KB).ToString('N0')) KB)"
    }

    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "Package content validated successfully"
}

$finalExitCode = 1
$devserverCleanupDirectory = $null

try {
    cd $env:BUILD_SOURCESDIRECTORY/src/SolutionTemplate
    & $env:BUILD_SOURCESDIRECTORY/build/test-scripts/update-uno-sdk-globaljson.ps1

    Write-Log "Starting devserver CLI test"

    if (-not $script:DevServerCliUsesDllPath) {
        Install-DevserverCliTool -PackagesDir $env:PACKAGES_DIR
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
    $maxAttempts = Get-ConfiguredIntValue -EnvironmentVariableName 'UNO_DEVSERVER_MAX_ATTEMPTS' -DefaultValue 30
    $skipLegacyStartupTests = Get-ConfiguredBooleanValue -EnvironmentVariableName 'UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS'

    # --- Phase 0 discovery tests ---
    Test-DiscoJsonOutput -SlnDir $slnDir
    Test-DiscoAddInsOnly -SlnDir $slnDir
    Test-CleanupCommand -SlnDir $slnDir
    Test-HostAddInsFlag -SlnDir $slnDir
    if ($env:PACKAGES_DIR) { Test-PackageContent -PackagesDir $env:PACKAGES_DIR }

    # --- Existing integration tests ---
    if ($skipLegacyStartupTests) {
        Write-Log "UNO_DEVSERVER_SKIP_LEGACY_STARTUP_TESTS=true. Skipping legacy start/stop and MCP script scenarios to focus on Codex validation."
    }
    else {
        $primaryPort = Test-DevserverStartStop -SlnDir $slnDir -CsprojDir $csprojDir -CsprojPath $csprojPath -DefaultPort $defaultPort -MaxAttempts $maxAttempts
        $solutionDirTestPort = Test-DevserverSolutionDirSupport -SlnDir $slnDir -DefaultPort $defaultPort -BaselinePort $primaryPort -MaxAttempts $maxAttempts
        Test-McpModeWithoutSolutionDir -SlnDir $slnDir -DefaultPort $defaultPort -PrimaryPort $primaryPort -SolutionDirPort $solutionDirTestPort -MaxAttempts $maxAttempts
        # +1 accounts for the port used internally by Test-McpModeWithoutSolutionDir
        $maxAllocatedPort = [Math]::Max($primaryPort, $solutionDirTestPort) + 1
        Test-McpModeWithRootsFallback -SlnDir $slnDir -DefaultPort $defaultPort -MaxAllocatedPort $maxAllocatedPort
        # Ensure a clean state before the Codex test — previous MCP tests may leave
        # behind a stale DevServer registration or lingering host process.
        Stop-DevserverInDirectory -Directory $slnDir
    }

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

    if ($script:DevServerCliToolPath -and (Test-Path $script:DevServerCliToolPath)) {
        Remove-Item -LiteralPath $script:DevServerCliToolPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit $finalExitCode
