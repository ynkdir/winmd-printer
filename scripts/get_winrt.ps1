$winsdkroot = "C:\Program Files (x86)\Windows Kits"
$sdkversion = "10.0.22621.0"
$platform = "${winsdkroot}\10\Platforms\UAP\${sdkversion}\Platform.xml"
$reference = "${winsdkroot}\10\References\${sdkversion}"

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

Copy-Item winrt\*.json json
