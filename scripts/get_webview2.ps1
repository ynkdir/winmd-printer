# https://www.nuget.org/packages/Microsoft.Web.WebView2

param(
    [Parameter(Mandatory = $true)]
    [string]$version
)

$ErrorActionPreference = "Stop"

function ExitOnError() {
    exit 1
}

$url = "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$version/microsoft.web.webview2.$version.nupkg"

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

if (-not (Test-Path webview2)) {
    New-Item webview2 -ItemType Directory
}
else {
    Remove-Item webview2\*
}

$tmpdir = New-TemporaryFolder

curl.exe -f -o $tmpdir\webview2.zip $url || ExitOnError

tar.exe -C $tmpdir -xvf $tmpdir\webview2.zip lib/Microsoft.Web.WebView2.Core.winmd || ExitOnError

dotnet run -o Microsoft.Web.WebView2.Core.json $tmpdir\lib\Microsoft.Web.WebView2.Core.winmd

py -X utf8 $PSScriptRoot\split_namespace.py -d webview2 Microsoft.Web.WebView2.Core.json || ExitOnError

Remove-Item -Recurse $tmpdir
