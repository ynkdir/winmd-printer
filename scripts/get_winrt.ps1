$winsdkroot = "C:\Program Files (x86)\Windows Kits"
$sdkversion = "10.0.22621.0"
$platform = "${winsdkroot}\10\Platforms\UAP\${sdkversion}\Platform.xml"
$extension_windows_desktop = "${winsdkroot}\10\Extension SDKs\WindowsDesktop\${sdkversion}\SDKManifest.xml"
$extension_windows_mobile = "${winsdkroot}\10\Extension SDKs\WindowsMobile\${sdkversion}\SDKManifest.xml"
$extension_windows_team = "${winsdkroot}\10\Extension SDKs\WindowsTeam\${sdkversion}\SDKManifest.xml"
$reference = "${winsdkroot}\10\References\${sdkversion}"
$unionmetadata = "${winsdkroot}\10\UnionMetadata\${sdkversion}"


function GetPlatformMetadata() {
    if (-not (Test-Path winrt)) {
        New-Item winrt -ItemType Directory
    }

    Remove-Item winrt\*

    $xml = [XML](Get-Content $platform)
    foreach ($ApiContract in $xml.ApplicationPlatform.ContainedApiContracts.ApiContract) {
        Write-Output $ApiContract.name
        Copy-Item "${reference}\$($ApiContract.name)\$($ApiContract.version)\$($ApiContract.name).winmd" winrt
        cmd /c "dotnet run `"winrt\$($ApiContract.name).winmd`" > `"winrt\$($ApiContract.name).json`""
    }

    Write-Output "make Windows.WinRT.json ..."
    py -c "import json, glob; json.dump([td for f in glob.glob('winrt/*.json') for td in json.load(open(f))], open('Windows.WinRT.json', 'w'), indent=2)"
}

# ?
function GetUnionMetadata() {
    cmd /c "dotnet run `"${unionmetadata}\Windows.winmd`" > Windows.WinRT.json"
}

GetUnionMetadata
py scripts\split_namespace.py Windows.WinRT.json
