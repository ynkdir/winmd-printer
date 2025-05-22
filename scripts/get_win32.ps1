# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

param(
    [Parameter(Mandatory = $true)]
    [string]$version
)

$ErrorActionPreference = "Stop"

function ExitOnError() {
    exit 1
}

$url = "https://api.nuget.org/v3-flatcontainer/microsoft.windows.sdk.win32metadata/$version-preview/microsoft.windows.sdk.win32metadata.$version-preview.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path win32)) {
    New-Item win32 -ItemType Directory
}
else {
    Remove-Item win32\*
}

$tmpdir = New-TemporaryFolder

curl.exe -o $tmpdir\win32.zip $url || ExitOnError

tar.exe -C $tmpdir -xvf $tmpdir\win32.zip Windows.Win32.winmd || ExitOnError

dotnet run -o Windows.Win32.json $tmpdir\Windows.Win32.winmd || ExitOnError

py -X utf8 $PSScriptRoot\split_namespace.py -d win32 Windows.Win32.json || ExitOnError

Remove-Item -Recurse $tmpdir
