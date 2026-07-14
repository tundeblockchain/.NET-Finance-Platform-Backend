<#
.SYNOPSIS
    Clears all user-table data in the Finance Platform SQL Server database.

.DESCRIPTION
    Runs ClearAllTables.sql against the target database via sqlcmd.
    Deletes rows from every user table (including archive tables); does not drop objects.

    Requires sqlcmd on PATH (SQL Server command-line tools).

.PARAMETER Server
    SQL Server instance. Default: localhost

.PARAMETER Database
    Target database name. Default: FinanceDb

.PARAMETER Username
    SQL auth username. Omit (with Password) to use Windows auth / Trusted Connection.

.PARAMETER Password
    SQL auth password.

.PARAMETER WhatIf
    Show what would run without executing.

.EXAMPLE
    .\Clear-SqlServer.ps1

.EXAMPLE
    .\Clear-SqlServer.ps1 -Server "localhost\SQLEXPRESS" -Database FinancePlatform

.EXAMPLE
    .\Clear-SqlServer.ps1 -Server "myserver" -Username sa -Password "YourPassword"
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = "High")]
param(
    [string]$Server = "localhost",
    [string]$Database = "FinanceDb",
    [string]$Username,
    [string]$Password
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

Test-SqlCmdAvailable

$root = $PSScriptRoot
$scriptPath = Join-Path $root "ClearAllTables.sql"

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Missing clear script: $scriptPath"
}

$authArgs = Get-SqlCmdAuthArgs

Write-Host "Clearing Finance Platform SQL tables"
Write-Host "  Server   : $Server"
Write-Host "  Database : $Database"
Write-Host "  Script   : ClearAllTables.sql"
Write-Host ""

if ($WhatIfPreference) {
    Write-Host "WhatIf: would run ClearAllTables.sql against $Database on $Server." -ForegroundColor Yellow
    return
}

if (-not $PSCmdlet.ShouldProcess("$Server / $Database", "DELETE all rows from all user tables")) {
    Write-Host "Cancelled." -ForegroundColor Yellow
    return
}

Write-Host "  -> ClearAllTables.sql"
Invoke-SqlScript `
    -ServerInstance $Server `
    -DatabaseName $Database `
    -ScriptPath $scriptPath `
    -AuthArgs $authArgs

Write-Host ""
Write-Host "Clear complete: all user tables emptied in '$Database'." -ForegroundColor Green
