# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

$version = "51.0.33"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${version}-preview.nupkg"

curl.exe -o win32.zip $url
tar.exe -xvf win32.zip Windows.Win32.winmd
dotnet run -o Windows.Win32.json.$version Windows.Win32.winmd

py $PSScriptRoot\split_namespace.py Windows.Win32.json.$version
