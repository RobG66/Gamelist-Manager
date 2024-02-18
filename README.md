![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)


February 18th

 Version 4.1 bugfix is now released!  A major update with multi threaded capable scraping from ScreenScraper now included.  Many other fixes and enhancements.  Grab it from the releases.
 Github continues to cause me issues, my own doing I guess.  
 
February 3rd

 A new 3.1 release and a few minor hiccups getting things updated on master branch.  I may remove the work in progress branch because it makes the whole source control experience more convoluted for me.  I'm not an expert on this :)  The 3.1 release contains fixes, improvements and a few new features.  Screenscraper scraping is still work in progress, but it is coming along.  I'm doing all of this as my way of contributing to the Batocera community and I hope at least a few people find use of it.  If you have an issue or find a bug, let me know and create an issue thingy.
 

Jan 27th

 Lots of refactoring as things evolve.  Moving some classes outside of form source and adding more modularity where it makes sense.  ScreenScraper scraping is somewhat working, but I still need to implement media downloading for it.  I'll worry about 'threading' later when it's more complete.       

 I'm moderately disabled with my hands/arms so doing a lot of keyboard work is extremely difficult for me.  It causes me physical pain too.  But, it helps my emotional state to tinker on this so it is a trade off I guess.  I should slow down a bit though.     


 Release 3.0 introduces preliminary scraping for Arcade (MAME/FBNeo) via ArcadeDB.  If you notice any problem, please open an issue.

 Known limitations:
  - Last scraper date is not set (yet)
  - ArcadeDB scraping is limited in what it provides for images and metadata, but it is quick.
  
![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/bc72b184-4ed0-4727-9fdc-474ec075a355)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/8fd797d5-df41-4927-bfc9-9c89ccf06b9f)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/2186b78a-e3b9-4b8e-98ff-ec5ff0bfca74)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/68f57414-dbbd-4515-ba48-4133cae1120d)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/5792cef6-100b-4c35-97a5-a78d08b44fe3)


Gamelist Manager is a gamelist management tool for Batocera gamelists.  This is version 2.x which is a complete c# rewrite of the original powershell tool.  

It offers functionality to:

- Quickly load and view gamelist data in a grid table
- Change visibility of items
- View and change favorites easily
- Fast sorting and filtering by genre and visibility values
- Identify missing roms
- Identify and clean up missing, single color or corrupt images and missing videos
- View game media with video playback
- Edit or delete items from the gamelist
- Clear or update scraper dates
- Identify non-playable MAME roms
- Run specific remote SSH commands on your Batocera host
- Securely save your Batocera credentials
- Map a network drive assistance
- Open images in default editor
- Create a terminal session to your Batocera host (openSSH)
- Scrap from Screen Scraper or Arcade DB.  Multi thread capable!
- Save your cleaned up gamelist!

The primary method of use requires mapping a network drive to the share  \\batocera\share.  The default credentials are user:root, password:linux.  The program can now securely store your credentials and assist you mapping a network drive if you like.

VLCSharp is used for video playback because it supports the different codecs required for various video file playback.  I am only building for 64bit to reduce source and release size.    

SSH.NET is used for SSH remoting

Windows Credential Manager is used for saving credentials.  I thought about using public/private keys for authentication, but credential manager was still far simpler and straightfoward to use.

Most of the essential features I inteded to have are available now, but there's always room for improvement.  I feel like trying to add scraping will be reinventing the wheel and this functionality is already built into Batocera.  


    - Method of use:
    - map a network drive to your batocera share (\\batocera\share).
    - Load gamelists from the new network drive.
    - I recommend first stopping ES on your batocera host which can now be done from the remote menu.
    
    


 A new 3.1 release and a few minor hiccups getting things updated on master branch.  I may remove the work in progress branch because it makes the whole source control experience more convoluted for me.  I'm not an expert on this :)  The 3.1 release contains fixes, improvements and a few new features.  Screenscraper scraping is still work in progress, but it is coming along.  I'm doing all of this as my way of contributing to the Batocera community and I hope at least a few people find use of it.  If you have an issue or find a bug, let me know and create an issue thingy.
 

Jan 27th

 Lots of refactoring as things evolve.  Moving some classes outside of form source and adding more modularity where it makes sense.  ScreenScraper scraping is somewhat working, but I still need to implement media downloading for it.  I'll worry about 'threading' later when it's more complete.       

 I'm moderately disabled with my hands/arms so doing a lot of keyboard work is extremely difficult for me.  It causes me physical pain too.  But, it helps my emotional state to tinker on this so it is a trade off I guess.  I should slow down a bit though.     


 Release 3.0 introduces preliminary scraping for Arcade (MAME/FBNeo) via ArcadeDB.  If you notice any problem, please open an issue.

 Known limitations:
  - Last scraper date is not set (yet)
  - ArcadeDB scraping is limited in what it provides for images and metadata, but it is quick.
  
![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/bc72b184-4ed0-4727-9fdc-474ec075a355)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/8fd797d5-df41-4927-bfc9-9c89ccf06b9f)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/2186b78a-e3b9-4b8e-98ff-ec5ff0bfca74)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/68f57414-dbbd-4515-ba48-4133cae1120d)

![image](https://github.com/RobG66/Gamelist-Manager/assets/91415974/5792cef6-100b-4c35-97a5-a78d08b44fe3)


Gamelist Manager is a gamelist management tool for Batocera gamelists.  This is version 2.x which is a complete c# rewrite of the original powershell tool.  

It offers functionality to:

- Quickly load and view gamelist data in a grid table
- Change visibility of items
- View and change favorites easily
- Fast sorting and filtering by genre and visibility values
- Identify missing roms
- Identify and clean up missing, single color or corrupt images
- View game media with video playback
- Edit or delete items from the gamelist
- Clear or update scraper dates
- Identify non-playable MAME roms
- Run specific remote SSH commands on your Batocera host
- Securely save your Batocera credentials
- Map a network drive assistance
- Open images in default editor
- Create a terminal session to your Batocera host (openSSH)
- Scrape????  Who knows?!  I'm now tinkering with that.
- Save your cleaned up gamelist!

The primary method of use requires mapping a network drive to the share  \\batocera\share.  The default credentials are user:root, password:linux.  The program can now securely store your credentials and assist you mapping a network drive if you like.

VLCSharp is used for video playback because it supports the different codecs required for various video file playback.  I am only building for 64bit to reduce source and release size.    

SSH.NET is used for SSH remoting

Windows Credential Manager is used for saving credentials.  I thought about using public/private keys for authentication, but credential manager was still far simpler and straightfoward to use.

Most of the essential features I inteded to have are available now, but there's always room for improvement.  I feel like trying to add scraping will be reinventing the wheel and this functionality is already built into Batocera.  


    - Method of use:
    - map a network drive to your batocera share (\\batocera\share).
    - Load gamelists from the new network drive.
    - I recommend first stopping ES on your batocera host which can now be done from the remote menu.
    
    

