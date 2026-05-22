$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# CPU Temperature (via ACPI thermal zone)
$tempC = "N/A"
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

# Top process
$topProc = Get-Process | Sort-Object CPU -Descending | Select-Object -First 1

# GPU temp fallback via WMI video controller
$gpuInfo = ""
try {
    $gpu = Get-WmiObject -Namespace root/wmi -Class WmiMonitorTemperature -ErrorAction SilentlyContinue
    if ($gpu) { $gpuInfo = " | GPU: $($gpu.CurrentTemperature)" }
} catch {}

Write-Host "[$timestamp] CPU: ${tempC}C | Load: ${load}% | Clock: ${clock}MHz | TopProc: $($topProc.ProcessName)($([math]::Round($topProc.CPU,1))s CPU, $([math]::Round($topProc.WorkingSet64/1MB,1))MB)$gpuInfo"
