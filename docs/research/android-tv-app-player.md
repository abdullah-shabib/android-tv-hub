# Android TV App Player — research

Primary-source notes for this Hub. The product is a Windows App Player for Android TV OS, not a BlueStacks clone and not Google TV.

## BlueStacks 5 (out of scope)

BlueStacks is a free Windows/macOS App Player for phone games. Official materials describe it as an Android environment with Google Play, keyboard/mouse controls, and gamer tooling.

Sources:

- [What is BlueStacks](https://www.bluestacks.com/blog/bluestacks-exclusives/what-is-bluestacks-en.html)
- [All you need to know about BlueStacks 5](https://support.bluestacks.com/hc/en-us/articles/360055360872-All-you-need-to-know-about-BlueStacks-5)
- [BlueStacks 5 release notes](https://support.bluestacks.com/hc/en-us/articles/360056960211-Release-Notes-BlueStacks-5) (5.22 adds Android 13 beta alongside Pie and 11)

Official feature set we are **not** chasing:

| BlueStacks capability | This Hub |
| --- | --- |
| Google Play Store / Google account sync | Out. AOSP-only Guest. |
| Phone/tablet gaming as the default Device Profile | Out of v1. Phone is v2. |
| Keymapping, Game Controls Editor, scripts | Out. TV Guest uses D-pad. |
| Multi-Instance Manager, instance sync | Out of v1. |
| Macro Recorder, Repeated Tap | Out. |
| Eco Mode, farm/background play | Out. |
| High FPS / renderer picker for games | Out. |
| macOS / Apple Silicon | Out. Windows only. |

BlueStacks is useful as a competitor map, not a spec.

## Android TV OS vs Google TV

Google’s own distinction: Google TV is a personalized experience on top of **Android TV OS**. Devices that run Android TV OS without that launcher are Android TV devices. Apps built for Android TV OS run on Google TV hardware. A device exposes Google TV via `com.google.android.feature.AMATI_EXPERIENCE`.

Sources:

- [Google TV FAQ (Android TV vs Google TV)](https://tv.google/intl/en_uk/)
- [Best practices on Google TV](https://developer.android.com/training/tv/get-started/google-tv)
- [Create and run a TV app](https://developer.android.com/training/tv/get-started/create) (Leanback launcher, `CATEGORY_LEANBACK_LAUNCHER`, touchscreen not required)

This Hub’s Guest is Android TV OS (AOSP / Lineage Leanback). Google TV is out of the glossary and out of the product.

## Official TV system images (not redistributable)

Android Studio Device Manager can create Android TV and Google TV AVDs. System images labeled Google APIs include Play services. The Android SDK license grants use of the SDK to develop apps for compatible Android, and forbids redistributing the SDK (including system images) except where an OSS license on a component says otherwise.

Sources:

- [Create and manage virtual devices](https://developer.android.com/studio/run/managing-avds)
- [Android SDK terms](https://developer.android.com/studio/terms) (§3.1, §3.4, §3.5)
- [AOSP: use Android Emulator virtual devices](https://source.android.com/docs/setup/test/avd) (you may *build* custom AVD images from AOSP and host them yourself)

The Hub must not bundle official Google TV / Play system images. Users who want those images already have Android Studio.

## GMS / Play Store

Google Mobile Services (Play Store, Play Services, and the rest of the GMS suite) are not AOSP. Distribution requires a Mobile Application Distribution Agreement (MADA) and device certification. Without a MADA, an OEM cannot distribute the Play Store.

Sources:

- [AOSP overview](https://source.android.com/docs/setup/about) (AOSP vs GMS; compatibility program)
- [United States v. Google findings, §6B](https://en.wikisource.org/wiki/United_States_v._Google/Findings_of_Fact/Section_6B) (MADA; Play Store as a GMS app)

Sideloading GApps into our Guest would still be redistribution if we shipped them. We do not ship them, and we do not document how to install them.

## Open-source Android-on-PC landscape

| Project | Host | Shape | Play Store | TV | Notes |
| --- | --- | --- | --- | --- | --- |
| [Waydroid](https://waydro.id/) | Linux | LXC container, shared kernel, Wayland | Not in the box (community GApps/microG) | Community ATV builds exist | Not a Windows path. |
| [redroid](https://github.com/remote-android/redroid-doc) | Linux | Docker/container, often headless | Not in the box | Not the product | Cloud/multi-instance, not an HTPC App Player. |
| Anbox / Anbox Cloud | Linux | Container | Limited | No | Anbox desktop is unmaintained; Anbox Cloud is Canonical’s product. |
| [Android-x86](https://www.android-x86.org/) / [Bliss OS](https://blissos.org/) | Bare metal or VM | Full OS ISO | Optional, not our job | Phone/desktop, not Leanback-first | Possible VM Guest, wrong Device Profile. |
| [LineageOS virtio QEMU](https://wiki.lineageos.org/libvirt-qemu) | Linux QEMU/KVM (and portable in principle) | `virtio_x86_64_tv` among other targets | Not in the box | Yes (`virtio_x86_64_tv`) | Closest official-ish TV Guest for QEMU. |
| [LineageOS-TV-x86](https://github.com/LineageOS-TV-x86) | Generic x86 | Android TV ISO/zip | Not in the box | Yes | Closest generic-x86 Android TV OS for an HTPC or VM. |
| AOSP Android Emulator | Win/macOS/Linux | QEMU-based, SDK-distributed builds | Google APIs / Play images exist | TV and Google TV AVDs | Prebuilt emulator + Google images: SDK license. AOSP emulator source is usable; we still consume a Lineage/AOSP *TV* image rather than wrapping Studio. |
| Windows Subsystem for Android | Windows 11 | Utility VM | Amazon Appstore, not Play | No | [Ended 5 March 2025](https://learn.microsoft.com/en-us/previous-versions/windows/android/wsa/). |

Windows cannot share the kernel with Android the way Waydroid does. A Guest in QEMU (or Hyper-V) is the realistic Windows shape.

## Video, 4K, HDR

TiviMate, Nuvio, and Stremio decode inside the Guest (ExoPlayer / Media3). The Hub does not decode video.

- **HDR10 / Dolby Vision:** Real HDR from a Guest needs the Guest to own the display engine (GPU passthrough). A QEMU window composited by DWM is almost always SDR. HDR is out of v1.
- **Widevine L1:** Hardware TEE + certification. Generic x86 VMs are L3 at best. Netflix/Disney-class apps are not a v1 claim. Source example: [waydroid-androidtv-build#11](https://github.com/supechicken/waydroid-androidtv-build/issues/11) (L1 not possible on generic x86).
- **4K HEVC SDR:** Needs in-Guest MediaCodec. virtio-gpu usually does not expose NVDEC/VAAPI. 4K may be software decode and fail. A spike against upstream Lineage TV + QEMU+WHPX gates any 4K *video* claim. 4K *UI* is a QEMU window at 3840×2160.

Nuvio TV is an Android TV client for the Stremio addon ecosystem ([NuvioMedia/NuvioTV](https://github.com/NuvioMedia/NuvioTV)). The Hub may sideload a user-provided APK. It must not bundle addons, debrid configs, or media.

## QEMU on Windows

[QEMU WHPX](https://www.qemu.org/docs/master/system/whpx.html) is the Windows Hypervisor Platform accelerator. It needs the Windows Hypervisor Platform optional feature. virtio-gpu 3D (virgl/venus) on Windows is weaker than on Linux KVM; treat GPU as best-effort.

QEMU is GPL. The Hub must launch it as a **child process**, not link `libqemu`, and must ship license texts if it redistributes the QEMU binary.

## Legal constraints (operating rules)

1. Do not redistribute GMS, Play Store, Google TV, or GApps.
2. Do not bundle Android SDK system images.
3. Do not rehost Lineage images in this git repo; the Hub downloads upstream artifacts.
4. If the Hub redistributes QEMU, include GPL notices and corresponding source offer.
5. Sideload only user-provided APKs. No bundled TiviMate (proprietary), no Stremio addon catalog.

## v1 vs later

**v1:** Windows Hub, TV Device Profile, QEMU+WHPX, upstream Lineage/AOSP TV Guest, keyboard + gamepad D-pad, windowed + exclusive fullscreen, APK sideload, 4K UI if the spike allows, 4K video only if the spike passes.

**Not v1:** Phone Device Profile, Play/microG, F-Droid preinstall, macros, multi-instance, BT remotes, HDR, GPU passthrough, media-app bundles.
