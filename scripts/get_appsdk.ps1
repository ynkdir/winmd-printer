# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

param(
    [Parameter(Mandatory=$true)]
    [string]$version
)

$ErrorActionPreference = "Stop"

function ExitOnError() {
    exit 1
}

$url = "https://api.nuget.org/v3-flatcontainer/microsoft.windowsappsdk/$version/microsoft.windowsappsdk.$version.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path appsdk)) {
    New-Item appsdk -ItemType Directory
} else {
    Remove-Item appsdk\*
}

$tmpdir = New-TemporaryFolder

curl.exe -f -o $tmpdir\appsdk.zip $url || ExitOnError

New-Item $tmpdir\appsdk -ItemType Directory
tar.exe -C $tmpdir\appsdk -xvf $tmpdir\appsdk.zip || ExitOnError

Get-Item $tmpdir\appsdk\lib\uap10.0\*.winmd, $tmpdir\appsdk\lib\uap10.0.18362\*.winmd | ForEach-Object {
    Write-Host $_.Name
    dotnet run -o "$tmpdir\$($_.BaseName).json" $_ || ExitOnError
}

Write-Host "make WindowsAppSDK.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o WindowsAppSDK.json (Get-Item $tmpdir\*.json) || ExitOnError

py -X utf8 $PSScriptRoot\split_namespace.py -d appsdk WindowsAppSDK.json || ExitOnError

Remove-Item -Recurse $tmpdir
