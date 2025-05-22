# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

param(
    [Parameter(Mandatory = $true)]
    [string]$version
)

$ErrorActionPreference = "Stop"

function ExitOnError() {
    exit 1
}

function New-TemporaryFolder() {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    return New-Item -Path $tmpfile -ItemType directory
}

function DownloadPackage($id, $version) {
    # Two packages can have same package dependency with different version.
    if (Test-Path $tmpdir\$id) {
        return
    }

    $id2 = $id.ToLower()
    $version2 = "$version" -replace '[\[\]]', ''
    $url = "https://api.nuget.org/v3-flatcontainer/$id2/$version2/$id2.$version2.nupkg"

    curl.exe -f -o $tmpdir\$id.zip $url || ExitOnError

    New-Item $tmpdir\$id -ItemType Directory
    tar.exe -C $tmpdir\$id -xvf $tmpdir\$id.zip || ExitOnError
}

function DownloadDependencies($id) {
    $xml = [xml]::new()
    $xml.Load("$tmpdir\$id\$id.nuspec")

    if ($null -eq $xml.package.metadata.dependencies.dependency) {
        return
    }

    foreach ($dependency in $xml.package.metadata.dependencies.dependency) {
        DownloadPackage $dependency.id $dependency.version
    }

    foreach ($dependency in $xml.package.metadata.dependencies.dependency) {
        DownloadDependencies $dependency.id
    }
}

function CheckDependencies($id, $known_dependencies) {
    $xml = [xml]::new()
    $xml.Load("$tmpdir\$id\$id.nuspec")
    $current_dependencies = $xml.package.metadata.dependencies.dependency | ForEach-Object { $_.id }

    $diff = Compare-Object $known_dependencies $current_dependencies
    if ($diff) {
        Write-Host $diff
        throw "dependencies was changed"
    }
}

if (-not (Test-Path appsdk)) {
    New-Item appsdk -ItemType Directory
}
else {
    Remove-Item appsdk\*
}

$tmpdir = New-TemporaryFolder

DownloadPackage "Microsoft.WindowsAppSDK" $version

CheckDependencies "Microsoft.WindowsAppSDK" @(
    "Microsoft.WindowsAppSDK.Base",
    "Microsoft.WindowsAppSDK.Foundation",
    "Microsoft.WindowsAppSDK.InteractiveExperiences",
    "Microsoft.WindowsAppSDK.WinUI",
    "Microsoft.WindowsAppSDK.DWrite",
    "Microsoft.WindowsAppSDK.Widgets",
    "Microsoft.WindowsAppSDK.AI",
    "Microsoft.WindowsAppSDK.Packages"
)

DownloadDependencies "Microsoft.WindowsAppSDK"

$winmdfiles = (Get-Item $tmpdir\Microsoft.WindowsAppSDK.AI\metadata\*.winmd,
    $tmpdir\Microsoft.WindowsAppSDK.Foundation\metadata\*.winmd,
    $tmpdir\Microsoft.WindowsAppSDK.InteractiveExperiences\metadata\10.0.18362.0\*.winmd,
    $tmpdir\Microsoft.WindowsAppSDK.Widgets\metadata\*.winmd,
    $tmpdir\Microsoft.WindowsAppSDK.WinUI\metadata\*.winmd)

foreach ($f in $winmdfiles) {
    Write-Host $f.Name
    dotnet run -o "$tmpdir\$($f.BaseName).json" $f || ExitOnError
}

Write-Host "make WindowsAppSDK.json ..."
py -X utf8 $PSScriptRoot\join_metadata.py -o WindowsAppSDK.json (Get-Item $tmpdir\*.json) || ExitOnError

py -X utf8 $PSScriptRoot\split_namespace.py -d appsdk WindowsAppSDK.json || ExitOnError

Remove-Item -Recurse $tmpdir
