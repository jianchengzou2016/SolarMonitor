param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Version = "",
    [string]$InnoCompilerPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Add-DirectoryToPathIfPresent {
    param([string]$PathToAdd)

    if (-not [string]::IsNullOrWhiteSpace($PathToAdd) -and (Test-Path $PathToAdd)) {
        $pathEntries = $env:PATH -split ';'
        if ($pathEntries -notcontains $PathToAdd) {
            $env:PATH = "$PathToAdd;$env:PATH"
        }
    }
}

function Resolve-InnoCompilerPath {
    param([string]$ConfiguredPath)

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        if (-not (Test-Path $ConfiguredPath)) {
            throw "Inno Setup compiler was not found at '$ConfiguredPath'."
        }

        return $ConfiguredPath
    }

    $commonPaths = @(
        "D:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $commonPaths) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw "Unable to find ISCC.exe. Install Inno Setup 6 or pass -InnoCompilerPath explicitly."
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$publishDirectory = Join-Path $root "artifacts\publish\$RuntimeIdentifier"
$outputDirectory = Join-Path $root "artifacts\installer"
$issPath = Join-Path $PSScriptRoot "SolarMonitor.iss"

Add-DirectoryToPathIfPresent "D:\Program Files\Git\cmd"
Add-DirectoryToPathIfPresent "C:\Program Files\Git\cmd"
$innoCompilerPath = Resolve-InnoCompilerPath -ConfiguredPath $InnoCompilerPath

New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

Push-Location $root
try {
    if ([string]::IsNullOrWhiteSpace($Version)) {
        $versionInfo = dotnet msbuild .\src\SolarMonitor.App\SolarMonitor.App.csproj -nologo -t:MinVer -getProperty:MinVerVersion -getProperty:Version | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to resolve application version from MSBuild."
        }

        $Version = $versionInfo.Properties.Version.Trim()
    }

    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw "Resolved version was empty."
    }

    $setupVersion = ($Version -split '\+', 2)[0]
    $setupPath = Join-Path $outputDirectory "SolarMonitor-Setup-$setupVersion-$RuntimeIdentifier.exe"

    dotnet publish .\src\SolarMonitor.App\SolarMonitor.App.csproj `
        -c $Configuration `
        -r $RuntimeIdentifier `
        --self-contained false `
        -p:PublishSingleFile=false `
        -o $publishDirectory
}
finally {
    Pop-Location
}

if (Test-Path $setupPath) {
    Remove-Item -LiteralPath $setupPath -Force
}

& $innoCompilerPath `
    "/DMyAppVersion=$setupVersion" `
    "/DMyAppSourceDir=$publishDirectory" `
    "/DMyAppOutputDir=$outputDirectory" `
    $issPath

Write-Host "Installer created at $setupPath"
