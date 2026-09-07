# Android TV Hub

The Hub is a Windows App Player that boots an Android TV OS Guest so a PC can run Leanback apps with a 10-foot UI.

## Language

**Hub**:
The Windows program that installs dependencies, manages Guest lifecycle, sideloads APKs, and holds settings.
_Avoid_: emulator UI, BlueStacks, Android Studio, launcher

**Guest**:
The Android TV OS that runs inside QEMU.
_Avoid_: ROM, VM image as a synonym for the OS itself, Google TV

**Device Profile**:
The form factor of a Guest. v1 is TV only; Phone is a later profile on the same Hub.
_Avoid_: skin, AVD, device type as a Windows-side setting only

**Android TV OS**:
The AOSP / Lineage Leanback Guest. It is not Google TV.
_Avoid_: Google TV, AndroidTV as a product name, Shield

**App Player**:
The Hub and Guest together, from the user's point of view.
_Avoid_: emulator as the product name, hypervisor, virtual machine as the product name

**Sideload**:
Installing an APK the user provides, via the Hub and ADB.
_Avoid_: store, catalog, GApps, Play Store, F-Droid as a bundled feature
