#Requires -Version 5.1
<#
.SYNOPSIS
  Spike: Lineage Android TV x86 under QEMU+WHPX on Windows.
#>
param(
    [int]$UiWidth = 1920,
    [int]$UiHeight = 1080,
    [int]$RamMb = 4096,
    [int]$Cpus = 4,
    [int]$AdbPort = 5555,
    [int]$BootWaitSeconds = 240,
    [switch]$FourK
)

$ErrorActionPreference = "Stop"
$root = Join-Path $env:LOCALAPPDATA "AndroidTvHub"
$iso = Join-Path $root "images\lineage-21.0-20260331-UNOFFICIAL-x86_64_tv-signed.iso"
$disk = Join-Path $root "disks\spike-tv-guest.qcow2"
$reportDir = Join-Path $PSScriptRoot "..\..\docs\research"
$qemu = "C:\Program Files\qemu\qemu-system-x86_64.exe"
$qemuImg = "C:\Program Files\qemu\qemu-img.exe"
$adb = Join-Path $root "tools\platform-tools\adb.exe"

if ($FourK) {
    $UiWidth = 3840
    $UiHeight = 2160
}

New-Item -ItemType Directory -Force -Path (Join-Path $root "disks"), (Join-Path $root "logs"), (Join-Path $root "tools") | Out-Null

function Write-Step($name, $ok, $detail) {
    $mark = if ($ok) { "PASS" } else { "FAIL" }
    "{0,-28} {1,-4} {2}" -f $name, $mark, $detail
}

$results = @()

$qemuOk = Test-Path $qemu
$results += [pscustomobject]@{ Name = "QEMU binary"; Ok = $qemuOk; Detail = $(if ($qemuOk) { & $qemu --version | Select-Object -First 1 } else { "missing $qemu" }) }

$accel = & $qemu -accel help 2>&1 | Out-String
$whpxListed = $accel -match "whpx"
$results += [pscustomobject]@{ Name = "QEMU lists WHPX"; Ok = $whpxListed; Detail = $accel.Trim() }

$err = Join-Path $env:TEMP "spike-whpx.err"
$out = Join-Path $env:TEMP "spike-whpx.out"
Remove-Item $err, $out -ErrorAction SilentlyContinue
$probe = Start-Process -FilePath $qemu -ArgumentList @("-accel","whpx,kernel-irqchip=off","-machine","q35","-m","256","-display","none","-serial","none","-monitor","none") -PassThru -RedirectStandardError $err -RedirectStandardOutput $out -WindowStyle Hidden
Start-Sleep -Seconds 3
$whpxRun = -not $probe.HasExited
if ($whpxRun) { Stop-Process -Id $probe.Id -Force -ErrorAction SilentlyContinue }
$results += [pscustomobject]@{ Name = "WHPX accelerator"; Ok = $whpxRun; Detail = $(if ($whpxRun) { "QEMU stayed up on WHPX" } else { Get-Content $err -ErrorAction SilentlyContinue }) }

$isoOk = (Test-Path $iso) -and ((Get-Item $iso).Length -gt 1GB)
$results += [pscustomobject]@{ Name = "Guest ISO present"; Ok = $isoOk; Detail = $(if (Test-Path $iso) { "{0:N1} MB $iso" -f ((Get-Item $iso).Length/1MB) } else { "missing $iso" }) }

if ($isoOk -and -not (Test-Path $disk)) {
    & $qemuImg create -f qcow2 $disk 64G | Out-Null
}
$results += [pscustomobject]@{ Name = "qcow2 disk"; Ok = (Test-Path $disk); Detail = $disk }

$adbReady = $false
if ($isoOk -and $whpxRun) {
    if (-not (Test-Path $adb)) {
        $zip = Join-Path $root "tools\platform-tools.zip"
        curl.exe -L -o $zip "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
        Expand-Archive -Path $zip -DestinationPath (Join-Path $root "tools") -Force
    }
    $adbReady = Test-Path $adb
}
$results += [pscustomobject]@{ Name = "adb available"; Ok = $adbReady; Detail = $adb }

$bootOk = $false
$leanback = $false
$sizeLine = ""
$hevc = $false
$ui4k = $false
$qemuProc = $null

$bootDir = Join-Path $root "boot"
$kernel = Join-Path $bootDir "kernel"
$initrd = Join-Path $bootDir "initrd.img"
if ($isoOk -and $whpxRun) {
    $args = @(
        "-accel","whpx,kernel-irqchip=off",
        "-machine","q35",
        "-cpu","qemu64",
        "-smp","$Cpus",
        "-m","$RamMb",
        "-device","virtio-vga,xres=${UiWidth},yres=${UiHeight}",
        "-display","sdl,gl=off",
        "-usb","-device","usb-kbd","-device","usb-tablet",
        "-device","qemu-xhci,id=xhci",
        "-netdev","user,id=net0,hostfwd=tcp::${AdbPort}-:5555",
        "-device","virtio-net-pci,netdev=net0",
        "-drive","file=$disk,if=virtio,format=qcow2",
        "-cdrom",$iso,
        "-name","AndroidTvHub-spike"
    )
    if ((Test-Path $kernel) -and (Test-Path $initrd)) {
        $args += @("-kernel",$kernel,"-initrd",$initrd,"-append","root=/dev/ram0 androidboot.live=true ROOT=LABEL=LineageOS_20260331 androidboot.selinux=permissive")
        Write-Host "Starting QEMU with extracted kernel/initrd (no GRUB)."
    } else {
        $args += @("-boot","order=d")
        Write-Host "Starting QEMU (live ISO). In GRUB pick the live/TV boot entry if prompted."
    }
    $qemuProc = Start-Process -FilePath $qemu -ArgumentList $args -PassThru
    if ($adbReady) {
        & $adb start-server | Out-Null
        $deadline = (Get-Date).AddSeconds($BootWaitSeconds)
        while ((Get-Date) -lt $deadline) {
            & $adb connect "127.0.0.1:$AdbPort" | Out-Null
            $devs = & $adb devices
            if ($devs -match "127.0.0.1:$AdbPort\s+device") {
                $bootOk = $true
                break
            }
            Start-Sleep -Seconds 8
        }
        if ($bootOk) {
            $feats = & $adb -s "127.0.0.1:$AdbPort" shell pm list features 2>$null | Out-String
            $leanback = $feats -match "android.software.leanback"
            $sizeLine = (& $adb -s "127.0.0.1:$AdbPort" shell wm size 2>$null | Out-String).Trim()
            $ui4k = $sizeLine -match "3840"
            $codecs = & $adb -s "127.0.0.1:$AdbPort" shell "cat /vendor/etc/media_codecs.xml /system/etc/media_codecs.xml 2>/dev/null" | Out-String
            $hevc = $codecs -match "HEVC" -or $codecs -match "hevc" -or $codecs -match "h265"
        }
    }
}

$results += [pscustomobject]@{ Name = "Guest ADB boot"; Ok = $bootOk; Detail = $(if ($bootOk) { "adb device at 127.0.0.1:$AdbPort" } else { "no adb within ${BootWaitSeconds}s (GRUB/live boot may need a keypress)" }) }
$results += [pscustomobject]@{ Name = "Leanback feature"; Ok = $leanback; Detail = $(if ($leanback) { "android.software.leanback" } else { "not observed" }) }
$results += [pscustomobject]@{ Name = "4K UI (wm size)"; Ok = $ui4k; Detail = $sizeLine }
$results += [pscustomobject]@{ Name = "HEVC decoder listed"; Ok = $hevc; Detail = $(if ($hevc) { "media_codecs.xml mentions HEVC" } else { "no HEVC in media_codecs.xml or Guest not booted" }) }
$results += [pscustomobject]@{ Name = "Keyboard/gamepad"; Ok = $whpxRun -and $isoOk; Detail = "QEMU SDL usb-kbd/usb-tablet; host SDL gamepads map into the QEMU window" }

Write-Host ""
Write-Host "=== spike results ==="
$results | ForEach-Object { Write-Step $_.Name $_.Ok $_.Detail }

$md = @()
$md += "# QEMU + WHPX spike"
$md += ""
$md += "Recorded $(Get-Date -Format 'yyyy-MM-dd HH:mm') on $env:COMPUTERNAME. Guest ISO: ``$iso``. QEMU window is SDL; Hub does not decode video."
$md += ""
$md += "| Check | Result | Detail |"
$md += "| --- | --- | --- |"
foreach ($r in $results) {
    $mark = if ($r.Ok) { "PASS" } else { "FAIL" }
    $detail = ([string]$r.Detail) -replace '\|','/' -replace '\r?\n',' '
    $md += "| $($r.Name) | $mark | $detail |"
}
$md += ""
$md += "## Decision for v1"
if (-not $hevc) {
    $md += "4K HEVC in-Guest decode was **not proven**. Per the locked plan, **drop 4K video from v1** if this stays FAIL. Keep 4K UI as a Hub setting (3840x2160 QEMU window) when WHPX+QEMU run."
} else {
    $md += "HEVC decoder is present in the Guest. That is not the same as smooth 4K playback; virtio-vga on Windows still likely software-decodes. Treat 4K video as best-effort until a legal HEVC clip plays without dropping to unusable CPU decode."
}
$md += ""
$md += "Do not claim TiviMate/Nuvio 4K until a user-provided APK actually plays a legal 4K HEVC file in this Guest."
$md += ""

$report = Join-Path (Resolve-Path $reportDir) "qemu-whpx-spike.md"
Set-Content -Path $report -Value ($md -join "`n") -Encoding UTF8
Write-Host ""
Write-Host "Wrote $report"

if ($qemuProc -and -not $qemuProc.HasExited) {
    Write-Host "Leaving QEMU running (pid $($qemuProc.Id)) so the Guest window can be inspected. Stop it from Task Manager or the Hub."
}
