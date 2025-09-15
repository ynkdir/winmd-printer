. $PSScriptRoot\common.ps1

function Get-Metadata {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$MinVersion,
        [Parameter(Mandatory)]
        [bool]$AllowPrerelease
    )

    $MinVersionNumbers = [System.Version]$MinVersion

    foreach ($version in Get-NugetIndex $Name) {
        if ($version -match "-" -and -not $AllowPrerelease) {
            continue
        }

        $versionNumbers = [System.Version]"$($version -replace '-.*', '')"
        if ($versionNumbers -lt $minVersionNumbers) {
            continue
        }

        if (Test-Path "metadata/$name.$version") {
            continue
        }

        switch ($Name) {
            "Microsoft.Windows.SDK.Win32Metadata" {
                & $PSScriptRoot\get_win32.ps1 $version
            }
            "Microsoft.Windows.SDK.Contracts" {
                & $PSScriptRoot\get_winrt.ps1 $version
            }
            "Microsoft.WindowsAppSDK" {
                if ($versionNumbers -ge [System.Version]"1.8") {
                    & $PSScriptRoot\get_appsdk1.8.ps1 $version
                } else {
                    & $PSScriptRoot\get_appsdk.ps1 $version
                }
            }
            "Microsoft.Web.WebView2" {
                & $PSScriptRoot\get_webview2.ps1 $version
            }
            "Microsoft.Graphics.Win2D" {
                & $PSScriptRoot\get_win2d.ps1 $version
            }
            default {
                throw "Unknown package $Name"
            }
        }
    }
}

function Main {
    $ErrorActionPreference = "Stop"
    $PSNativeCommandUseErrorActionPreference = $true

    Get-Metadata "Microsoft.Windows.SDK.Win32Metadata" "63.0" $true
    Get-Metadata "Microsoft.Windows.SDK.Contracts" "10.0.26100.4948" $false
    Get-Metadata "Microsoft.WindowsAppSDK" "1.5" $false
    Get-Metadata "Microsoft.Web.WebView2" "1.0.2651" $false
    Get-Metadata "Microsoft.Graphics.Win2D" "1.2.0" $false
}

Main
