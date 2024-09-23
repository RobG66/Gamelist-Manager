![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

September 22,2024

 V7 beta has been released.  See release notes and please report any issues you find




September 13th, 2024
Soon!!!

August 21st, 2024
 Work continues on  the new version.  I have all 3 scrapers working and plan to add a fourth soon (TheGamesDB).  I've added music downloads for the EmuMovies scraper and a jukebox for playing those (and videos).  As I pull pieces of the older codebase into the new program, I found a few bugs and fixed those.  I've done other little things like switch to an HTTP singleton because it's just more efficient.  Release is when it's ready, but probably some time in September.  I may release a version with a few things missing, but main functionality is there.  Oh, one more thing and I think it's quite a big thing: **Scraper Cache**!!  It makes sense to keep a cache of scraper data so you can always go back and scrape metdata locally instead of the web every single time. 
    


 ![Screenshot 2024-08-21 210505](https://github.com/user-attachments/assets/95a41dd4-5794-4429-93ce-d49a15ff2e77)


August 8th, 2024.
 I have transitioned into using WPF instead of WinForms for the GUI.  A lot of the codebase is re-usable, but some needs refactoring which takes time.  I think it's worth it.  I just finished implementing undo/redo functionality for changes being made, including bulk changes.  

  As for the GUI, it closely resembles what I had before, but with some aesthetic updates.  Custom buttons was something I really wanted to do and WPF lets me 'easily' do that.  A few things require considerable work to get just right, but it is a learning process.  It's not ready for release, but here's a sneak peak:  


 ![image](https://github.com/user-attachments/assets/cffcf79c-fb5e-4584-a6be-f19f2303268b)

![image](https://github.com/user-attachments/assets/5a2ea355-c774-4bf5-b47d-677c087e2a15)

 



July 18th.  Work continues on 6.2.  Cartridge (disc, various cartridge views) scraping has been added and there will also (hopefully) be a new menu tool for automatically adding downloaded media packs to the appropriate games.  Of course there are limitations where the media names have to be similar enough to be matched to rom names using a fuzzy search.  I've also added the ability to select the remote video source from scrapers, but I'm not sure how much, if any difference there is.  No update from TheGamesDB on getting an API key yet.  

July 14th.  The next version will see the addition of configurable media paths for each type of media item.  TheGameDB scraping is being looked into and I am awaiting API access.

July 13th.  Version 6.1.  I think the MameHelper for CHD will be very useful.  Text search and replace implemented as well.  Please, use the "ISSUES" menu item on github to report anything or request a feature.  If it's possible and a good idea, I will try to add it!

July 11th.  Version 6 is finally out!  Feedback is welcome, please report any issue.  (It was a lot of work!)

Gamelist Manager is a comprehensive gamelist management tool for Batocera gamelists.    

It offers functionality to quickly and easily:
  
- Scrape data from ScreenScraper, ArcadeDB and now EmuMovies (media only)
- Easy media updating by drag and drop locally or from HTTP browser.
- Configurable persistent settings per system and per scraper
- Configurable filepaths for each media type (version 6.2)
- Quickly load and view gamelist data in a grid table
- Single or bulk change visibility of items
- Identify new and missing gamelist items
- View and change favorites easily
- Fast sorting and filtering of views 
- Identify and clean up missing, single color or corrupt images and missing videos
- View game media with video playback
- Edit or remove items from the gamelist
- Clear or update scraper dates
- Identify non-playable MAME roms
- Identify MAME clones
- Identify MAME games requiring CHD and check if the file is present
- Run remote SSH commands on your Batocera host
- Securely save all your credentials using Windows Credential Manager
- Quick drive mapping assistance
- Open images in a default editor
- Create M3U files from selected items
- Export gamelist data to csv
- Create a terminal session to your Batocera host (openSSH)
- Save your cleaned up gamelist!

The primary method of use requires mapping a network drive to the share  \\batocera\share.  The default credentials are user:root, password:linux.  The program can now securely store your credentials and assist you mapping a network drive if you like.


    - Method of use:
    - map a network drive to your batocera share (\\batocera\share).
    - Load gamelists from the new network drive.
    - I strongly recommend stopping Emulation Station on your batocera host which can now be done from the remote menu.
    

VLCSharp is used for video playback because it supports the different codecs required for various video file playback.  I am only building for 64bit to reduce source and release size.    

SSH.NET is used for SSH remoting.

Windows Credential Manager is used for saving credentials.  I thought about using public/private keys for authentication, but credential manager was still far simpler and straightfoward to use.

Most of the essential features I inteded to have are available now, but there's always room for improvement.

 ![image](https://github.com/user-attachments/assets/c246b08e-e95f-47d7-949a-a2ea99216d98)

![image](https://github.com/user-attachments/assets/cdacb479-876f-410a-b5ac-c43e8d66b902)

![image](https://github.com/user-attachments/assets/a41c4432-b84b-483f-a0f9-7e3458a3e64e)

![image](https://github.com/user-attachments/assets/f5496cc1-2ec8-402a-8d4b-3304791968f4)

![image](https://github.com/user-attachments/assets/fbf4afad-5de0-4745-be37-49b12e4392a8)

![image](https://github.com/user-attachments/assets/be99978a-a1b0-4b0b-b4e6-e88ebf98d7da)

![image](https://github.com/user-attachments/assets/bbd0cf82-ffa4-4384-a1e9-b93ba2d5cf87)





