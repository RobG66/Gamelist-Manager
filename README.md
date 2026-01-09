![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/fa68ee19-06c4-4d7c-b468-8d8b63b4ac5f" />

### 💖 Support Development

If you find Gamelist Manager helpful, consider supporting its continued development:

- [PayPal](https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US)
- [Buy Me a Coffee](https://buymeacoffee.com/silverballb)

This program is not Wine compatible

---
# Gamelist Manager

A comprehensive desktop application designed for retro gaming enthusiasts to manage, organize, and enhance their game collections. Gamelist Manager provides powerful tools for editing metadata, scraping information from online databases, and maintaining gamelist XML files for popular emulation frontends like EmulationStation and Batocera/Retrobat.

## ✨ Key Features

### 📝 Advanced Metadata Management
- **Bulk Editing**: Update multiple games simultaneously with search and replace functionality
- **Comprehensive Fields**: Manage names, descriptions, genres, publishers, developers, release dates, ratings, player counts, arcade system names, families, game IDs, languages, and more
- **Smart Filtering**: Filter by visibility (visible/hidden), genre, or custom criteria with 7 comparison modes (Is Like, Is Not Like, Starts With, Ends With, Is, Is Empty, Is Not Empty)
- **Find & Replace**: Powerful search tools with column-specific searching and batch replacement for all items or selected items
- **Undo/Redo System**: Full history tracking with configurable undo depth (up to 99 levels) so you never lose your work
- **Clear Data Tools**: Selectively clear metadata or media paths for selected items or all items at once

### 🎮 Multi-Source Game Scrapers
- **Three Scraper Options**: Choose from ArcadeDB, EmuMovies, or ScreenScraper
- **Flexible Scraping Modes**: Scrape all items or only selected items
- **Smart Caching System**: Store scraped data locally to speed up future operations and reduce API calls
- **Configurable Media Sources**: Select different image sources (screenshots, flyers, cabinets, etc.) for images, marquees, thumbnails, cartridges, videos, box art, and wheels
- **15+ Media Types**: Download images, thumbnails, videos, marquees, bezels, manuals, titleshots, wheels, cartridges, box art, box backs, fan art, maps, music, and magazines
- **Scraping Options**: 
  - Scrape from cache first (with option to skip non-cached items)
  - Include or exclude hidden items
  - Multi-threaded scraping with configurable thread count
  - Real-time progress tracking with percentage completion
  - Detailed scraping log with color-coded status messages
- **Overwrite Protection**: Separately control whether to overwrite names, metadata, or media
- **Cache Management**: View cache item count and clear cache when needed

### 🖼️ Media Management
- **Find & Add Media**: Automatically locate and link existing media files to your games
- **Media Cleanup**: Identify and remove orphaned media files
- **15+ Media Types**: Support for images, thumbnails, videos, marquees, bezels, manuals, titleshots, wheels, cartridges, box art, box backs, fan art, maps, music, and magazines
- **Configurable Paths**: Customize folder paths for each media type
- **Path Management**: View and edit media paths directly in the grid
- **Media Preview**: Built-in media viewer with autoplay option for videos
- **Media Statistics**: View counts of existing media by type

### 🔍 Discovery & Organization
- **Find New Items**: Scan ROM directories to add newly discovered games with configurable search depth (0-9 levels)
- **Identify Missing**: Detect games listed in your gamelist but missing from disk
- **DAT Tools**: Comprehensive DAT file analysis and comparison
  - Import directly from MAME executable
  - Load external DAT files
  - View detailed DAT statistics (total, parents, clones, CHD required, playable/non-playable)
  - Gamelist comparison showing parents, clones, CHD games, non-working games, and items not in DAT
  - Generate reports for specific categories
  - Optional inclusion of hidden items in analysis
- **Visibility Management**: Show or hide games individually, in bulk, by selection, or by genre
- **Favorites Tracking**: Mark and filter your favorite games with dedicated counter
- **Status Indicators**: Visual color-coding for new items (green) and missing items (red)
- **Genre Filtering**: Quick filter by genre with clear button
- **Statistics Display**: Real-time counts for total games, hidden games, showing games, and favorites

### 🌐 Remote Batocera Management
- **Network Drive Mapping**: Connect to Batocera shares directly from the application
- **SSH Terminal Access**: Open terminal sessions to your Batocera host
- **System Information**: Check Batocera version and view available updates
- **Remote Control**: 
  - Stop running emulators
  - Stop EmulationStation
  - Reboot Batocera host
  - Shutdown Batocera host
- **SSH Key Management**: Easy SSH key configuration and removal
- **Configurable Connection**: Store hostname, user ID, and password in settings

### 🎨 Customizable Interface
- **Theme System**: Choose from 5 color themes (Default/Silver, Blue/Azure, Dark/Black, Cool/Alice Blue, Warm/White Smoke)
- **Adjustable Font Sizes**: Scale grid text from 8pt to 14pt for optimal readability
- **Alternating Row Colors**: Customize alternating row colors for better readability
- **Grid Line Options**: Control visibility of horizontal, vertical, all, or no grid lines
- **Column Visibility**: Show or hide any combination of 17+ columns
- **Column Autosize**: Automatically fit columns to content with remember setting
- **Always on Top**: Keep Gamelist Manager visible while working
- **Dual-Pane Layout**: View game list and descriptions side-by-side with adjustable splitter
- **Collapsible Panels**: Minimize metadata/media selection panel when not needed
- **Remember Settings**: Optionally remember column visibility and autosize preferences

### 📊 Data Management Tools
- **CSV Export**: Export your collection data for use in spreadsheets or external analysis
- **Backup & Restore**: Create and restore gamelist backups to prevent data loss
- **Recent Files**: Quick access to your most recently opened gamelists (configurable 1-50 files)
- **Auto-Save Reminders**: Optional prompts before losing unsaved changes
- **Bulk Change Confirmation**: Optional confirmation dialogs for major bulk operations
- **File Status Bar**: Shows loaded filename and last modification time
- **Reset Views**: Quickly reset filters and view settings to defaults

### 🎵 Entertainment Features
- **Video Jukebox**: Browse and play game videos from your collection
- **Music Jukebox**: Play background music while managing your library with configurable default volume
- **Media Autoplay**: Optional automatic playback of videos in the media viewer

### ⚙️ Advanced Settings
- **Change Tracking**: Enable/disable undo/redo with configurable history depth
- **Image Verification**: Validate downloaded images to detect corruption and single-color images
- **Search Depth Control**: Set folder recursion depth (0-9) for finding new items
- **Auto-Expand Logger**: Automatically expand the scraper log during operations
- **MAME Integration**: Configure path to MAME executable for DAT imports
- **Custom Media Paths**: Define separate folder paths for all 15+ media types
- **Quick Reset Options**: Reset all settings or just folder paths to defaults

## 🚀 Perfect For

- EmulationStation and Batocera users managing large game collections
- Retro gaming enthusiasts who want complete, well-organized metadata
- Arcade cabinet builders who need to scrape and organize MAME/arcade information
- Multi-platform collectors who need flexible, powerful management tools
- Users who want remote control and management of their Batocera systems
- Anyone who wants to clean up and enhance their game library without manual XML editing

## 💻 System Requirements

- Windows 7 or later
- .NET Framework 4.8 or higher  
- Internet connection (for scraping features)
- Network connection (for Batocera remote features)

---
