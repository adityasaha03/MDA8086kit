param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = (Get-Item $ScriptDir).Parent.FullName
$DistDir = Join-Path $RootDir "dist"
$StagingDir = Join-Path $DistDir "MDA8086_Kit_v$Version"
$ZipPath = Join-Path $DistDir "MDA8086_Kit_v$Version.zip"

Write-Host "Starting Release Build for version $Version..."

Set-Location $RootDir

# 1. Clean and Build
Write-Host "Cleaning and building Release..."
dotnet clean -c Release
dotnet build -c Release

# 2. Run Tests
Write-Host "Running tests..."
dotnet test -c Release --no-build

# 3. Create Dist and Staging
if (Test-Path $DistDir) { Remove-Item -Recurse -Force $DistDir }
New-Item -ItemType Directory -Force -Path $StagingDir | Out-Null

# 4. Copy Artifacts
Write-Host "Copying artifacts to staging..."
$ExePath = Join-Path $RootDir "src\Mda8086Kit\bin\Release\net48\MDA8086_Kit.exe"

if (-not (Test-Path $ExePath)) {
    throw "Executable not found at $ExePath. Build might have failed."
}

Copy-Item $ExePath -Destination $StagingDir

# Copy documentation and other assets if they exist
$FilesToCopy = @("README.md", "LICENSE", "NOTICE")
foreach ($file in $FilesToCopy) {
    $srcFile = Join-Path $RootDir $file
    if (Test-Path $srcFile) {
        Copy-Item $srcFile -Destination $StagingDir
    }
}

# Copy directories
$DirsToCopy = @("docs", "samples")
foreach ($dir in $DirsToCopy) {
    $srcDir = Join-Path $RootDir $dir
    if (Test-Path $srcDir) {
        Copy-Item $srcDir -Destination $StagingDir -Recurse
    }
}

# Create a sample config
$ConfigContent = "; Sample MDA8086_Kit.config`r`n; Place beside MDA8086_Kit.exe to set defaults."
Set-Content -Path (Join-Path $StagingDir "MDA8086_Kit.config") -Value $ConfigContent

# 5. Zip it up
Write-Host "Zipping to $ZipPath..."
Compress-Archive -Path "$StagingDir\*" -DestinationPath $ZipPath -Force

# 6. Generate SHA-256
$Hash = Get-FileHash -Path $ZipPath -Algorithm SHA256
$HashLine = "$($Hash.Hash)  MDA8086_Kit_v$Version.zip"
$HashFile = Join-Path $DistDir "MDA8086_Kit_v$Version.sha256"
Set-Content -Path $HashFile -Value $HashLine

Write-Host "Release packaging complete! Artifacts available in $DistDir"
