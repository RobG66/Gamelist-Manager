# Linux Installation - LibVLC

> **Note:** This is an initial draft and may contain errors.

Gamelist Manager uses LibVLC for video preview playback.

---

## Debian / Ubuntu / Linux Mint

```bash
sudo apt install libvlc-dev libvlccore-dev
```

## Fedora / RHEL / CentOS Stream

```bash
sudo dnf install vlc-devel
```

## Arch Linux / Manjaro

```bash
sudo pacman -S vlc
```

## openSUSE

```bash
sudo zypper install vlc-devel
```

---

If your distribution is not listed, install the VLC development libraries from your package manager. Video preview will be silently unavailable if LibVLC is not found at runtime.
