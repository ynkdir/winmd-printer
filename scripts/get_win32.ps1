$winmdver = "48.0.19"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${winmdver}-preview.nupkg"
curl.exe -o winmd.zip $url
tar.exe -xvf winmd.zip Windows.Win32.winmd
cmd /c "dotnet run Windows.Win32.winmd > Windows.Win32.json.$winmdver"
rm winmd.zip
