![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

July 13th.  Version 6.1.  I think the MameHelper for CHD will be very useful.  Text search and replace implemented as well.  Please, use the "ISSUES" menu item on github to report anything or request a feature.  If it's possible and a good idea, I will try to add it!

July 11th.  Version 6 is finally out!  Feedback is welcome, please report any issue.  (It was a lot of work!)

Gamelist Manager is a comprehensive gamelist management tool for Batocera gamelists.    

It offers functionality to quickly and easily:

- Easy media updating by local or HTTP drag and drop.  
- Scrape data from ScreenScraper, ArcadeDB and EmuMovies
- Configurable persistent settings per system and per scraper
- Quickly load and view gamelist data in a grid table
- Change visibility of items
- Identify new and missing gamelist items
- View and change favorites easily
- Fast sorting and filtering of views 
- Identify and clean up missing, single color or corrupt images and missing videos
- View game media with video playback
- Edit or remove items from the gamelist
- Clear or update scraper dates
- Identify non-playable MAME roms
- Identify MAME clones
- Run specific remote SSH commands on your Batocera host
- Securely save your Batocera credentials
- Map a network drive assistance
- Open images in default editor
- Create M3U files
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

 
![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/bc72b184-4ed0-4727-9fdc-474ec075a355)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/8fd797d5-df41-4927-bfc9-9c89ccf06b9f)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/2186b78a-e3b9-4b8e-98ff-ec5ff0bfca74)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/68f57414-dbbd-4515-ba48-4133cae1120d)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/5792cef6-100b-4c35-97a5-a78d08b44fe3)


    

