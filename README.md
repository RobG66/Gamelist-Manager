![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/fa68ee19-06c4-4d7c-b468-8d8b63b4ac5f" />

### 💖 Support Development

If you find Gamelist Manager helpful, consider supporting its continued development:

- [PayPal](https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US)
- [Buy Me a Coffee](https://buymeacoffee.com/silverballb)

This program is not Wine compatible

---
January 7th, 2026

 Media controls are wired in, but there's a lot of work to do there..  I did a rough port-in of the scraper gui from WPF.  Obviously it needs work, but that's how it kind of starts.  I'm using Gitub Copilot to assist with the 'porting' and helping me with the mvvm stuff too.  There are so many bindings and hooks that it's easy to become overwhelmed by it all.  Copilot makes a lot of mistakes too so that increases the amount I need to use it to find and fix what it screwed up in the first place.  It's a vicious circle and there's no refund for their misakes!  I have already hit a major roadblock with VLC which is frustrating, but I've pushed that aside for now.  And I still don't like the Avalonia licensing where they keep their media player paywalled because they know VLC has issues.  I don't want their other accelerate stuff, I just want a working media player that isn't such a PITA (cough VLC cough)!  Copilot costs money, Avalonia Accelerate costs money, Uno costs money...  Suddenly free software isn't free any more and I hate that.  I have no problem spending time doing this, but I do have limits on out of pocket expenses.  Sorry for the rant...  Anyhow, see the rough in, I hope I can complete the scraper without running out of copilot assistance $$$$.                         

 <img width="3840" height="2040" alt="image" src="https://github.com/user-attachments/assets/02898248-b577-40bb-8678-2c60a2f34326" />



---
 January 4th, 2026

  Gamelist Manager 8.6 will be the last version to be built upon windows WPF.  I have decided to focus on future development being cross platform compatible with Avalonia.  I have already begun porting over code which isn't too bad, but the gui will see some design changes for sure.        

 Early work, not themed yet... Functional, but still a lot to do.  I'm just (re) building the settings page now.

<img width="2676" height="1768" alt="image" src="https://github.com/user-attachments/assets/f6c4f55d-3507-4a1d-b173-91fe4e058d7f" />

<img width="2676" height="1768" alt="image" src="https://github.com/user-attachments/assets/5900cc7b-277c-421f-b8ca-a2f55c8bea0d" />

<img width="2676" height="1768" alt="image" src="https://github.com/user-attachments/assets/b1fcdab6-8ffe-474e-b63d-0e8786be324a" />



---
December 31st, 2025

 Known bugs fixed, and a few more features will round out 2025 with the release of Gamelist Manager 8.6.  Get it while it's hot! :)

 And Happy New Year everyone!
---

December 27th, 2025

 There is a slight chance of a hangup happening in the media player control which is related to ui threading.  So I have made some tweaks (again!) to this control and will release an update in the next few days.  It's very rare that the freeze happens and can only happen if you have the media page open and are moving between games.  I've also added an easier way to switch systems for which you already have gamelists for.  You can still use the menu or just quick pick a system:

<img width="1380" height="984" alt="image" src="https://github.com/user-attachments/assets/1de3f71b-75f9-4366-860b-81ed416ba556" />

---

December 24th, 2025

 I think I will be posting 8.5 sometime today, barring any last minute issue.  This is my present to everyone out there!  It's been a lot of work and I've had fun making this program.  ES-DE gamelist support is something I will look at in the new year since it seems worthwhile adding.  I've added 3 more themes in Gamelist Manager for the upcoming release.  Keep an eye out :)


---
 December 22nd, 2025

 Scraping single media items is soon going to get a whole lot easier...!  Not only that, but media drag/drop is now added as well.  Oh and more media options like directly deleting/removing what you don't like from the media page.  This will be in the next version 8.5......  And if you're reading this, let me know and post something in the comments section!  Your participation is greatly appreciated!  

 <img width="3839" height="2020" alt="image" src="https://github.com/user-attachments/assets/25ddcb0e-bb0a-4576-b983-28f071c30447" />

---
 December 21st, 2025

  Wow, sometimes things just fall into place so easily, especially when code refactoring makes everything more 'modular'.  The next version is going to be really, really good because it will finally add media drag/drop.  Also, you will be able to re-scrape media items right in the media page.  Another nice edition is in the settings you can permantently disable media types you don't want in your way, like map - which I have yet to find any use for.  It's only in there because ES has it.  I don't even know if any scrapers provide that media?  Other questionable media items are mix, magazine and bezel.  Again, only in there because it's in ES.  I know you can scrape bezel and mix, but magazine?  Chances are these media items are leftovers that have just been left in place all this time and not cleaned up.     

<img width="1921" height="1604" alt="image" src="https://github.com/user-attachments/assets/279e2328-c6a8-463b-a0e6-4262aa69ec02" />


---
 December 20th

 8.43 now released with some important fixes!  I'm hoping nothing else turns up so I can sit back on this version for a bit.    

 ---
 December 19th Part 2!

 I guess I will be posting 8.43 this weekend.  I have had to update ArcadeDB scraping due to some host API changes which I just noticed.  I'm also making methods like clear all data and clear all media respect the filtered view.  So now 'all' means everything you see grid, not everything including what you don't see because of a filter.

---
December 19th 2025

 Another small update posted tonight.  Credentials were being stored in windows credential manager and I have changed that to use an encrypted json string that is saved with all the other settings.  It's more consistent and simpler design.  I did a bit more cleanup too, it's ongoing WIP.   

---
December 15th, 2025

 Update:  Emumovies is now back in business!  You can try downloading their videos with "High Definition" as the source.  The quality of these videos is excellent!  Although I think you need an Emumovies subscription to download videos.

 8.4 is here and it's a bit of a doozy!  I did A LOT of code refactoring on the scrapers which was quite involved.  And the other day someone pointed out to me that ScreenScraper has 'support-2d' media which is essentialy cartridge media.  I didn't know about that so I added it now!  I could not test EmuMovies scraping since the weekend because their site is having issues after a server move - I hope that gets sorted soon.  I've added some buttons on the media page which will get expanded upon in future releases.  Like more media options and drag/drop (which keeps getting pushed back).  And of important note, since I did a lot of work on this version, please let me know about anything that doesn't seem right.  Is the .Net10 requirement any trouble??

---

December 5th, 2025

So I have an eperimental build of my Gamelist Manager and it can now do batch processing from ArcadeDB.  For stricly metadata (not files) it can download and update all metadata in less than 10 seconds for 3500 games..  For 15000 items, it's about a minute.  It's too fast and the gui logger cannot keep up so I have to turn that off.  It's not the first trouble I've had with the visual logger not being able to keep up.  Your mileage may vary depending on how many cpu cores you have.  I tested with 5 tasks in parallel. 


 Phew, who knew that editing icons could be so time consuming!  I finally managed to put decent icons on the ribbon application drop down menu.

 <img width="1546" height="1724" alt="image" src="https://github.com/user-attachments/assets/2e16b76b-c19f-4789-a50d-6ff334842abf" />





---
December 4th, 2025

 The next version forthcoming will contain a considerable refactor in scraping code.  Functionality is not changing, but there's been a lot of code cleanup which prioritizes separation of concern.  This means the scraper classes have one concern and that is to do the url scrape.  That (now) does not include downloading or updating data.  It's a cleaner approach and something I have been wanting to do for a while now.  As part of that cleanup, I've added logging (to file) of the log output and there is also a new download summary when finished.  BTW, deletions are also logged as well (logs folder).  Dat tools is getting another function to identify what you are 'missing' in your gamelist (ie: playable roms).  I have to figure out some more filtering magic of the mame xml first.

 <img width="2951" height="660" alt="image" src="https://github.com/user-attachments/assets/e01015a1-74a7-423f-b05f-c194d6083d6c" />


---
 November 28th, 2025

  8.3 release on schedule.  New optional ribbon menu and the ability to delete items and optionally delete the associated media too.
  The dark theme was removed (for now) because it proved just too difficult to theme the ribbon that way.  I may add it back just for the classic menu...
  The ribbon was spur of the moment design and it was a lot of work in such a short period of time.  I hope you like it and find it useful.  What is coming
  for 8.4?  I dunno, I keep wanting to add media drag/drop, but it keeps getting pushed back.  Please report any bugs, thank you.  :) 

---
 November 26th, 2025

  8.24 released with a few more minor bug fixes.  8.3 will probably come fairly soon (weekend?) with the new ribbon menu.  I had to remove the dark theme because the WPF ribbon just doesn't look good with dark colors.  The ribbon control itself just doesn't lend itself to being themed easily and I had to create another resource dictionary just to style disabled tabs/menus in the ribbon.  It's a very clunky control!  I was tempted to try fluent ribbon, but I think I'll be ok without it.  You will be able to seamlessly switch between old and new menus and also auto hide the ribbon menu if you want.  I've also replaced many of the icons on the ribbon with new ones!

---

 Nov 23rd, 2025

  A new readme file which I had claude.ai create for me and it's done a better job than I could have ever.  8.23 is out now and fixes most known issues.  Unless something major is found, I'll wait to fix anything else until it's time for another regular update.  And speaking of updates, something I am experimenting with for the next update will be an optional ribbon menu.  You can still use the old menu, but the ribbon will be more practical and easier to use than constantly opening menus.  It takes up a bit more headroom so maybe I will have an option for it to auto collapse or not if you want.  There's always choice! :)  

  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/de9c4e13-640b-473f-9f2f-01b950b014df" />
  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/214d5d32-475a-41b6-83e9-683cd5e22611" />
  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/35493581-136d-47d2-8f47-3ae13b38c5a7" />
<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/39fae76a-491d-4477-8a14-b0f29d6085f4" />
<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/a362d0aa-9072-4fc6-8a60-3eafb497bd99" />





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
