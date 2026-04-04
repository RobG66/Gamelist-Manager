# Changelog

All notable changes to Gamelist Manager will be documented in this file.

## [9.0.0-beta] - 2025-03-02

### Added
- Cross-platform Avalonia UI rewrite targeting Windows and Linux
- DataGrid view with sortable, reorderable, resizable columns for all gamelist fields
- Integrated scraper panel (collapsible sidebar) supporting ArcadeDB, EmuMovies, and ScreenScraper
- Multi-threaded scraping with live progress bar and time-remaining estimate
- Per-scraper metadata field selection (Name, Description, Genre, Players, Rating, Region, Language, Release Date, Developer, Publisher, Arcade Name, Family, Game ID)
- Per-scraper media type selection with configurable source per media type (Image, Marquee, Thumbnail, Cartridge, Video, Box Art, Wheel, Titleshot, Manual, Map, Fan Art, Box Back, Music)

### Notes
- Linux release requires LibVLC installed on the target system for video preview playback (see `LINUX.md` for distro-specific instructions)
