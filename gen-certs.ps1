# ---------------------------------------------------
# PowerShell Script: gen-certs.ps1
# Generates CA, Gateway, and Device certificates
# Passwords derived from a base password
# ---------------------------------------------------

# Root folders
$rootPath    = "Certs"
$caPath      = Join-Path $rootPath "CA"
$gatewayPath = Join-Path $rootPath "Gateway"
$devicePath  = Join-Path $rootPath "Devices"

# Create folder structure
New-Item -ItemType Directory -Force -Path $caPath, $gatewayPath, $devicePath | Out-Null

# Base password (you can also read this from an environment variable)
$BasePassword = "passW0rd"

# -----------------------------
# 1. Root CA
# -----------------------------
$CAPass = $BasePassword + "CA"  # e.g., passW0rdCA
openssl genrsa -aes256 -passout pass:$CAPass -out "$caPath/ca.key" 4096
openssl req -x509 -new -nodes -key "$caPath/ca.key" -sha256 -days 3650 `
    -out "$caPath/ca.crt" -subj "/CN=PulseNet Dev CA" -passin pass:$CAPass

# -----------------------------
# 2. Gateway certificate
# -----------------------------
$GatewayPass = $BasePassword + "123"  # e.g., passW0rd123
openssl genrsa -out "$gatewayPath/gateway.key" 2048
openssl req -new -key "$gatewayPath/gateway.key" -out "$gatewayPath/gateway.csr" -subj "/CN=localhost"
openssl x509 -req -in "$gatewayPath/gateway.csr" -CA "$caPath/ca.crt" -CAkey "$caPath/ca.key" -CAcreateserial `
    -out "$gatewayPath/gateway.crt" -days 365 -sha256 -passin pass:$CAPass
openssl pkcs12 -export -out "$gatewayPath/gateway.pfx" -inkey "$gatewayPath/gateway.key" -in "$gatewayPath/gateway.crt" `
    -certfile "$caPath/ca.crt" -password pass:$GatewayPass

# -----------------------------
# 3. Device certificates
# -----------------------------
# Number of devices to generate
$deviceCount = 5

for ($i = 1; $i -le $deviceCount; $i++) {
    $deviceNumber = $i.ToString("D3")           # 001, 002, etc.
    $deviceName   = "device-$deviceNumber"      # device-001, device-002, etc.
    $deviceFolder = Join-Path $devicePath $deviceName
    New-Item -ItemType Directory -Force -Path $deviceFolder | Out-Null

    $deviceKey  = Join-Path $deviceFolder "$deviceName.key"
    $deviceCsr  = Join-Path $deviceFolder "$deviceName.csr"
    $deviceCrt  = Join-Path $deviceFolder "$deviceName.crt"
    $devicePfx  = Join-Path $deviceFolder "$deviceName.pfx"

    $devicePass = $BasePassword + $deviceNumber # e.g., passW0rd001

    # Generate key, CSR, CRT, and PFX
    openssl genrsa -out $deviceKey 2048
    openssl req -new -key $deviceKey -out $deviceCsr -subj "/CN=localhost/O=$deviceName"
    openssl x509 -req -in $deviceCsr -CA "$caPath/ca.crt" -CAkey "$caPath/ca.key" -CAcreateserial `
        -out $deviceCrt -days 365 -sha256 -passin pass:$CAPass
    openssl pkcs12 -export -out $devicePfx -inkey $deviceKey -in $deviceCrt `
        -certfile "$caPath/ca.crt" -password pass:$devicePass
}

Write-Host "Certificates generated successfully in $rootPath"
