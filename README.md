# Android TV Hub

A Windows App Player for **Android TV OS**. The Hub is a WinUI 3 control surface; QEMU+WHPX runs an upstream Lineage Android TV Guest. Sideload APKs you provide. No Play Store, no Google TV, no bundled media apps.

See [CONTEXT.md](CONTEXT.md) for the glossary, [docs/adr/](docs/adr/) for the locked decisions, and [docs/research/android-tv-app-player.md](docs/research/android-tv-app-player.md) for the competitor/legal map.

## Requirements

- Windows 10 2004+ or Windows 11, x64
- CPU virtualization enabled in firmware
- [Windows Hypervisor Platform](https://learn.microsoft.com/en-us/virtualization/api/) (QEMU lists this as `whpx`)
- [QEMU for Windows](https://qemu.weilnetz.de/) (`winget install SoftwareFreedomConservancy.QEMU`)
- .NET 8 SDK to build
- Windows App SDK runtime 1.6 (`winget install Microsoft.WindowsAppRuntime.1.6`) — already present on many Windows 11 PCs

## Build

```powershell
dotnet build src\AndroidTvHub\AndroidTvHub.csproj -c Release -p:Platform=x64
```

Run `src\AndroidTvHub\bin\x64\Release\net8.0-windows10.0.19041.0\AndroidTvHub.exe`.

## Release package

GitHub Actions builds an unpackaged **win-x64 zip** on every push/PR and attaches it to a [GitHub Release](https://github.com/abdullah-shabib/android-tv-hub/releases) when you push a tag `vMAJOR.MINOR.PATCH`:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The zip contains `AndroidTvHub.exe` plus LICENSE notices. It does **not** include QEMU or the Guest ISO.

Locally:

```powershell
./tools/pack-release.ps1 -Version 0.1.0
```

## v1

- Download the pinned Lineage TV x86 ISO (not stored in git)
- Start/stop QEMU (windowed or exclusive fullscreen)
- Keyboard + gamepad via QEMU SDL
- Sideload an APK over ADB (`localhost:5555`)
- CPU / RAM settings

- 4K UI as a window size; **4K video is not a v1 claim** (see the QEMU spike note)
