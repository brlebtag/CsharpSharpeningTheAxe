$Company = Read-Host "Enter your company name"
$CN = Read-Host "Enter Subject (CN=...) or press Enter to use company name"
$Password = Read-Host "Enter password (default: 3.14159)" -AsSecureString

$Publisher_CN = if ($CN) { $CN } else { "CN=$Company" }
$Password = if ($Password) { $Password } else { ConvertTo-SecureString -String "3.14159" -Force -AsPlainText }

# SOLUÇÃO PARA A DATA: 
# Criamos um objeto de data real para evitar erros de digitação ou região.
# Aqui, definimos para 10 anos a partir de hoje.
$ExpiryDate = (Get-Date).AddYears(10)

try {
    $cert = New-SelfSignedCertificate `
        -Subject $Publisher_CN `
        -FriendlyName $Company `
        -KeyAlgorithm RSA `
        -KeyLength 3072 `
        -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -Type CodeSigningCert `
        -CertStoreLocation "Cert:\LocalMachine\my" `
        -KeyDescription "Code Signing Cert for MSIX Packages" `
        -NotAfter $ExpiryDate `
        -HashAlgorithm SHA256 `
        -TextExtension @("2.5.29.19={text}CA=false")

    # Exportação usando o caminho do script
    $cert | Export-PfxCertificate -FilePath "$PSScriptRoot\$Company.pfx" -Password $Password -Force
    
    Write-Host "Sucesso! Certificado salvo em: $PSScriptRoot\$Company.pfx" -ForegroundColor Green
}
catch {
    Write-Error "Erro ao gerar ou exportar o certificado: $_"
}