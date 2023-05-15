# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

$version = "1.3.230502000"
$url = "https://globalcdn.nuget.org/packages/microsoft.windowsappsdk.$version.nupkg"

if (-not (Test-Path appsdk)) {
    New-Item appsdk -ItemType Directory
}

if (Test-Path __tmp) {
    Remove-Item -Recurse __tmp
}
New-Item __tmp -ItemType Directory
New-Item __tmp\appsdk -ItemType Directory

Invoke-WebRequest $url -OutFile appsdk.zip
Expand-Archive appsdk.zip -DestinationPath __tmp\appsdk
Copy-Item __tmp\appsdk\lib\uap10.0\*.winmd __tmp
Copy-Item __tmp\appsdk\lib\uap10.0.18362\*.winmd __tmp

Get-Item __tmp\*.winmd | ForEach-Object { Write-Host $_.Name; dotnet run -o "__tmp\$($_.BaseName).json" $_ }

Write-Host "make appsdk.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o appsdk.json (Get-Item __tmp/*.json)

py -X utf8 $PSScriptRoot\split_namespace.py -d appsdk appsdk.json
