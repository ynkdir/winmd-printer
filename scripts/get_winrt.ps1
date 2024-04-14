# https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts

param(
    [Parameter(Mandatory=$true)]
    [string]$version
)

$url = "https://globalcdn.nuget.org/packages/microsoft.windows.sdk.contracts.$version.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path winrt)) {
    New-Item winrt -ItemType Directory
}

$tmpdir = New-TemporaryFolder

curl.exe -o $tmpdir\winrt.zip $url

New-Item $tmpdir\winrt -ItemType Directory
tar.exe -C $tmpdir\winrt -xvf $tmpdir\winrt.zip

Get-Item $tmpdir\winrt\ref\netstandard2.0\*.winmd | ForEach-Object {
    Write-Host $_.Name
    dotnet run -o "$tmpdir\$($_.BaseName).json" $_
}

Write-Host "make WindowsSDK.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o WindowsSDK.json (Get-Item $tmpdir\*.json)

py -X utf8 $PSScriptRoot\split_namespace.py -d winrt WindowsSDK.json

Remove-Item -Recurse $tmpdir
