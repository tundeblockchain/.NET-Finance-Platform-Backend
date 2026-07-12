<#
.SYNOPSIS
    Deploys Finance Platform SQL Server scripts in dependency order.

.DESCRIPTION
    Applies scripts under this folder:
      1. 00_CreateDatabase.sql  (against master)
      2. Tables/*.sql
      3. Archives/*.sql
      4. Procedures/**/*.sql

    Requires sqlcmd on PATH (SQL Server command-line tools).

.PARAMETER Server
    SQL Server instance. Default: localhost

.PARAMETER Database
    Target database name. Default: FinancePlatform

.PARAMETER Username
    SQL auth username. Omit (with Password) to use Windows auth / Trusted Connection.

.PARAMETER Password
    SQL auth password.

.PARAMETER SkipCreateDatabase
    Skip 00_CreateDatabase.sql (use when the database already exists).

.PARAMETER WhatIf
    List scripts that would run without executing them.

.EXAMPLE
    .\Deploy-SqlServer.ps1

.EXAMPLE
    .\Deploy-SqlServer.ps1 -Server "localhost\SQLEXPRESS" -Database FinancePlatform

.EXAMPLE
    .\Deploy-SqlServer.ps1 -Server "myserver.database.windows.net" -Username sa -Password "Secret"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Server = "localhost",
    [string]$Database = "FinanceDb",
    [string]$Username,
    [string]$Password,
    [switch]$SkipCreateDatabase
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-SqlCmdAvailable {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw @"
sqlcmd was not found on PATH.
Install SQL Server Command Line Utilities, or add sqlcmd to PATH, then retry.
"@
    }
}

function Get-SqlCmdAuthArgs {
    if ($Username) {
        if ([string]::IsNullOrWhiteSpace($Password)) {
            throw "Password is required when Username is specified."
        }
        return @("-U", $Username, "-P", $Password)
    }

    return @("-E")
}

function Invoke-SqlScript {
    param(
        [Parameter(Mandatory)][string]$ServerInstance,
        [Parameter(Mandatory)][string]$DatabaseName,
        [Parameter(Mandatory)][string]$ScriptPath,
        [Parameter(Mandatory)][string[]]$AuthArgs
    )

    $args = @(
        "-S", $ServerInstance,
        "-d", $DatabaseName,
        "-b",
        "-I",
        "-i", $ScriptPath
    ) + $AuthArgs

    & sqlcmd @args
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed (exit $LASTEXITCODE) for: $ScriptPath"
    }
}

function Get-OrderedScripts {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$TargetDatabase,
        [switch]$SkipCreate
    )

    $scripts = [System.Collections.Generic.List[object]]::new()

    $createDb = Join-Path $Root "00_CreateDatabase.sql"
    if (-not $SkipCreate) {
        if (-not (Test-Path -LiteralPath $createDb)) {
            throw "Missing create-database script: $createDb"
        }
        $scripts.Add([pscustomobject]@{
            Path     = $createDb
            Database = "master"
            Phase    = "CreateDatabase"
        })
    }

    $tableDir = Join-Path $Root "Tables"
    $archiveDir = Join-Path $Root "Archives"
    $procedureDir = Join-Path $Root "Procedures"

    $phases = @(
        @{ Name = "Tables";     Files = @(Get-ChildItem -Path $tableDir -Filter "*.sql" -File -ErrorAction SilentlyContinue | Sort-Object Name) }
        @{ Name = "Archives";   Files = @(Get-ChildItem -Path $archiveDir -Filter "*.sql" -File -ErrorAction SilentlyContinue | Sort-Object Name) }
        @{ Name = "Procedures"; Files = @(Get-ChildItem -Path $procedureDir -Filter "*.sql" -File -Recurse -ErrorAction SilentlyContinue | Sort-Object FullName) }
    )

    foreach ($phase in $phases) {
        foreach ($file in $phase.Files) {
            $scripts.Add([pscustomobject]@{
                Path     = $file.FullName
                Database = $TargetDatabase
                Phase    = $phase.Name
            })
        }
    }

    if ($scripts.Count -eq 0) {
        throw "No SQL scripts found under $Root"
    }

    return $scripts
}

Test-SqlCmdAvailable

$root = $PSScriptRoot
$authArgs = Get-SqlCmdAuthArgs
$scripts = Get-OrderedScripts -Root $root -TargetDatabase $Database -SkipCreate:$SkipCreateDatabase

if (-not $SkipCreateDatabase -and $Database -ne "FinancePlatform") {
    Write-Warning "00_CreateDatabase.sql creates database 'FinancePlatform'. -Database '$Database' is used only for Tables/Archives/Procedures. Create that database first or keep -Database FinancePlatform."
}

Write-Host "Deploying Finance Platform SQL scripts"
Write-Host "  Server   : $Server"
Write-Host "  Database : $Database"
Write-Host "  Scripts  : $($scripts.Count)"
Write-Host ""

$succeeded = 0
$currentPhase = $null

foreach ($script in $scripts) {
    if ($script.Phase -ne $currentPhase) {
        $currentPhase = $script.Phase
        Write-Host "=== $currentPhase ===" -ForegroundColor Cyan
    }

    $relative = $script.Path.Substring($root.Length).TrimStart("\", "/")
    $targetDb = if ($script.Phase -eq "CreateDatabase") { "master" } else { $Database }

    if ($WhatIfPreference) {
        Write-Host "  $relative  (against $targetDb)"
        continue
    }

    Write-Host "  -> $relative"
    Invoke-SqlScript `
        -ServerInstance $Server `
        -DatabaseName $targetDb `
        -ScriptPath $script.Path `
        -AuthArgs $authArgs
    $succeeded++
}

Write-Host ""
if ($WhatIfPreference) {
    Write-Host "WhatIf: $($scripts.Count) script(s) would be applied." -ForegroundColor Yellow
}
else {
    Write-Host "Deploy complete: $succeeded / $($scripts.Count) script(s) applied." -ForegroundColor Green
}
