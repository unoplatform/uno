Set-PSDebug -Trace 1

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

    if (-not (Test-Path $csprojDir)) {
        throw "Project directory not found: $csprojDir"
    }

    Set-Location $slnDir

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

    & dotnet uno-devserver stop

    exit 0
}
catch {
    Write-Log "ERROR: $($_.Exception.Message)"
    try { Pop-Location -ErrorAction SilentlyContinue; Pop-Location -ErrorAction SilentlyContinue } catch {}
    exit 1
}
