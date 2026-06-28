# lib/ — Native Libraries

This folder holds the libmpv native libraries required for video playback.
**The binaries themselves are NOT committed to git** (see `.gitignore`).
Clone operators must populate this folder locally before building.

## Windows

Place `libmpv-2.dll` in this folder.

Source: https://sourceforge.net/projects/mpv-player-windows/files/libmpv/

Pick a recent `libmpv` archive (e.g. `libmpv-2-x86_64-*.7z`), extract it,
and copy `libmpv-2.dll` here.

## Linux

No file required in this folder on Linux. Install libmpv via your package manager:

```bash
# Debian / Ubuntu
sudo apt install libmpv2

# Fedora
sudo dnf install libmpv

# Arch
sudo pacman -S libmpv
```

The runtime resolves `libmpv.so.2` via the system loader path.

## Build Behavior

The `Gamelist_Manager.csproj` includes `lib\*.dll`, `lib\*.so`, and `lib\*.so.*`
as content files copied to the output directory. The output folder layout
preserves the `lib/` subfolder, so the loader can find the binaries via
`<appdir>/lib/libmpv-2.dll` on Windows.

## Runtime Detection

`MpvService.IsNativeLibraryPresent()` probes for the library at startup:

- Windows: looks for `lib/libmpv-2.dll` under the app base directory, then
  falls back to the system search path.
- Linux: uses `NativeLibrary.TryLoad("libmpv.so.2")` which relies on the
  system loader.

If the library is missing, video previews are silently disabled and a
warning is printed to the console. The rest of the application continues
to function normally.
