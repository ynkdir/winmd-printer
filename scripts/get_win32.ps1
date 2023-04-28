$winmdver = "50.0.71"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.win32metadata.${winmdver}-preview.nupkg"
curl.exe -o winmd.zip $url
tar.exe -xvf winmd.zip Windows.Win32.winmd
cmd /c "dotnet run Windows.Win32.winmd > Windows.Win32.json.$winmdver"
rm winmd.zip

py scripts\split_namespace.py Windows.Win32.json.$winmdver
