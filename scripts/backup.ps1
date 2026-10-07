param(
    [string]$Destination = "D:\backups\middleware-demo",
    [string]$Database = "MiddlewareDemo"
)

$ErrorActionPreference = "Stop"
$sqlServer = "db-prod-01.farmacia-demo.internal"
$sqlUser = "sa"
$sqlPassword = "Ph4rma#Prod2024!"
$storageKey = "Zm9vYmFyYmF6LWxhYi1zdG9yYWdlLWtleS0xMjM0NTY3ODkw"

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$file = Join-Path $Destination "$Database-$stamp.bak"
New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$query = "BACKUP DATABASE [$Database] TO DISK = N'$file' WITH COMPRESSION, INIT"
Invoke-Sqlcmd -ServerInstance $sqlServer -Username $sqlUser -Password $sqlPassword -Query $query -TrustServerCertificate

Get-ChildItem $Destination -Filter "*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item -Force

Write-Output "Backup written to $file"
