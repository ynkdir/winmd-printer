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

Get-ChildItem winrt\*.winmd | ForEach-Object { Write-Host $_.Name; cmd /c "dotnet run `"$_`" > `"winrt\$($_.BaseName).json`""}

Write-Output "make Windows.WinRT.json ..."
py -c "import json, glob; json.dump([td for f in glob.glob('winrt/*.json') for td in json.load(open(f))], open('Windows.WinRT.json', 'w'), indent=2)"

py $PSScriptRoot\split_namespace.py Windows.WinRT.json
