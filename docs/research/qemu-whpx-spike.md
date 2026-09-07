# QEMU + WHPX spike

Recorded 2026-09-07 on DESKTOP-5TRH89C. Guest ISO: `lineage-21.0-20260331-UNOFFICIAL-x86_64_tv-signed.iso` (SHA-256 `29c44bb7bb0cb6531a11e3778377c985c4c96b881b2666fba3901e4c21d67bc2`). QEMU 11.1.0. The Hub does not decode video.

## Machine

- QEMU lists `whpx` and `tcg`.
- A short-lived `qemu-system-x86_64 -accel whpx,kernel-irqchip=off` stayed up (WHPX accepted).
- Host: Windows 11 build 26100 (Hyper-V PV; serial log: `Hyper-V: Host Build 10.0.26100.9168`).

## ISO / GRUB

The ISO is LineageOS 21.0 TV x86 (`TITLE="LineageOS 21.0"`). GRUB timeout is 30s. Default live args:

`root=/dev/ram0 androidboot.live=true ROOT=LABEL=LineageOS_20260331`

Boot files extracted: `KERNEL` (21.5 MB), `INITRD.IMG` (1.8 MB).

## Direct kernel boot (better than waiting on GRUB)

Command shape: `-kernel` / `-initrd` / `-append` as above, `-cdrom` the ISO, virtio disk, virtio-vga 1920×1080, SDL, USB keyboard+tablet, `hostfwd=tcp::5555-:5555`, `console=ttyS0`.

Serial evidence:

- Linux `6.18.18-zenith+` (Android clang 22).
- `Run /init as init process`.
- USB HID: `QEMU USB Keyboard`, `QEMU USB Tablet`.
- `apexd: Started` and preinstalled APEX (`com.android.art`, `com.android.runtime`, …).
- KernelSU aborted its own init on this CPU (`X86_FEATURE_INDIRECT_SAFE is not enabled`) but did **not** panic the kernel.

## ADB / Leanback / 4K

| Check | Result | Detail |
| --- | --- | --- |
| QEMU binary | PASS | 11.1.0 |
| WHPX accelerator | PASS | process stays up |
| Guest ISO | PASS | 2.44 GiB, checksum as published |
| Kernel + userspace start | PASS | serial: `/init`, apexd |
| Keyboard/tablet | PASS | kernel enumerates QEMU USB HID |
| ADB TCP | FAIL | `127.0.0.1:5555` shows **offline** (port open, adbd not usable yet) |
| Leanback feature | FAIL | not measured (needs working adb) |
| 4K UI (`wm size`) | FAIL | not measured |
| HEVC decoder | FAIL | not measured; virtio-vga still implies software decode |

A first GRUB-only run (`-boot order=d`) also failed ADB within 300s.

## Decision for v1

**Drop 4K video from v1.** In-Guest 4K HEVC was not proven. Keep 4K UI as a Hub setting (3840×2160 QEMU window).

Do not claim TiviMate/Nuvio 4K until a user-provided APK plays a legal 4K HEVC file on a **device** (not offline) ADB session.

The Hub boots with extracted kernel/initrd so GRUB is not required. Sideload remains best-effort until the Guest brings adbd online (developer options / TCP ADB).
