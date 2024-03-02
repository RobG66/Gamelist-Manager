![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

March 2nd 2024

 Development of Gamelist Manager is maybe winding down.  I've done everything (and MORE!) than I set out to initially do and I'm not sure what else I can add at this point.  Bugs will be fixed and if someone suggests a good improvement, I will consider it.    

February 27th

 For 4.5 (hopefully) I am working on an image drag & drop control.  This will let you (somewhat) easily drag and drop new images for a selected game from a windows explorer window or maybe even a webpage.  There's quite a few possibilities for how it could work, but I am just not sure on the best method yet.  More cleanup and optimization of code going on under the hood.


February 24th

 4.4 is here.  I've added additional ScreenScraper elements to scrape so it should now 100% mirror what Batocera is scraping.  I don't know if anyone is using these elements in their themes, but if they are it will help ensure compatability.  Boxback, fanart, genreid, arcadesystemname were added.  There's always code optimization which can be done, but I'm going to leave some of that for later.  I've already done A LOT in 4.4 and I want to get it posted.  I just want to test it a bit more.

February 22nd

 Version 4.4 is getting close.  There was a significant change to background data management with the inclusion of a dataset table to xml deserialization class.  It was a bear to implement, but in the long run it helps simplify data transition from xml to dataset and back to xml for saving.  Why not just work with XML only?  Well, the dataset table is extremely flexible and fast for sorting, filtering and searching.  

 4.4 will also include options to export your gamelist to CSV and also create M3U files for platforms that support it.  Just select the files, click create M3u and it's done!

  ScreenScraper scraping sees more tweaking with the ability to scrape by ID.  It's not faster, just obviously more precise.  The deserialization class makes it a whole lot easier to manage and save these values.  It can be enabled or disabled in the screen scraper options.

   Suggestions are always welcome and if you would like to see something added, I will try my best as long as it makes sense.  

February 20th

 4.3 is released.  Improved compatibility when additional scraper elements are present in the gamelist.xml which causes the dataset to have more tables.  Also added an escape for single quotes in path select to avoid an exception error.  Something to note is that you can have the scraper form open and still edit the names.  This will allow you to try a different name when scraping if the API is not recognizing what you are trying to scrape.  I will look into adding the ScreenScraper search API, but that might take a bit of work to implement.


February 19th

 Another bugfix release (4.2), sorry about that!  I do a lot of testing myself, but sometimes something slips by, usually if I make a last minute change.
 Oh, this version also has backups for bad media AND it checks for unused media too.

February 18th

 Version 4.1 bugfix is now released!  A major update with multi threaded capable scraping from ScreenScraper now included.  Many other fixes and enhancements.  Grab it from the releases.
 Github continues to cause me issues, my own doing I guess.  
 
February 3rd

 A new 3.1 release and a few minor hiccups getting things updated on master branch.  I may remove the work in progress branch because it makes the whole source control experience more convoluted for me.  I'm not an expert on this :)  The 3.1 release contains fixes, improvements and a few new features.  Screenscraper scraping is still work in progress, but it is coming along.  I'm doing all of this as my way of contributing to the Batocera community and I hope at least a few people find use of it.  If you have an issue or find a bug, let me know and create an issue thingy.
 

Jan 27th

 Lots of refactoring as things evolve.  Moving some classes outside of form source and adding more modularity where it makes sense.  ScreenScraper scraping is somewhat working, but I still need to implement media downloading for it.  I'll worry about 'threading' later when it's more complete.       

 I'm moderately disabled with my hands/arms so doing a lot of keyboard work is extremely difficult for me.  It causes me physical pain too.  But, it helps my emotional state to tinker on this so it is a trade off I guess.  I should slow down a bit though.     

  
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
- Create M3U files
- Export data to csv
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
    
    

