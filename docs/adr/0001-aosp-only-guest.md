---
status: accepted
---

# AOSP-only Guest

This Hub is an open-source App Player. Google TV, Play Store, and the rest of GMS are proprietary and require a MADA plus device certification; the Android SDK also forbids redistributing official system images. We ship and boot only AOSP or Lineage Android TV OS Guests, with no GApps in the box.

**Considered options:** wrap official Google TV / Play AVD images (user-accepted SDK license); document microG; bundle GApps.

**Consequences:** many Play-only TV apps will not install from a store. Sideload is the only v1 install path. Reversing this decision means abandoning the open-source positioning.
