[CmdletBinding()]
param([Parameter(Mandatory=$true)]
      [string]$buildNumber)

# Ensure the error action preference is set to the default for PowerShell3, 'Stop'
$ErrorActionPreference = 'Stop'

# Constants
$WindowsSDKOptions = @("OptionId.UWPCpp")
$WindowsSDKRegPath = "HKLM:\Software\Microsoft\Windows Kits\Installed Roots"
$WindowsSDKRegRootKey = "KitsRoot10"
$WindowsSDKVersion = "10.0.$buildNumber.0"
$WindowsSDKInstalledRegPath = "$WindowsSDKRegPath\$WindowsSDKVersion\Installed Options"
$StrongNameRegPath = "HKLM:\SOFTWARE\Microsoft\StrongName\Verification"
$PublicKeyTokens = @("31bf3856ad364e35")

function Download-File
{
    param ([string] $outDir,
           [string] $downloadUrl,
           [string] $downloadName)

    $downloadPath = Join-Path $outDir "$downloadName.download"
    $downloadDest = Join-Path $outDir $downloadName
    $downloadDestTemp = Join-Path $outDir "$downloadName.tmp"

    Write-Host -NoNewline "Downloading $downloadName..."

    try
    {
        $webclient = new-object System.Net.WebClient
        $webclient.DownloadFile($downloadUrl, $downloadPath)
    }
    catch [System.Net.WebException]
    {
        Write-Host
        Write-Warning "Failed to fetch updated file from $downloadUrl"
        if (!(Test-Path $downloadDest))
        {
            throw "$downloadName was not found at $downloadDest"
        }
        else
        {
            Write-Warning "$downloadName may be out of date"
        }
    }

    Unblock-File $downloadPath

    $downloadDestTemp = $downloadPath;

    # Delete and rename to final dest
    if (Test-Path -PathType Container $downloadDest)
    {
        [System.IO.Directory]::Delete($downloadDest, $true)
    }

    Move-Item -Force $downloadDestTemp $downloadDest
    Write-Host "Done"

    return $downloadDest
}

function Get-ISODriveLetter
{
    param ([string] $isoPath)

    $diskImage = Get-DiskImage -ImagePath $isoPath
    if ($diskImage)
    {
        $volume = Get-Volume -DiskImage $diskImage

        if ($volume)
        {
            $driveLetter = $volume.DriveLetter
            if ($driveLetter)
            {
                $driveLetter += ":"
                return $driveLetter
            }
        }
    }

    return $null
}

function Mount-ISO
{
    param ([string] $isoPath)

    # Check if image is already mounted
    $isoDrive = Get-ISODriveLetter $isoPath

    if (!$isoDrive)
    {
        Mount-DiskImage -ImagePath $isoPath -StorageType ISO | Out-Null
    }

    $isoDrive = Get-ISODriveLetter $isoPath
    Write-Verbose "$isoPath mounted to ${isoDrive}:"
}

function Dismount-ISO
{
    param ([string] $isoPath)

    $isoDrive = (Get-DiskImage -ImagePath $isoPath | Get-Volume).DriveLetter

    if ($isoDrive)
    {
        Write-Verbose "$isoPath dismounted"
        Dismount-DiskImage -ImagePath $isoPath | Out-Null
    }
}

function Disable-StrongName
{
    param ([string] $publicKeyToken = "*")

    reg ADD "HKLM\SOFTWARE\Microsoft\StrongName\Verification\*,$publicKeyToken" /f | Out-Null
    if ($env:PROCESSOR_ARCHITECTURE -eq "AMD64")
    {
        reg ADD "HKLM\SOFTWARE\Wow6432Node\Microsoft\StrongName\Verification\*,$publicKeyToken" /f | Out-Null
    }
}

function Test-Admin
{
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal $identity
    $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-RegistryPathAndValue
{
    param (
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $path,
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $value)

    try
    {
        if (Test-Path $path)
        {
            Get-ItemProperty -Path $path | Select-Object -ExpandProperty $value -ErrorAction Stop | Out-Null
            return $true
        }
    }
    catch
    {
    }

    return $false
}

function Test-InstallWindowsSDK
{
    $retval = $true

    if (Test-RegistryPathAndValue -Path $WindowsSDKRegPath -Value $WindowsSDKRegRootKey)
    {
        # A Windows SDK is installed
        # Is an SDK of our version installed with the options we need?
        if (Test-RegistryPathAndValue -Path $WindowsSDKInstalledRegPath -Value "$WindowsSDKOptions")
        {
            # It appears we have what we need. Double check the disk
            $sdkRoot = Get-ItemProperty -Path $WindowsSDKRegPath | Select-Object -ExpandProperty $WindowsSDKRegRootKey
            if ($sdkRoot)
            {
                if (Test-Path $sdkRoot)
                {
                    $refPath = Join-Path $sdkRoot "References\$WindowsSDKVersion"
                    if (Test-Path $refPath)
                    {
                        $umdPath = Join-Path $sdkRoot "UnionMetadata\$WindowsSDKVersion"
                        if (Test-Path $umdPath)
                        {
                            # Pretty sure we have what we need
                            $retval = $false
                        }
                    }
                }
            }
        }
    }

    return $retval
}

function Test-InstallStrongNameHijack
{
    foreach($publicKeyToken in $PublicKeyTokens)
    {
        $key = "$StrongNameRegPath\*,$publicKeyToken"
        if (!(Test-Path $key))
        {
            return $true
        }
    }

    return $false
}

Write-Host -NoNewline "Checking for installed Windows SDK $WindowsSDKVersion..."
$InstallWindowsSDK = Test-InstallWindowsSDK
if ($InstallWindowsSDK)
{
    Write-Host "Installation required"
}
else
{
    Write-Host "INSTALLED"
}

$StrongNameHijack = Test-InstallStrongNameHijack
Write-Host -NoNewline "Checking if StrongName bypass required..."

if ($StrongNameHijack)
{
    Write-Host "REQUIRED"
}
else
{
    Write-Host "Done"
}

if ($StrongNameHijack -or $InstallWindowsSDK)
{
    if (!(Test-Admin))
    {
        Write-Host
        throw "ERROR: Elevation required"
    }
}

if ($InstallWindowsSDK)
{
    # Static(ish) link for Windows SDK
    # Note: there is a delay from Windows SDK announcements to availability via the static link
    $uri = "https://go.microsoft.com/fwlink/?prd=11966&pver=1.0&plcid=0x409&clcid=0x409&ar=Flight&sar=Sdsurl&o1=$buildNumber"

    if($buildNumber -eq 19041)
    {
        # Workaround for missing SDK
        $uri = "https://software-download.microsoft.com/download/pr/19041.1.191206-1406.vb_release_WindowsSDK.iso";
    }

    if ($env:TEMP -eq $null)
    {
        $env:TEMP = Join-Path $env:SystemDrive 'temp'
    }

    $winsdkTempDir = Join-Path $env:TEMP "WindowsSDK"

    if (![System.IO.Directory]::Exists($winsdkTempDir))
    {
        [void][System.IO.Directory]::CreateDirectory($winsdkTempDir)
    }

    $file = "winsdk_$buildNumber.iso"

    Write-Verbose "Getting WinSDK from $uri"
    $downloadFile = Download-File $winsdkTempDir $uri $file

    # TODO Check if zip, exe, iso, etc.
    try
    {
        Write-Host -NoNewline "Mounting ISO $file..."
        Mount-ISO $downloadFile
        Write-Host "Done"

        $isoDrive = Get-ISODriveLetter $downloadFile

        if (Test-Path $isoDrive)
        {
            Write-Host -NoNewLine "Installing WinSDK..."

            $setupPath = Join-Path "$isoDrive" "WinSDKSetup.exe"
            Start-Process -Wait $setupPath "/features $WindowsSDKOptions /q"
            Write-Host "Done"
        }
        else
        {
            throw "Could not find mounted ISO at ${isoDrive}"
        }
    }
    finally
    {
        Write-Host -NoNewline "Dismounting ISO $file..."
        #Dismount-ISO $downloadFile
        Write-Host "Done"
    }
}

if ($StrongNameHijack)
{
    Write-Host -NoNewline "Disabling StrongName for Windows SDK..."

    foreach($key in $PublicKeyTokens)
    {
        Disable-StrongName $key
    }

    Write-Host "Done"
}
