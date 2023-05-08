# https://www.nuget.org/packages/Microsoft.Windows.SDK.Win32Metadata/

$version = "51.0.33"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${version}-preview.nupkg"

curl.exe -o win32.zip $url
tar.exe -xvf win32.zip Windows.Win32.winmd
cmd /c "dotnet run Windows.Win32.winmd > Windows.Win32.json.$version"

py $PSScriptRoot\split_namespace.py Windows.Win32.json.$version
