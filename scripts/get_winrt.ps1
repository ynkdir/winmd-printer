# https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.Windows.SDK.Contracts",
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

    Get-Item $tmpdir\$Name.$Version\ref\netstandard2.0\*.winmd | ForEach-Object {
        Write-Host $_.Name
        dotnet.exe run -o "$tmpdir\$($_.BaseName).json" $_
    }

    py.exe -X utf8 $PSScriptRoot\join_metadata.py -o $tmpdir\$Name.$Version.json (Get-Item $tmpdir\*.json)

    py.exe -X utf8 $PSScriptRoot\split_namespace.py -d $DstDir $tmpdir\$Name.$Version.json

    tar.exe -C $DstDir -acf "$Name.$version.zip" *

    Remove-Item -Recurse $tmpdir
}

Main
