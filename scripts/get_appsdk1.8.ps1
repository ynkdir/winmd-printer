# https://www.nuget.org/packages/Microsoft.WindowsAppSDK

param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Name = "Microsoft.WindowsAppSDK",
    [string]$DstDir = "metadata\$Name.$Version"
)

. $PSScriptRoot\common.ps1

function Download-Dependencies($nuspec, $tmpdir) {
    $xml = [xml]::new()
    $xml.Load($nuspec)

    if ($null -eq $xml.package.metadata.dependencies.dependency) {
        return
    }

    foreach ($dependency in $xml.package.metadata.dependencies.dependency) {
        $id = $dependency.id
        $version = $dependency.version -replace '[\[\]]', ''
        Download-NugetPackage $dependency.id $version $tmpdir\$id
    }
}

function Check-Dependencies($nuspec, $known_dependencies) {
    $xml = [xml]::new()
    $xml.Load($nuspec)
    $current_dependencies = $xml.package.metadata.dependencies.dependency | ForEach-Object { $_.id }

    $diff = Compare-Object $known_dependencies $current_dependencies
    if ($diff) {
        Write-Host $diff
        throw "dependencies was changed"
    }
}

function Main {
    $ErrorActionPreference = "Stop"
    $PSNativeCommandUseErrorActionPreference = $true

    if (-not (Test-Path $DstDir)) {
        New-Item $DstDir -ItemType Directory
    }

    $tmpdir = New-TemporaryFolder

    Download-NugetPackage $Name $Version $tmpdir\$Name.$Version

    if ([System.Version]$Version -ge [System.Version]"1.8.250916003") {
        Check-Dependencies $tmpdir\$Name.$Version\$Name.nuspec @(
            "Microsoft.WindowsAppSDK.Base",
            "Microsoft.WindowsAppSDK.Foundation",
            "Microsoft.WindowsAppSDK.InteractiveExperiences",
            "Microsoft.WindowsAppSDK.WinUI",
            "Microsoft.WindowsAppSDK.DWrite",
            "Microsoft.WindowsAppSDK.Widgets",
            "Microsoft.WindowsAppSDK.AI",
            "Microsoft.WindowsAppSDK.Runtime",
            "Microsoft.WindowsAppSDK.ML"
        )

        Download-Dependencies $tmpdir\$Name.$Version\$Name.nuspec $tmpdir

        $winmdfiles = (Get-Item $tmpdir\Microsoft.WindowsAppSDK.AI\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.Foundation\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.InteractiveExperiences\metadata\10.0.18362.0\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.Widgets\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.WinUI\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.ML\metadata\*.winmd)
    } else {
        Check-Dependencies $tmpdir\$Name.$Version\$Name.nuspec @(
            "Microsoft.WindowsAppSDK.Base",
            "Microsoft.WindowsAppSDK.Foundation",
            "Microsoft.WindowsAppSDK.InteractiveExperiences",
            "Microsoft.WindowsAppSDK.WinUI",
            "Microsoft.WindowsAppSDK.DWrite",
            "Microsoft.WindowsAppSDK.Widgets",
            "Microsoft.WindowsAppSDK.AI",
            "Microsoft.WindowsAppSDK.Runtime"
        )

        Download-Dependencies $tmpdir\$Name.$Version\$Name.nuspec $tmpdir

        $winmdfiles = (Get-Item $tmpdir\Microsoft.WindowsAppSDK.AI\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.Foundation\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.InteractiveExperiences\metadata\10.0.18362.0\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.Widgets\metadata\*.winmd,
            $tmpdir\Microsoft.WindowsAppSDK.WinUI\metadata\*.winmd)
    }

    foreach ($f in $winmdfiles) {
        Write-Host $f.Name
        dotnet.exe run -o "$tmpdir\$($f.BaseName).json" $f
    }

    py.exe -X utf8 $PSScriptRoot\join_metadata.py -o $tmpdir\$Name.json (Get-Item $tmpdir\*.json)

    py.exe -X utf8 $PSScriptRoot\split_namespace.py -d $DstDir $tmpdir\$Name.json

    tar.exe -C $DstDir -acf "$Name.$version.zip" *

    Remove-Item -Recurse $tmpdir
}

Main
