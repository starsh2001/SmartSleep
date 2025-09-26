param(
    [string]$CommitMessageFile
)

if (-not $CommitMessageFile) {
    Write-Error "commit-msg hook requires the path to the commit message file"
    exit 1
}

if (-not (Test-Path -Path $CommitMessageFile)) {
    Write-Error "Commit message file '$CommitMessageFile' was not found"
    exit 1
}

$firstLine = (Get-Content -Path $CommitMessageFile -TotalCount 1 -Encoding UTF8).Trim()

if ([string]::IsNullOrWhiteSpace($firstLine)) {
    Write-Error "Commit message must start with a non-empty summary line"
    exit 1
}

$allowedTypes = @('feat','fix','docs','style','refactor','perf','test','build','ci','chore','revert','release')
$typesPattern = [string]::Join('|', $allowedTypes)
$pattern = "^(${typesPattern})(\([a-z0-9-]+\))?: .{1,72}$"

if ($firstLine -notmatch $pattern) {
    Write-Host "Commit message does not follow Conventional Commits." -ForegroundColor Red
    Write-Host "Use: type(scope?): short description" -ForegroundColor Yellow
    Write-Host "Allowed types: $($allowedTypes -join ', ')" -ForegroundColor Yellow
    Write-Host "Example: feat(settings): add idle reset logic" -ForegroundColor Yellow
    exit 1
}

exit 0
