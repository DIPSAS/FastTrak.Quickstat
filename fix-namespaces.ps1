# Script to fix standard Delphi unit references to use namespaces

$fixes = @(
    @{Pattern = '^\s*Classes,'; Replacement = '  System.Classes,'}
    @{Pattern = '^\s*Classes;'; Replacement = '  System.Classes;'}
    @{Pattern = ',\s*Classes,'; Replacement = ', System.Classes,'}
    @{Pattern = ',\s*Classes;'; Replacement = ', System.Classes;'}
    @{Pattern = '^\s*SysUtils,'; Replacement = '  System.SysUtils,'}
    @{Pattern = '^\s*SysUtils;'; Replacement = '  System.SysUtils;'}
    @{Pattern = ',\s*SysUtils,'; Replacement = ', System.SysUtils,'}
    @{Pattern = ',\s*SysUtils;'; Replacement = ', System.SysUtils;'}
    @{Pattern = '^\s*Graphics,'; Replacement = '  Vcl.Graphics,'}
    @{Pattern = '^\s*Graphics;'; Replacement = '  Vcl.Graphics;'}
    @{Pattern = ',\s*Graphics,'; Replacement = ', Vcl.Graphics,'}
    @{Pattern = ',\s*Graphics;'; Replacement = ', Vcl.Graphics;'}
    @{Pattern = '^\s*Windows,'; Replacement = '  Winapi.Windows,'}
    @{Pattern = '^\s*Windows;'; Replacement = '  Winapi.Windows;'}
    @{Pattern = ',\s*Windows,'; Replacement = ', Winapi.Windows,'}
    @{Pattern = ',\s*Windows;'; Replacement = ', Winapi.Windows;'}
    @{Pattern = '^\s*Forms,'; Replacement = '  Vcl.Forms,'}
    @{Pattern = '^\s*Forms;'; Replacement = '  Vcl.Forms;'}
    @{Pattern = ',\s*Forms,'; Replacement = ', Vcl.Forms,'}
    @{Pattern = ',\s*Forms;'; Replacement = ', Vcl.Forms;'}
    @{Pattern = '^\s*Dialogs,'; Replacement = '  Vcl.Dialogs,'}
    @{Pattern = '^\s*Dialogs;'; Replacement = '  Vcl.Dialogs;'}
    @{Pattern = ',\s*Dialogs,'; Replacement = ', Vcl.Dialogs,'}
    @{Pattern = ',\s*Dialogs;'; Replacement = ', Vcl.Dialogs;'}
    @{Pattern = '^\s*Controls,'; Replacement = '  Vcl.Controls,'}
    @{Pattern = '^\s*Controls;'; Replacement = '  Vcl.Controls;'}
    @{Pattern = ',\s*Controls,'; Replacement = ', Vcl.Controls,'}
    @{Pattern = ',\s*Controls;'; Replacement = ', Vcl.Controls;'}
    @{Pattern = '^\s*StdCtrls,'; Replacement = '  Vcl.StdCtrls,'}
    @{Pattern = '^\s*StdCtrls;'; Replacement = '  Vcl.StdCtrls;'}
    @{Pattern = ',\s*StdCtrls,'; Replacement = ', Vcl.StdCtrls,'}
    @{Pattern = ',\s*StdCtrls;'; Replacement = ', Vcl.StdCtrls;'}
    @{Pattern = '^\s*ExtCtrls,'; Replacement = '  Vcl.ExtCtrls,'}
    @{Pattern = '^\s*ExtCtrls;'; Replacement = '  Vcl.ExtCtrls;'}
    @{Pattern = ',\s*ExtCtrls,'; Replacement = ', Vcl.ExtCtrls,'}
    @{Pattern = ',\s*ExtCtrls;'; Replacement = ', Vcl.ExtCtrls;'}
    @{Pattern = '^\s*Contnrs,'; Replacement = '  System.Contnrs,'}
    @{Pattern = '^\s*Contnrs;'; Replacement = '  System.Contnrs;'}
    @{Pattern = ',\s*Contnrs,'; Replacement = ', System.Contnrs,'}
    @{Pattern = ',\s*Contnrs;'; Replacement = ', System.Contnrs;'}
    @{Pattern = '^\s*Db,'; Replacement = '  Data.DB,'}
    @{Pattern = '^\s*Db;'; Replacement = '  Data.DB;'}
    @{Pattern = ',\s*Db,'; Replacement = ', Data.DB,'}
    @{Pattern = ',\s*Db;'; Replacement = ', Data.DB;'}
    @{Pattern = '^\s*Generics\.Collections,'; Replacement = '  System.Generics.Collections,'}
    @{Pattern = '^\s*Generics\.Collections;'; Replacement = '  System.Generics.Collections;'}
    @{Pattern = ',\s*Generics\.Collections,'; Replacement = ', System.Generics.Collections,'}
    @{Pattern = ',\s*Generics\.Collections;'; Replacement = ', System.Generics.Collections;'}
    @{Pattern = '^\s*Math,'; Replacement = '  System.Math,'}
    @{Pattern = '^\s*Math;'; Replacement = '  System.Math;'}
    @{Pattern = ',\s*Math,'; Replacement = ', System.Math,'}
    @{Pattern = ',\s*Math;'; Replacement = ', System.Math;'}
)

$files = Get-ChildItem -Path "FastTrak\*.pas" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    foreach ($fix in $fixes) {
        $content = $content -replace $fix.Pattern, $fix.Replacement
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "Fixed: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`nDone fixing namespace references!" -ForegroundColor Cyan
