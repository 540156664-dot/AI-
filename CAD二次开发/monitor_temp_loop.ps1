$logFile = Join-Path $PSScriptRoot "temp_log.txt"
$duration = 600  # 10 minutes total
$interval = 10   # every 10 seconds
$endTime = (Get-Date).AddSeconds($duration)

Write-Host "=== Temperature Monitor Started ==="
Write-Host "Duration: ${duration}s | Interval: ${interval}s | Log: temp_log.txt"
Write-Host "Press Ctrl+C to stop"
Write-Host ""

"=== Temperature Log $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File $logFile

$maxTemp = 0
$minTemp = 999

while ((Get-Date) -lt $endTime) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    # CPU temp
    $tempC = 0
    try {
        $tz = Get-WmiObject MSAcpi_ThermalZoneTemperature -Namespace root/wmi -ErrorAction SilentlyContinue
        if ($tz) {
            $tempK = $tz.CurrentTemperature / 10.0
            $tempC = [math]::Round($tempK - 273.15, 1)
        }
    } catch {}

    # CPU Load
    $cpu = Get-WmiObject Win32_Processor
    $load = $cpu.LoadPercentage
    $clock = $cpu.CurrentClockSpeed

    # Track min/max
    if ($tempC -gt $maxTemp) { $maxTemp = $tempC }
    if ($tempC -lt $minTemp) { $minTemp = $tempC }

    # Color-coded output
    $color = "White"
    if ($tempC -ge 80) { $color = "Red" }
    elseif ($tempC -ge 70) { $color = "Yellow" }

    $line = "[$timestamp] CPU: ${tempC}C | Load: ${load}% | Clock: ${clock}MHz"
    Write-Host $line
    $line | Out-File $logFile -Append

    Start-Sleep -Seconds $interval
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Min Temp: ${minTemp}C | Max Temp: ${maxTemp}C | Duration: ${duration}s"
Write-Host "Log saved to: $logFile"
