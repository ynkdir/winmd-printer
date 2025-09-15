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

    dotnet.exe run -o $tmpdir\Windows.Win32.json $tmpdir\$Name.$Version\Windows.Win32.winmd

    py.exe -X utf8 $PSScriptRoot\split_namespace.py -d $DstDir $tmpdir\Windows.Win32.json

    Remove-Item -Recurse $tmpdir
}

Main
