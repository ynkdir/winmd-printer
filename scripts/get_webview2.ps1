# https://www.nuget.org/packages/Microsoft.Web.WebView2

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.Web.WebView2",
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

    $winmdfiles = (Get-Item $tmpdir\$Name.$Version\lib\Microsoft.Web.WebView2.Core.winmd)

    dotnet.exe run -d $DstDir $winmdfiles

    tar.exe -C $DstDir -acf "$Name.$version.zip" *

    Remove-Item -Recurse $tmpdir
}

Main
