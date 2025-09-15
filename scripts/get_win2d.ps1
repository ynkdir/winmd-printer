# https://www.nuget.org/packages/Microsoft.Graphics.Win2D

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.Graphics.Win2D",
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

    dotnet.exe run -o $tmpdir\Microsoft.Graphics.Canvas.json $tmpdir\$Name.$Version\lib\uap10.0\Microsoft.Graphics.Canvas.winmd

    py.exe -X utf8 $PSScriptRoot\split_namespace.py -d $DstDir $tmpdir\Microsoft.Graphics.Canvas.json

    Remove-Item -Recurse $tmpdir
}

Main
