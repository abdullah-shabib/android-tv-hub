---
status: accepted
---

# Consume upstream TV Guest images

4K UI and 4K in-Guest decode may need kernel/MediaCodec work, but maintaining an Android TV x86 ROM is a different project. The Hub downloads pinned upstream Lineage Android TV x86 artifacts (`virtio_x86_64_tv` and/or LineageOS-TV-x86). We do not vendor Guest images in git.

**Considered options:** fork and maintain a Guest ROM; build AOSP `tv` x86_64 ourselves as the official image.

**Consequences:** 4K HEVC is best-effort against upstream. If the QEMU spike cannot play 4K video, v1 drops 4K video (keeps 4K UI if possible) instead of standing up a ROM team. Images are fetched at runtime from upstream URLs.
