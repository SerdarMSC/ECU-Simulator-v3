# listen-only.ps1
# Hicbir komut GONDERMEZ, sadece portu dinler. Amac: bu kartin "sniffer" modunda
# olup olmadigini, yani CAN hattinda gercek trafik varken kendiliginden veri
# gonderip gondermedigini gormek.

param(
    [Parameter(Mandatory=$true)]
    [string]$PortName,
    [int]$Baud = 115200,
    [int]$Seconds = 15
)

Write-Host "Sadece dinleme modu: $PortName @ $Baud baud, $Seconds saniye" -ForegroundColor Magenta
Write-Host "HICBIR KOMUT GONDERILMIYOR - sadece kartin kendiliginden bir sey soyleyip soylemedigine bakiyoruz."
Write-Host "Simdi ELM327'yi/CAN kaynagini baglayip enerji verin (henuz vermediyseniz)." -ForegroundColor Yellow
Write-Host ""

$sp = New-Object System.IO.Ports.SerialPort $PortName, $Baud, ([System.IO.Ports.Parity]::None), 8, ([System.IO.Ports.StopBits]::One)
$sp.ReadTimeout = 200
$sp.DtrEnable = $true
$sp.RtsEnable = $true

try {
    $sp.Open()
} catch {
    Write-Host "ACILAMADI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Port acildi, dinleniyor... (Ctrl+C ile durdurabilirsiniz)" -ForegroundColor Green

$totalBytes = 0
$sw = [System.Diagnostics.Stopwatch]::StartNew()

while ($sw.Elapsed.TotalSeconds -lt $Seconds) {
    try {
        if ($sp.BytesToRead -gt 0) {
            $n = $sp.BytesToRead
            $buf = New-Object byte[] $n
            $sp.Read($buf, 0, $n) | Out-Null
            $totalBytes += $n
            $hex = ($buf | ForEach-Object { $_.ToString("X2") }) -join " "
            $ascii = -join ($buf | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { "." } })
            $t = [math]::Round($sw.Elapsed.TotalSeconds, 2)
            Write-Host "[$t s] $n bayt geldi | HEX: $hex | ASCII: $ascii" -ForegroundColor Cyan
        }
    } catch {}
    Start-Sleep -Milliseconds 20
}

$sp.Close()
Write-Host ""
if ($totalBytes -eq 0) {
    Write-Host "SONUC: $Seconds saniyede hicbir bayt gelmedi (0 bayt toplam)." -ForegroundColor Red
} else {
    Write-Host "SONUC: Toplam $totalBytes bayt geldi! Kart gercekten veri gonderiyor." -ForegroundColor Green
}
