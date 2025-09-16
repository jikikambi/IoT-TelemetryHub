$password = "Pa55W0rd"
$pfxFiles = Get-ChildItem -Path "/app/Certs" -Filter "*.pfx"

foreach ($pfx in $pfxFiles) {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $pfx.FullName, $password
    $pemBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    $pemPath = Join-Path $pfx.DirectoryName ($pfx.BaseName + "-fixed.crt")
    [System.IO.File]::WriteAllBytes($pemPath, $pemBytes)
    Write-Host "Generated PEM:" $pemPath
}