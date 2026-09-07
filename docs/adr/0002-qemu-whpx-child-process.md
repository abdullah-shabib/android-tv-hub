---
status: accepted
---

# QEMU + WHPX as a child process

Windows cannot run Waydroid. A custom hypervisor would be a multi-year engine. Hyper-V Manager is a poor App Player UX and GPU-PV does not give the Guest the HDMI for “Android as a window on the dGPU.” We run upstream QEMU with the WHPX accelerator and launch it as a **child process** so the Hub (Apache-2.0 C#) does not link `libqemu` (GPL).

**Considered options:** Hyper-V Gen2 VMs; GPU passthrough / DDA; wrapping the Android Studio emulator; a from-scratch engine.

**Consequences:** QEMU owns the Guest framebuffer (SDL/D3D window). The Hub is the control surface, not an in-process renderer. If we redistribute QEMU.exe we must ship GPL notices and a corresponding-source offer.
