# https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts

$version = "10.0.22621.2"
$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.contracts.$version.nupkg"

if (-not (Test-Path winrt)) {
    New-Item winrt -ItemType Directory
}
Remove-Item winrt\*

curl.exe -o winrt.zip $url
tar.exe -C winrt -xvf winrt.zip 'ref/netstandard2.0/*.winmd'
Move-Item winrt\ref\netstandard2.0\*.winmd winrt\
Remove-Item -Recurse winrt\ref

Get-ChildItem winrt\*.winmd | ForEach-Object { Write-Host $_.Name; dotnet run -o "winrt\$($_.BaseName).json" $_ }

Write-Host "make Windows.WinRT.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o Windows.WinRT.json (Get-Item winrt/*.json)

py -X utf8 $PSScriptRoot\split_namespace.py -d json Windows.WinRT.json
