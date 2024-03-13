# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

$version = "1.5.240311000"
$url = "https://globalcdn.nuget.org/packages/microsoft.windowsappsdk.$version.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path appsdk)) {
    New-Item appsdk -ItemType Directory
}

$tmpdir = New-TemporaryFolder

curl.exe -o $tmpdir\appsdk.zip $url

New-Item $tmpdir\appsdk -ItemType Directory
tar.exe -C $tmpdir\appsdk -xvf $tmpdir\appsdk.zip

Get-Item $tmpdir\appsdk\lib\uap10.0\*.winmd, $tmpdir\appsdk\lib\uap10.0.18362\*.winmd | ForEach-Object {
    Write-Host $_.Name
    dotnet run -o "$tmpdir\$($_.BaseName).json" $_
}

Write-Host "make WindowsAppSDK.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o WindowsAppSDK.json (Get-Item $tmpdir\*.json)

py -X utf8 $PSScriptRoot\split_namespace.py -d appsdk WindowsAppSDK.json

Remove-Item -Recurse $tmpdir
