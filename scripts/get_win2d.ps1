# https://www.nuget.org/packages/Microsoft.Graphics.Win2D

param(
    [Parameter(Mandatory=$true)]
    [string]$version
)

$ErrorActionPreference = "Stop"

function ExitOnError() {
    exit 1
}

$url = "https://api.nuget.org/v3-flatcontainer/microsoft.graphics.win2d/$version/microsoft.graphics.win2d.$version.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path win2d)) {
    New-Item win2d -ItemType Directory
} else {
    Remove-Item win2d\*
}

$tmpdir = New-TemporaryFolder

curl.exe -f -o $tmpdir\win2d.zip $url || ExitOnError

tar.exe -C $tmpdir -xvf $tmpdir\win2d.zip lib/uap10.0/Microsoft.Graphics.Canvas.winmd || ExitOnError

dotnet run -o Microsoft.Graphics.Canvas.json $tmpdir\lib\uap10.0\Microsoft.Graphics.Canvas.winmd

py -X utf8 $PSScriptRoot\split_namespace.py -d win2d Microsoft.Graphics.Canvas.json || ExitOnError

Remove-Item -Recurse $tmpdir
