# Read JSON from stdin passed by Claude Code
$inputJson = $Input | Out-String
if ([string]::IsNullOrWhiteSpace($inputJson)) {
    Exit 0
}

try {
    $json = ConvertFrom-Json $inputJson
} catch {
    Write-Warning "Failed to parse JSON from hook stdin"
    Exit 0
}

# Resolve the file path of the edited file
$filePath = $null
if ($json.tool_input) {
    if ($json.tool_input.TargetFile) {
        $filePath = $json.tool_input.TargetFile
    } elseif ($json.tool_input.AbsolutePath) {
        $filePath = $json.tool_input.AbsolutePath
    } elseif ($json.tool_input.file_path) {
        $filePath = $json.tool_input.file_path
    }
}

if (-not $filePath) {
    Exit 0
}

# Get extension and check
$ext = [System.IO.Path]::GetExtension($filePath).ToLower()

if ($ext -eq ".cs") {
    $relative = Resolve-Path -Path $filePath -Relative
    $relative = $relative -replace "^\.\\", "" -replace "\\", "/"
    
    Write-Host "Running dotnet format on C# file: $relative"
    dotnet format 10xCookBook.sln --include $relative --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "C# formatting validation failed. Please run 'dotnet format' to fix style issues."
        Exit 2
    }
} elseif ($ext -eq ".ts") {
    Write-Host "Running TypeScript typecheck on frontend..."
    Push-Location frontend
    npx tsc --noEmit -p tsconfig.app.json
    $exitCode = $LASTEXITCODE
    Pop-Location
    if ($exitCode -ne 0) {
        Write-Error "TypeScript typecheck failed. Please fix compilation/type errors."
        Exit 2
    }
}

Exit 0
