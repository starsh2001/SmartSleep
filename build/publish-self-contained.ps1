param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "publish"
)

$publishDir = Join-Path $PSScriptRoot $Output
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

$project = Join-Path $PSScriptRoot "..\src\SmartSleep.App\SmartSleep.App.csproj"
Write-Host "Publishing $project ($Configuration, $Runtime)..."

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Publish complete. Output: $publishDir"
