# https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts

$version = "10.0.22621.2"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.contracts.$version.nupkg"

if (-not (Test-Path winrt)) {
    New-Item winrt -ItemType Directory
}

if (Test-Path __tmp) {
    Remove-Item -Recurse __tmp
}
New-Item __tmp -ItemType Directory
New-Item __tmp\winrt -ItemType Directory


curl.exe -o winrt.zip $url
tar.exe -C __tmp\winrt -xvf winrt.zip
Copy-Item __tmp\winrt\ref\netstandard2.0\*.winmd __tmp\

Get-Item __tmp\*.winmd | ForEach-Object { Write-Host $_.Name; dotnet run -o "__tmp\$($_.BaseName).json" $_ }

Write-Host "make Windows.WinRT.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o Windows.WinRT.json (Get-Item __tmp/*.json)

py -X utf8 $PSScriptRoot\split_namespace.py -d winrt Windows.WinRT.json
