# test-usbcan.ps1
# USB-CAN kartini C# projesinden BAGIMSIZ olarak test eder.
# Tum yaygin baud degerlerini dener, gelen HER baytı (varsa) hex olarak gosterir.

param(
    [Parameter(Mandatory=$true)]
    [string]$PortName
)

$bauds = @(9600, 19200, 38400, 57600, 115200, 230400)

function Test-Baud {
    param($port, $baud)

    Write-Host ""
    Write-Host "=== $port @ $baud baud ===" -ForegroundColor Cyan

    try {
        $sp = New-Object System.IO.Ports.SerialPort $port, $baud, ([System.IO.Ports.Parity]::None), 8, ([System.IO.Ports.StopBits]::One)
        $sp.ReadTimeout = 500
        $sp.WriteTimeout = 2000
        $sp.DtrEnable = $true
        $sp.RtsEnable = $true
        $sp.Open()
    } catch {
        Write-Host "  ACILAMADI: $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    Write-Host "  Port acildi. Boot icin 2 sn bekleniyor (DTR resetleme ihtimaline karsi)..."
    Start-Sleep -Seconds 2

    try { $sp.DiscardInBuffer() } catch {}

    # Boot sirasinda / hicbir komut gondermeden kendiliginden bir sey soyluyor mu?
    Start-Sleep -Milliseconds 300
    $unsolicited = Read-AllBytes $sp
    if ($unsolicited.Length -gt 0) {
        Write-Host "  [KOMUTSUZ VERI GELDI] $(HexDump $unsolicited)" -ForegroundColor Yellow
    }

    # V komutunu dene (SLCAN versiyon sorgusu)
    Send-Command $sp "V`r"
    Start-Sleep -Milliseconds 600
    $resp1 = Read-AllBytes $sp
    Write-Host "  'V' komutuna yanit: $(HexDump $resp1)" -ForegroundColor $(if ($resp1.Length -gt 0) { "Green" } else { "DarkGray" })

    # O komutunu dene (kanal ac - bazi kartlar sadece buna cevap verir)
    Send-Command $sp "S6`r"
    Start-Sleep -Milliseconds 300
    Send-Command $sp "O`r"
    Start-Sleep -Milliseconds 600
    $resp2 = Read-AllBytes $sp
    Write-Host "  'S6'+'O' komutuna yanit: $(HexDump $resp2)" -ForegroundColor $(if ($resp2.Length -gt 0) { "Green" } else { "DarkGray" })

    $sp.Close()
}

function Send-Command($sp, $text) {
    Write-Host "  -> Gonderiliyor: '$($text -replace "`r","\r")'"
    $sp.Write($text)
}

function Read-AllBytes($sp) {
    $bytes = New-Object System.Collections.Generic.List[byte]
    try {
        while ($sp.BytesToRead -gt 0) {
            $b = $sp.ReadByte()
            if ($b -ge 0) { $bytes.Add([byte]$b) }
        }
    } catch {}
    return ,$bytes.ToArray()
}

function HexDump($bytes) {
    if ($bytes.Length -eq 0) { return "(hicbir sey - 0 bayt)" }
    $hex = ($bytes | ForEach-Object { $_.ToString("X2") }) -join " "
    $ascii = -join ($bytes | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { "." } })
    return "$($bytes.Length) bayt | HEX: $hex | ASCII: $ascii"
}

Write-Host "USB-CAN kart testi baslatiliyor: $PortName" -ForegroundColor Magenta
Write-Host "Test edilecek baud'lar: $($bauds -join ', ')"

foreach ($b in $bauds) {
    Test-Baud -port $PortName -baud $b
}

Write-Host ""
Write-Host "Test tamamlandi." -ForegroundColor Magenta
Write-Host "Herhangi bir baud'da '(hicbir sey - 0 bayt)' DISINDA bir seyler gorduyseniz,"
Write-Host "o baud/komut kombinasyonunu bana bildirin."
