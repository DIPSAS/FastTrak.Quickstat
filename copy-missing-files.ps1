# Script to automatically copy missing files from FastTrakDev to FastTrak
param(
    [int]$MaxIterations = 50
)

$iteration = 0
$foundMissing = $true

while ($foundMissing -and $iteration -lt $MaxIterations) {
    $iteration++
    Write-Host "`n=== Iteration $iteration ===" -ForegroundColor Cyan
    
    # Run the build and capture output
    $output = & .\build.bat 2>&1 | Out-String
    
    # Check if build succeeded
    if ($output -match "Build successful") {
        Write-Host "Build completed successfully!" -ForegroundColor Green
        $foundMissing = $false
        break
    }
    
    # Look for missing unit errors
    if ($output -match "Unit '([^']+)' not found") {
        $missingUnit = $matches[1]
        
        # Skip standard Delphi RTL/VCL units
        $standardUnits = @('Classes', 'SysUtils', 'Windows', 'Graphics', 'Controls', 'Forms', 'Dialogs', 
                          'Messages', 'Variants', 'Db', 'Math', 'StrUtils', 'DateUtils', 'Types',
                          'Generics.Collections', 'Generics.Defaults', 'IOUtils', 'UITypes', 'Registry')
        if ($standardUnits -contains $missingUnit) {
            Write-Host "Skipping standard unit: $missingUnit (needs namespace prefix)" -ForegroundColor Yellow
            $foundMissing = $false
            break
        }
        
        Write-Host "Missing unit: $missingUnit" -ForegroundColor Yellow
        
        # Search for the file in FastTrakDev
        $fileName = "$missingUnit.pas"
        $sourceFile = Get-ChildItem -Path "FastTrakDev" -Filter $fileName -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($sourceFile) {
            Write-Host "Found: $($sourceFile.FullName)" -ForegroundColor Green
            Write-Host "Copying to FastTrak..." -ForegroundColor Green
            Copy-Item $sourceFile.FullName -Destination "FastTrak\" -Force
        } else {
            Write-Host "File not found in FastTrakDev: $fileName" -ForegroundColor Red
            $foundMissing = $false
        }
    } else {
        Write-Host "No missing unit error found or different error occurred" -ForegroundColor Red
        Write-Host $output
        $foundMissing = $false
    }
}

if ($iteration -ge $MaxIterations) {
    Write-Host "`nReached maximum iterations ($MaxIterations)" -ForegroundColor Red
}
