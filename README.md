
<img width="738" height="141" alt="glm" src="https://github.com/user-attachments/assets/6d394150-3a6c-43bc-871e-53a2017fce4e" />

A cross platform (Windows/Linux) desktop application for viewing and editing EmulationStation gamelist XML files, with scrapers for metadata and artwork, built on Avalonia UI and targeting Windows and Linux.

If you find this useful, please consider supporting the project.

**Non-beta Version 9 release coming this weekend, please be patient.**

---

Dark theme scraper showing:
<img width="3840" height="2040" alt="Screenshot 2026-03-14 221657" src="https://github.com/user-attachments/assets/f7a6af8f-b5be-4d07-b52d-a120f0fab2af" />

Light theme media preview showing:
<img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/012379c4-2b10-4d72-bbc0-cd76118bb0bf" />


---

## Version 9.0.x-beta

This is a beta release. Core functionality is working and stable. Some menu items are not yet active, some features have not been added - yet.

---

## Compatibility

Gamelist Manager works with any system running EmulationStation that stores its data in the standard `gamelist.xml` format. The following systems have built-in templates, but require further testing.

| System | Notes |
|--------|-------|
| Batocera | Full support, default credentials pre-filled |
| Knulli | Full support |
| Recalbox | Full support |
| RetroPie | Full support |
| JELOS | Full support |
| ArkOS | Full support |
| RetroBat | Windows-local, no remote connection needed |

Any other EmulationStation-based system should work fine with a manual connection setup.

---

## Profiles

Profiles are the main way to manage multiple devices or configurations. Each profile is a separate settings file that stores its own connection details, ROM paths, media folder locations, and scraper preferences independently. You can switch between profiles from the navigation panel without restarting.

**What a profile stores:**
- Remote hostname, username, and password
- ROMs root folder path
- MAME executable path
- All media subfolder paths and filename suffixes
- Scraper service selection and credentials
- Behavioral settings (save reminder, bulk change confirmation, etc.)

**Profile management:**
- Create a blank profile or copy from the current active one
- Create from a system template to pre-fill connection settings automatically
- Rename or delete profiles at any time (the active profile and the last remaining profile cannot be deleted)
- Switch profiles instantly from the navigation flyout

Templates are stored in `Ini/templates.ini` and can be edited to add custom systems or pre-fill different defaults.

---

## Features

### Gamelist Editing

- Load, save, and reload `gamelist.xml` files
- Sortable, resizable DataGrid with toggleable columns
- Edit any metadata field directly in the grid
- Undo/redo with a configurable history depth (WIP)
- Bulk operations on selected rows
- Genre and custom column filters (Needs more testing)
- Show All / Visible Only / Hidden Only display modes
- Stats bar showing total, filtered, visible, hidden, and favorite counts
- Duplicate ROM path detection when loading (WIP)
- Automatic backup created before each save
- Save reminder when closing with unsaved changes

### Scraping

Three scrapers are available. Each is configured separately per profile.

**ArcadeDB** - Free, no account required, arcade-focused. Supports batch scraping to process large lists quickly.

**EmuMovies** - Account required. Strong media library.  Video downloads may require subscription.

**ScreenScraper** - Account required. Comprehensive multi-system database with language and region selection. 

Scraper options per run:
- Scrape all games or selected rows only
- Overwrite existing name, metadata, and/or media independently
- Skip games not in cache, or scrape from cache only
- Configurable log verbosity (Minimal, Normal, Verbose) with optional timestamps

### Media

- 17 configurable media types: image, titleshot, marquee, wheel, thumbnail, cartridge, video, music, map, bezel, manual, fanart, boxart, boxback, magazine, mix
- Each type has its own subfolder path and optional filename suffix, all configurable per profile
- Inline media preview panel for images and video
- Right-click context menu on media paths: open, open with, open file location, copy path, clear, delete
- Optional image verification after download to catch corrupt files
arcade ROM filenames to their proper full titles. This is used during scraping to improve match accuracy.

### Appearance and Scaling

- Light and Dark themes with 11 accent color choices
- Adjustable global UI font size and DataGrid font size independently
- Layout scales proportionally with font size, including the nav panel, window dimensions, and icon buttons
- Alternating row colors and grid line visibility options for the DataGrid
- Column visibility and auto-fit state can optionally be remembered between sessions

---

## Requirements

| Platform | Requirement |
|----------|-------------|
| Windows | None - self-contained single executable, no install needed |
| Linux | LibVLC required for video preview (see LINUX.md) |
