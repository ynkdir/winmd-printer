# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.Windows.SDK.Win32Metadata",
    [string]$DstDir = "metadata\$Name.$Version"
)

. $PSScriptRoot\common.ps1

function Main {
    $ErrorActionPreference = "Stop"
    $PSNativeCommandUseErrorActionPreference = $true

    if (-not (Test-Path $DstDir)) {
        New-Item $DstDir -ItemType Directory
    }

    $tmpdir = New-TemporaryFolder

    Download-NugetPackage $Name $Version $tmpdir\$Name.$Version

    $winmdfiles = (Get-Item $tmpdir\$Name.$Version\Windows.Win32.winmd)

    dotnet.exe run -d $DstDir $winmdfiles

    tar.exe -C $DstDir -acf "$Name.$version.zip" *

    Remove-Item -Recurse $tmpdir
}

Main
