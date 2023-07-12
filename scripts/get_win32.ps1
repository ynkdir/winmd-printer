# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

$version = "53.0.14"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${version}-preview.nupkg"

curl.exe -o win32.zip $url
tar.exe -xvf win32.zip Windows.Win32.winmd
dotnet run -o Windows.Win32.json.$version Windows.Win32.winmd

py -X utf8 $PSScriptRoot\split_namespace.py -d win32 Windows.Win32.json.$version
