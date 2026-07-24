# lib/ — native runtime libraries

All third-party native runtime libraries go in this folder, flat —
no subfolders. Windows `.dll` and Linux `.so` files coexist by
extension; the Jukebox loader code picks the right filename per OS
at runtime.

This folder is intentionally empty in the repository. You must
populate it manually before running Jukebox. The Jukebox checks at
startup and will show a clear error dialog listing what's missing.

---

## Required libraries (audio + video — always needed)

### Windows
| File | Source | License |
|------|--------|---------|
| `bass.dll` | https://www.un4seen.com/ (download `bass24.zip`, 64-bit) | Proprietary, non-commercial |
| `libmpv-2.dll` | https://sourceforge.net/projects/mpv-player-windows/files/libmpv/ (download latest `mpv-dev-x86_64-*.7z`, extract with 7-Zip, find `libmpv-2.dll` inside) | GPL v2+ (or LGPL if built with `--enable-lgpl`) |

### Linux
| File | Source | License |
|------|--------|---------|
| `libbass.so` | https://www.un4seen.com/ (download `bass24-linux.zip`) | Proprietary, non-commercial |
| `libmpv.so.2` | `sudo apt install libmpv-dev` (places it in `/usr/lib/x86_64-linux-gnu/`), OR download from https://github.com/mpv-player/mpv releases | GPL v2+ (or LGPL if built with `--enable-lgpl`) |

> **Linux alternative:** if you install `libmpv-dev` via apt, the Jukebox
> will find `libmpv.so.2` on the system library path even if it's not in
> `lib/`. The loader falls back to the OS default search path.

---

## Optional libraries (visualizer — install as plugin to enable ProjectM)

If you want music visualizations, download the `ProjectM-Avalonia-<platform>.zip` for your platform from [https://github.com/RobG66/Avalonia.ProjectM](https://github.com/RobG66/Avalonia.ProjectM) and extract it to the `plugins/` directory (resulting in a `plugins/ProjectM.Avalonia/` folder).
Alternatively, you can place the files flat inside `lib/` as a fallback.

### Windows (Plugin files)
| File | Source | License |
|------|--------|---------|
| `ProjectM.Avalonia.dll` | Build from https://github.com/RobG66/Avalonia.ProjectM — run `build.ps1`, unzip `ProjectM-Avalonia-win-x64.zip`, find this file inside the `ProjectM.Avalonia` folder | MIT (the wrapper itself) |
| `ProjectM.Avalonia.deps.json` | Same as above | MIT |
| `libprojectM.dll` | Same zip as above — built from source by CI | LGPL v2.1+ |
| `glew32.dll` | Same zip as above — required by `libprojectM.dll` on Windows | BSD 3-Clause / MIT |

### Linux (Plugin files)
| File | Source | License |
|------|--------|---------|
| `ProjectM.Avalonia.dll` | Same as Windows — pure managed IL, identical file | MIT |
| `ProjectM.Avalonia.deps.json` | Same zip | MIT |
| `libprojectM.so.4` | Same zip — built from source by CI | LGPL v2.1+ |

> The separate `ProjectM/` folder with `presets/` (9,400+ `.milk` files) and `textures/` can be placed
> in the Jukebox's build output directory (next to `Jukebox.exe`).

---

## What the final layout looks like

After populating `lib/` and dropping in the `ProjectM/` folder, your
Jukebox build output directory should look like:

```
<appdir>/
├── Jukebox.exe
├── lib/                               ← required native runtimes, flat
│   ├── bass.dll                       (Windows — BASS audio)
│   ├── libbass.so                     (Linux   — BASS audio)
│   ├── libmpv-2.dll                   (Windows — libmpv video)
│   └── libmpv.so.2                    (Linux   — libmpv video)
├── plugins/
│   └── ProjectM.Avalonia/             ← visualizer plugin directory (optional)
│       ├── ProjectM.Avalonia.dll      (managed wrapper)
│       ├── ProjectM.Avalonia.deps.json (wrapper dependency manifest)
│       ├── libprojectM.dll            (Windows — ProjectM, optional)
│       ├── libprojectM.so.4           (Linux   — ProjectM, optional)
│       └── glew32.dll                 (Windows — required by libprojectM.dll)
└── ProjectM/                          ← preset data only (optional)
    ├── presets/
    │   └── (... .milk files)
    └── textures/
```

---

## Licensing notes

See [../THIRD_PARTY_LICENSES.md](../THIRD_PARTY_LICENSES.md) for the
full licensing breakdown. Key points:

- **BASS** is proprietary — free for non-commercial use only.
- **libmpv** default builds are GPL v2+. If you redistribute Jukebox
  with libmpv bundled, ensure your licensing accommodates this (or use
  an LGPL build).
- **libprojectM** is LGPL v2.1+ — dynamic linking is fine, but the
  LICENSE file must accompany the binary (the ProjectM-Avalonia
  release zip includes it as `libprojectM-LICENSE.txt`).
- **GLEW** is BSD/MIT — no real restrictions.

---

## Why we don't commit these to git

Third-party binaries carry licensing obligations (GPL/LGPL/proprietary)
that we don't want entangled with the repo's history. Manual placement
makes the obligations explicit: you accept each library's license by
downloading it yourself.

See [../DEPENDENCIES.md](../DEPENDENCIES.md) for more details.
