<p align="center">

<img width="472" height="249" alt="gamelistmanager" src="https://github.com/user-attachments/assets/d57bc076-e36c-46e7-9116-8d3793fe283d" />


A robust, cross platform (Windows/Linux) desktop application for viewing and editing EmulationStation gamelist XML files, with fullly configurable scraper services for metadata and artwork.  Built on Avalonia UI and targeting Windows and Linux.  


If you find this program useful, please consider supporting its continued development.

[![GitHub](https://img.shields.io/badge/GitHub-RobG66-181717?style=for-the-badge&logo=github)](https://github.com/RobG66)
[![Ko-Fi](https://img.shields.io/badge/Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/robg66)


<img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/4b486149-e82d-4638-9329-a9e7fafd6aab" />

<img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/ba7bccae-38d6-4122-a320-c68ad5e8cb81" />

<img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/c7b36788-3eab-473c-b6a2-e12ee38c2596" />

<img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/132cb76e-6d0f-4dba-abd3-d0f8f4d158c9" />


</p>

## Features

### Scraping
- Scrape metadata and media from **ArcadeDB**, **ScreenScraper**, and **EmuMovies**
- 15+ media types including screenshots, box art, videos, bezels, wheels, cartridges, marquees, fan art, and manuals
- Multi-threaded scraping with local caching for speed
- Per media type source selection — mix and match scrapers
- Overwrite protection — choose what to update and what to preserve
- Automatic filtering of corrupt or single-color images
- Configurable language, region, and fallback settings
- Progress monitoring and logging

### Metadata Editing
- Bulk edit genres, descriptions, publishers, release dates, ratings, and player counts
- Find and replace across all fields with filtering
- Mark games as favorites or hidden
- CSV export for external analysis
- Automatic backups on save

### Image Editor
- Crop, resize, and remove backgrounds from media images
- Edit images directly without leaving the app

### Collection Management
- Scan ROM directories to find games missing from your gamelist
- Identify gamelist entries with no matching file on disk
- Auto-link existing media to game *(coming soon)*
- Find and remove orphaned media files *(coming soon)*
- Scan for bad, missing, or unnecessary media *(coming soon)*
- DAT file import and romset analysis for **MAME** and **FBNeo**

### Batocera Remote Management
- Remote SSH commands
- Network drive mapping to Batocera shares
- Check system version and available updates
- Control EmulationStation — restart, reboot, or shutdown remotely

### Interface
- Cross-platform — runs natively on **Windows** and **Linux**
- Built-in image and video preview
- Drag and drop media from disk or web browser
- Multiple color themes
- Customizable fonts, column visibility, and grid spacing
- Advanced filtering and search
- Recent files history and quick system switching

## Compatibility
Gamelist Manager is designed to work with any system running EmulationStation that stores its
data in the standard `gamelist.xml` format. ES-DE is now supported!

## Documentation

 Documentation is currently a work in progress.  However, the program is design to be intuitive and easy to use.

## Requirements

| Platform | Requirement |
|----------|-------------|
| Windows | None - self-contained single executable, no install needed |
| Linux | LibVLC required for video preview (see LINUX.md) |
