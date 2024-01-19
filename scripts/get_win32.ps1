# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

$version = "57.0.42"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${version}-preview.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path win32)) {
    New-Item win32 -ItemType Directory
}

$tmpdir = New-TemporaryFolder

curl.exe -o $tmpdir\win32.zip $url

tar.exe -C $tmpdir -xvf $tmpdir\win32.zip Windows.Win32.winmd

dotnet run -o Windows.Win32.json $tmpdir\Windows.Win32.winmd

py -X utf8 $PSScriptRoot\split_namespace.py -d win32 Windows.Win32.json

Remove-Item -Recurse $tmpdir
