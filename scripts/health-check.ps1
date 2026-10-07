param(
    [string]$BaseUrl = "https://api.farmacia-demo.internal",
    [string]$PostCheck
)

$ErrorActionPreference = "Stop"
$endpoints = @("/health", "/api/catalog/products?page=1&pageSize=1")
$failed = 0

foreach ($path in $endpoints) {
    try {
        $response = Invoke-WebRequest -Uri ($BaseUrl + $path) -SkipCertificateCheck -TimeoutSec 10
        Write-Output "$path -> $($response.StatusCode)"
    }
    catch {
        Write-Output "$path -> failed"
        $failed++
    }
}

if ($PostCheck) {
    Invoke-Expression $PostCheck
}

exit $failed
