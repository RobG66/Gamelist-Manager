# Linux Installation - libmpv

Gamelist Manager uses libmpv for video preview playback.

---

## Debian / Ubuntu / Linux Mint

```bash
sudo apt install libmpv2
```

## Fedora / RHEL / CentOS Stream

```bash
sudo dnf install libmpv
```

## Arch Linux / Manjaro

```bash
sudo pacman -S libmpv
```

## openSUSE

```bash
sudo zypper install libmpv2
```

---

If your distribution is not listed, install libmpv from your package manager.
Video preview will be silently unavailable if libmpv is not found at runtime.

The runtime resolves `libmpv.so.2` via the system loader path; no bundled
binary is required on Linux.
