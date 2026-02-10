![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/fa68ee19-06c4-4d7c-b468-8d8b63b4ac5f" />

### 💖 Support Development

**If you find Gamelist Manager helpful, consider supporting its new cross-platform development**

Version 8 is not wine compatible.  It uses API that are simply not supported in that environment.

---

## What it does

Manage and scrape game metadata and media from ArcadeDB, ScreenScraper, or EmuMovies. Edit hundreds of games at once. Manage large ROM collections without touching gamelist xml files directly.

**Scraping**
- Choose different available sources for each many media types.
- Scrape more media types such as cartridge
- Scrape individual metadata or media items, or batch process your entire collection at once
- Fast, multi-threaded scraping with local caching. 
- 15+ media types: screenshots, box art, videos, bezels, manuals, wheels, cartridges, marquees, fan art, and more
- Overwrite protection - choose what to update and what to keep
- Inline media scanning means bad images (corrupt, single color) are automatically discarded
- Progress monitoring and saved logs
- Configurable language and region settings, with additional user selectable fallback settings (screenscraper)

**Editing**
- Bulk edit metadata: genres, descriptions, publishers, release dates, ratings, player counts
- Find and replace across all fields with filtering
- Full undo/redo system 
- Show/hide games with extensive filtering options
- Mark favorites, hidden or edit any data 
- CSV export for external analysis
- Automatic backups while saving

**Organization**
- Scan ROM directories to find new games not in your gamelist
- Identify games in your gamelist that are missing from disk
- Auto-link existing media files to games
- Find and remove orphaned media
- DAT file import and comparison for MAME/arcade collections
- Scan for bad, missing or unecessay media
- Add existing or new media back to your gamelist
- Customizable media paths in settings
- Additional tools for full gamelist and media reporting
- DAT Tools for comprehensive analysis of romsets such as Mame and FBNeo

**Remote management** (Batocera)
- SSH terminal access from within the app
- Map network drives to your Batocera shares
- Check version and available updates
- Stop emulators, restart EmulationStation, reboot or shutdown the system

**Interface**
- Drag and drop media files to add new or existing media, locally or from web browser
- Preview images and videos with built-in player
- 5 color themes
- Customizable fonts, grid spacing, column visibility
- Advanced, easy to use filtering options
- Recent files history and quick system select capability
- Comprehensive settings to suit what you want

## Getting started

Download from [Releases](https://github.com/RobG66/Gamelist-Manager/releases). Extract and run - no installation needed.

1. Open your gamelist.xml (usually in `/userdata/roms/[system]/` on Batocera)
2. Set your media folder paths in Settings
3. Pick a scraper and configure what to download
4. Scrape or edit as needed
5. Save

## Requirements

- Windows 10 or later
- .NET Framework 10

This is not compatible with Wine.

## Support

If you find this useful:

- [PayPal](https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US)
- [Ko-fi](https://buymeacoffee.com/silverballb)

## Credits

Uses data from ArcadeDB, ScreenScraper, and EmuMovies APIs.
