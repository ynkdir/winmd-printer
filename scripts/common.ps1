function New-TemporaryFolder {
    $tmpfile = New-TemporaryFile
    Remove-Item $tmpfile
    New-Item -Path $tmpfile -ItemType directory
}

function Download-NugetPackage {
    param (
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$Version,
        [Parameter(Mandatory)]
        [string]$DstDir
    )

    $tmpdir = New-TemporaryFolder

    New-Item $DstDir\$Name.$Version -ItemType Directory

    $uri = "https://api.nuget.org/v3-flatcontainer/$($Name.ToLower())/$Version/$($Name.ToLower()).$Version.nupkg"

    curl.exe -o $tmpdir\$Name.$Version.zip $uri

    tar.exe -C $DstDir -xvf $tmpdir\$Name.$Version.zip

    Remove-Item -Recurse $tmpdir
}

function Get-NugetIndex {
    param (
        [Parameter(Mandatory)]
        [string]$Name
    )

    $uri = "https://api.nuget.org/v3-flatcontainer/$($Name.ToLower())/index.json"
    $response = Invoke-RestMethod -Uri $uri
    $response.versions
}
