<#
.SYNOPSIS
    Bygger QuickStat.dpr med Delphi-kompilatoren (dcc32).

.DESCRIPTION
    PowerShell-versjon av build.bat. Kompilerer QuickStat mot kildekoden i
    FastTrak- og Spring-mappene i repoet, uten behov for at IDE-pakker er
    installert. Skriver ferdig QuickStat.exe til repo-roten.

.PARAMETER StudioVersion
    RAD Studio-versjon (mappenavn under Embarcadero\Studio). Standard: 37.0.

.PARAMETER Project
    Delphi-prosjektfil (.dpr) som skal bygges. Standard: QuickStat.dpr.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -StudioVersion 23.0
#>
[CmdletBinding()]
param(
    [string]$StudioVersion = '37.0',
    [string]$Project       = 'QuickStat.dpr'
)

$ErrorActionPreference = 'Stop'

# Kjør fra mappen der scriptet ligger, slik at relative stier stemmer.
$RepoRoot = $PSScriptRoot
Push-Location $RepoRoot
try {
    $DelphiRoot = "C:\Program Files (x86)\Embarcadero\Studio\$StudioVersion"
    $Dcc32      = Join-Path $DelphiRoot 'bin\dcc32.exe'
    $Brcc32     = Join-Path $DelphiRoot 'bin\brcc32.exe'
    $DelphiLib  = Join-Path $DelphiRoot 'lib\win32\release'
    $RaizePath  = "C:\Users\Public\Documents\Embarcadero\Studio\$StudioVersion\CatalogRepository\BonusKSVC\8.0.1\Source"

    if (-not (Test-Path $Dcc32)) {
        throw "Fant ikke Delphi-kompilatoren: $Dcc32. Angi riktig versjon med -StudioVersion."
    }
    if (-not (Test-Path $Project)) {
        throw "Fant ikke prosjektfilen: $Project"
    }

    Write-Host "Bygger $Project med RAD Studio $StudioVersion ..." -ForegroundColor Cyan

    # dcc32 trenger $(BDS) for å finne standardbibliotekene.
    $env:BDS = $DelphiRoot

    # {$R *.res} krever en ressursfil som normalt genereres av IDE-en og er
    # git-ignorert. Generer den fra prosjektikonet hvis den mangler.
    $ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($Project)
    $ResFile     = "$ProjectName.res"
    $IconFile    = "$ProjectName`_Icon.ico"
    if (-not (Test-Path $ResFile)) {
        if (Test-Path $IconFile) {
            Write-Host "Genererer $ResFile fra $IconFile ..." -ForegroundColor Cyan
            # brcc32 skriver som standard <input>.res, så .rc-filen navngis likt.
            $RcFile = "$ProjectName.rc"
            "MAINICON ICON `"$IconFile`"" | Set-Content -Path $RcFile -Encoding Ascii
            & $Brcc32 $RcFile
            $brccExit = $LASTEXITCODE
            Remove-Item $RcFile -Force
            if ($brccExit -ne 0 -or -not (Test-Path $ResFile)) {
                throw "Klarte ikke å generere $ResFile."
            }
        }
        else {
            throw "Mangler $ResFile og fant ikke ikonet $IconFile for å generere den."
        }
    }

    $namespaces = 'System;Xml;Data;Datasnap;Web;Soap;Vcl;Vcl.Imaging;Vcl.Touch;Vcl.Samples;Vcl.Shell;VCLTee;Winapi'
    $unitPaths  = "FastTrak;Spring;$RaizePath;$DelphiLib"
    $resPaths   = "FastTrak;$RaizePath"

    & $Dcc32 `
        "-NS$namespaces" `
        "-U$unitPaths" `
        '-IFastTrak' `
        "-R$resPaths" `
        $Project

    if ($LASTEXITCODE -ne 0) {
        throw "Bygging feilet med feilkode $LASTEXITCODE."
    }

    Write-Host 'Bygging fullført.' -ForegroundColor Green
}
finally {
    Pop-Location
}
