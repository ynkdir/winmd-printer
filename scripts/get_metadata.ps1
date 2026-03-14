param(
    [string]$Id,
    [string]$Version,
    [string]$DstDir = "metadata"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

function New-TemporaryFolder {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    New-Item -Path $tmpfile -ItemType directory
}

function Get-NupkgIndex {
    param (
        [Parameter(Mandatory)]
        [string]$Id
    )

    $uri = "https://api.nuget.org/v3-flatcontainer/$($Id.ToLower())/index.json"
    $response = Invoke-RestMethod -Uri $uri
    return $response.versions
}

function Get-NupkgNames {
    param(
        [Parameter(Mandatory)]
        [string]$NupkgDir
    )

    return Get-ChildItem $NupkgDir | ForEach-Object { $_.Name -replace "\.\d.*", "" }
}

function Get-NupkgPath {
    param(
        [Parameter(Mandatory)]
        [string]$NupkgDir,
        [Parameter(Mandatory)]
        [string]$Id
    )

    foreach ($p in (Get-ChildItem $NupkgDir)) {
        $nupkg_name = $p.Name -replace "\.\d.*", ""
        if ($nupkg_name -eq $Id) {
            return $p
        }
    }

    throw "cannot find $Id"
}

function Get-KnownPackages {
    param(
        [Parameter(Mandatory)]
        [hashtable]$recipe
    )

    $r = $recipe.KnownPackages
    foreach ($recipe_id in $recipe.Dependencies) {
        $r += Get-KnownPackages $RECIPES[$recipe_id]
    }
    return $r
}

function Get-Winmdfiles {
    param(
        [Parameter(Mandatory)]
        [string]$NupkgDir,
        [Parameter(Mandatory)]
        [hashtable]$recipe
    )

    $r = @()
    foreach ($m in $recipe.Metadata) {
        $nupkg_path = Get-NupkgPath $NupkgDir $m.Id
        $r += Get-Item "$nupkg_path\$($m.Pattern)"
    }
    return $r
}

function Get-Recipe {
    param(
        [Parameter(Mandatory)]
        [string]$Id,
        [Parameter(Mandatory)]
        [string]$Version
    )

    if ($Id -eq "Microsoft.Windows.SDK.Win32Metadata") {
        return $RECIPES["Microsoft.Windows.SDK.Win32Metadata"]
    }
    elseif ($Id -eq "Microsoft.Windows.SDK.Contracts") {
        return $RECIPES["Microsoft.Windows.SDK.Contracts"]
    }
    elseif ($Id -eq "Microsoft.WindowsAppSDK") {
        if ([System.Version]$Version -lt [System.Version]"1.6.0") {
            return $RECIPES["Microsoft.WindowsAppSDK.1.5"]
        }
        elseif ([System.Version]$Version -lt [System.Version]"1.7.0") {
            return $RECIPES["Microsoft.WindowsAppSDK.1.6"]
        }
        elseif ([System.Version]$Version -lt [System.Version]"1.8.0") {
            return $RECIPES["Microsoft.WindowsAppSDK.1.7"]
        }
        elseif ([System.Version]$Version -lt [System.Version]"1.8.250916003") {
            return $RECIPES["Microsoft.WindowsAppSDK.1.8"]
        }
        else {
            return $RECIPES["Microsoft.WindowsAppSDK.1.8.250916003"]
        }
    }
    elseif ($Id -eq "Microsoft.Web.WebView2") {
        return $RECIPES["Microsoft.Web.WebView2"]
    }
    elseif ($Id -eq "Microsoft.Graphics.Win2D") {
        if ([System.Version]$Version -lt [System.Version]"1.3.0") {
            return $RECIPES["Microsoft.Graphics.Win2D.1.2"]
        }
        else {
            return $RECIPES["Microsoft.Graphics.Win2D.1.3"]
        }
    }

    throw "cannot find $Id.$Version"
}

$RECIPES = @{
    "Microsoft.Windows.SDK.Win32Metadata"   = @{
        Id            = "Microsoft.Windows.SDK.Win32Metadata"
        Dependencies  = @()
        KnownPackages = @("Microsoft.Windows.SDK.Win32Metadata")
        Metadata      = @(
            @{Id = "Microsoft.Windows.SDK.Win32Metadata"; Pattern = "Windows.Win32.winmd" }
        )
    }
    "Microsoft.Windows.SDK.Contracts"       = @{
        Id            = "Microsoft.Windows.SDK.Contracts"
        Dependencies  = @()
        KnownPackages = @(
            "Microsoft.Windows.SDK.Contracts"
            "System.Runtime.WindowsRuntime"
            "System.Runtime.InteropServices.WindowsRuntime"
            "System.Runtime.WindowsRuntime.UI.Xaml"
        )
        Metadata      = @(
            @{Id = "Microsoft.Windows.SDK.Contracts"; Pattern = "ref/netstandard2.0/*.winmd" }
        )
    }
    "Microsoft.WindowsAppSDK.1.5"           = @{
        Id            = "Microsoft.WindowsAppSDK"
        Dependencies  = @()
        KnownPackages = @("Microsoft.WindowsAppSDK", "Microsoft.Windows.SDK.BuildTools")
        Metadata      = @(
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0.18362/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0/*.winmd" }
        )
    }
    "Microsoft.WindowsAppSDK.1.6"           = @{
        Id            = "Microsoft.WindowsAppSDK"
        Dependencies  = @("Microsoft.Web.WebView2")
        KnownPackages = @("Microsoft.WindowsAppSDK", "Microsoft.Windows.SDK.BuildTools")
        Metadata      = @(
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0.18362/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0/*.winmd" }
        )
    }
    "Microsoft.WindowsAppSDK.1.7"           = @{
        Id            = "Microsoft.WindowsAppSDK"
        Dependencies  = @("Microsoft.Web.WebView2")
        KnownPackages = @("Microsoft.WindowsAppSDK", "Microsoft.Windows.SDK.BuildTools")
        Metadata      = @(
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0.18362/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK"; Pattern = "lib/uap10.0/*.winmd" }
        )
    }
    "Microsoft.WindowsAppSDK.1.8"           = @{
        Id            = "Microsoft.WindowsAppSDK"
        Dependencies  = @("Microsoft.Web.WebView2")
        KnownPackages = @(
            "Microsoft.WindowsAppSDK"
            "Microsoft.WindowsAppSDK.Base"
            "Microsoft.WindowsAppSDK.Foundation"
            "Microsoft.WindowsAppSDK.InteractiveExperiences"
            "Microsoft.WindowsAppSDK.WinUI"
            "Microsoft.WindowsAppSDK.DWrite"
            "Microsoft.WindowsAppSDK.Widgets"
            "Microsoft.WindowsAppSDK.AI"
            "Microsoft.WindowsAppSDK.Runtime"
            "Microsoft.Windows.SDK.BuildTools"
            "Microsoft.Windows.SDK.BuildTools.MSIX"
        )
        Metadata      = @(
            @{Id = "Microsoft.WindowsAppSDK.Foundation"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.InteractiveExperiences"; Pattern = "metadata/10.0.18362.0/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.WinUI"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.Widgets"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.AI"; Pattern = "metadata/*.winmd" }
        )
    }
    "Microsoft.WindowsAppSDK.1.8.250916003" = @{
        Id            = "Microsoft.WindowsAppSDK"
        Dependencies  = @("Microsoft.Web.WebView2")
        KnownPackages = @(
            "Microsoft.WindowsAppSDK"
            "Microsoft.WindowsAppSDK.Base"
            "Microsoft.WindowsAppSDK.Foundation"
            "Microsoft.WindowsAppSDK.InteractiveExperiences"
            "Microsoft.WindowsAppSDK.WinUI"
            "Microsoft.WindowsAppSDK.DWrite"
            "Microsoft.WindowsAppSDK.Widgets"
            "Microsoft.WindowsAppSDK.AI"
            "Microsoft.WindowsAppSDK.Runtime"
            "Microsoft.WindowsAppSDK.ML"
            "Microsoft.Windows.SDK.BuildTools"
            "Microsoft.Windows.SDK.BuildTools.MSIX"
        )
        Metadata      = @(
            @{Id = "Microsoft.WindowsAppSDK.Foundation"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.InteractiveExperiences"; Pattern = "metadata/10.0.18362.0/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.WinUI"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.Widgets"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.AI"; Pattern = "metadata/*.winmd" }
            @{Id = "Microsoft.WindowsAppSDK.ML"; Pattern = "metadata/*.winmd" }
        )
    }
    "Microsoft.Web.WebView2"                = @{
        Id            = "Microsoft.Web.WebView2"
        Dependencies  = @()
        KnownPackages = @("Microsoft.Web.WebView2")
        Metadata      = @(
            @{Id = "Microsoft.Web.WebView2"; Pattern = "lib/*.winmd" }
        )
    }
    "Microsoft.Graphics.Win2D.1.2"          = @{
        Id            = "Microsoft.Graphics.Win2D"
        Dependencies  = @("Microsoft.WindowsAppSDK.1.5")
        KnownPackages = @("Microsoft.Graphics.Win2D")
        Metadata      = @(
            @{Id = "Microsoft.Graphics.Win2D"; Pattern = "lib/uap10.0/*.winmd" }
        )
    }
    "Microsoft.Graphics.Win2D.1.3"          = @{
        Id            = "Microsoft.Graphics.Win2D"
        Dependencies  = @("Microsoft.WindowsAppSDK.1.6")
        KnownPackages = @("Microsoft.Graphics.Win2D")
        Metadata      = @(
            @{Id = "Microsoft.Graphics.Win2D"; Pattern = "lib/uap10.0/*.winmd" }
        )
    }
}

function Get-Metadata {
    param(
        [Parameter(Mandatory)]
        [string]$Id,
        [Parameter(Mandatory)]
        [string]$Version,
        [Parameter(Mandatory)]
        [string]$DstDir
    )

    $recipe = Get-Recipe $Id $Version

    if (-not (Test-Path $DstDir)) {
        New-Item $DstDir -ItemType Directory
    }

    $tmpdir = New-TemporaryFolder

    nuget.exe install $Id -Version $Version -OutputDirectory $tmpdir

    $installed_packages = Get-NupkgNames $tmpdir
    $known_packages = Get-KnownPackages $recipe
    if (Compare-Object $known_packages $installed_packages) {
        throw "unexpected packages result: $installed_packages expected: $known_packages"
    }

    $winmdfiles = Get-Winmdfiles $tmpdir $recipe

    dotnet.exe run -d $DstDir $winmdfiles

    tar.exe -C $DstDir -acf "$Id.$Version.zip" *

    Remove-Item -Recurse $tmpdir
}

function Get-AllMetadata {
    param(
        [Parameter(Mandatory)]
        [string]$Id,
        [Parameter(Mandatory)]
        [string]$MinVersion,
        [Parameter(Mandatory)]
        [bool]$AllowPrerelease,
        [Parameter(Mandatory)]
        [string]$DstDir
    )

    $MinVersionNumbers = [System.Version]$MinVersion

    foreach ($version in Get-NupkgIndex $Id) {
        if ($version -match "-" -and -not $AllowPrerelease) {
            continue
        }

        $versionNumbers = [System.Version]"$($version -replace '-.*', '')"
        if ($versionNumbers -lt $minVersionNumbers) {
            continue
        }

        if (Test-Path "$DstDir\$Id.$version") {
            continue
        }

        Get-Metadata $Id $version "$DstDir\$Id.$version"
    }
}

function Main {
    if ($Id -eq "") {
        Get-AllMetadata "Microsoft.Windows.SDK.Win32Metadata" "63.0" $true $DstDir
        Get-AllMetadata "Microsoft.Windows.SDK.Contracts" "10.0.26100.4948" $false $DstDir
        Get-AllMetadata "Microsoft.WindowsAppSDK" "1.5" $false $DstDir
        Get-AllMetadata "Microsoft.Web.WebView2" "1.0.2651" $false $DstDir
        Get-AllMetadata "Microsoft.Graphics.Win2D" "1.2.0" $false $DstDir
    }
    else {
        Get-Metadata $Id $Version $DstDir
    }
}


Main
