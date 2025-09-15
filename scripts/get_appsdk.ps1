# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.WindowsAppSDK",
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

    Get-Item $tmpdir\$Name.$Version\lib\uap10.0\*.winmd, $tmpdir\$Name.$Version\lib\uap10.0.18362\*.winmd | ForEach-Object {
        Write-Host $_.Name
        dotnet run -o "$tmpdir\$($_.BaseName).json" $_
    }

    py -X utf8 $PSScriptRoot\join_metadata.py -o $tmpdir\$Name.json (Get-Item $tmpdir\*.json)

    py -X utf8 $PSScriptRoot\split_namespace.py -d $DstDir $tmpdir\$Name.json

    tar.exe -C $DstDir -acf "$Name.$version.zip" *

    Remove-Item -Recurse $tmpdir
}

Main
