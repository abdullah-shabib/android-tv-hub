# Third-party notices

The Hub (this repository) is Apache-2.0. It launches other programs that are **not** Apache-2.0. Those programs are downloaded or installed on the user's machine; they are not stored in this git repository.

## QEMU

QEMU is licensed under GPL-2.0-or-later (and other licenses for individual files). The Hub starts `qemu-system-x86_64` as a **child process** and does not link `libqemu`.

- Project: https://www.qemu.org/
- Windows builds used in development: https://qemu.weilnetz.de/
- See ADR 0002.

If you redistribute a QEMU binary next to the Hub, you must include QEMU's GPL notices and offer corresponding source.

## LineageOS TV x86 Guest images

Guest ISOs are fetched from upstream at runtime (SourceForge / GitHub). LineageOS is primarily Apache-2.0 with other notices in the source tree. Do not vendor ISOs in this repo.

- https://github.com/LineageOS-TV-x86
- https://sourceforge.net/projects/lineageos-tv-x86/

## Android platform-tools (ADB)

`adb` is part of Google's Android SDK Platform-Tools. The Hub may download it from Google's official URL for the user's machine. It is not copied into this git repository. Use is subject to the Android SDK license.

- https://developer.android.com/tools/releases/platform-tools
- https://developer.android.com/studio/terms
