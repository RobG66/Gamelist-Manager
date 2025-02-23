![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

Donations: https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US

February 23rd, 2025.  Emumovies scraping is now fixed.  It was a combination of a few things...  A typo, a missing bearer token in post and urlEncoding being needed for some filenames.  I'm fixing a few other minor bugs and implementing a few requests.  I think I might add separate regional settings for media and metadata (ScreenScraper).  Sometime next week I hope to have a new version posted.  Let me know what you think, find me on discord on the Retrobat and Batocera servers under the name RobG66.



February 21st, 2025.  I am dusting off the 7.5 source code for an update to fix a few things.  Emumovies scraping has an issue which I do want to get working.  A few other reported issues I will address as well.  I realized I just don't have the motivation right now to continue on another re-write with MVVM as I alluded to.  The application has so many GUI triggers, filters, etc that it's just too difficult to adhere to an architecture standard where this should be done through bindings and other means instead of direct gui manipulation.  I spend way too much time trying to get even simple things working.  That isn't fun, it's frustrating.  


January 28th, 2025.  Work has progressed slowly on the next major update, which now focuses on following MVVM architecture design.  It has been a considerable learning curve and I am still learning as I work my way through it.  Of course I am still using WPF and C#.  Here's a little teaser image of the gui progress.  The top menu is now gone and I am also trying to achieve a more 'modern' look to things, without resorting to third party libraries.  Custom templates work really well for this!  

![image](https://github.com/user-attachments/assets/8c35c9cd-4dc7-43f3-aec1-e290309fd3d4)



January 13th, 2025.  Just remember that using the cache can be extremely beneficial.  From that cache, you can rescrape any meta data you want within just a matter of seconds.  If you are just scraping media, I suggest trying EmuMovies first as it will be a lot faster.  Also, make sure you enable image verification in the settings to avoid downloading single color and corrupt images - there can be a lot!   If you like Mame, try out the Mame tools for identifying unplayable game and games requiring a chd.  The tool will even check if you have the CHD files or if they are missing!   


January 11th, 2025.  I want better structure in my code so I am now trying to learn how the "Model-View-ViewModel" works.  Some aspects of the UI I really want to improve on as well, but the design needs a lot of thought.  I think good GUI design is maybe the most difficult thing.  I strive for a design that doesn't overwhelm the user and is intuitive to use without reading documentation.  Yet it also has to offer a good deal of granular control if the user wants that too.  Version 7.5 is in a good state so my plan is to look at doing another major update.  I plan to stick with WPF because it works well (except I really hate the menus).  Well, I'll see how it goes. :)   


Version 7.5 is released!  This major improvement with this version is where it incorporates using HTTPClientfactory instead of an HTTPClient singleton.  It's a better approach to multi-threaded scraping.  A few bug fixes and tweaks here and there as well.  Media drag/drop is still a WIP, maybe available next release version.  



Sneak peak of WIP:
![image](https://github.com/user-attachments/assets/3fce76b1-6114-438f-b8bd-b0127de59a87)




December 21st, 2024.  I added a donation link at the top of the read me.  It's been about a year since I started this project and there are still things I want to do with it.  I've never asked for anything before - it was never my expectation or intent.  The program will never be paywalled.  But sometimes a person needs help....        


December 20th, 2024 - I am working to implement media drag/drop similar to the older Winforms version.  It's not difficult, just time consuming.  


December 12th, 2024 - Version 7.4 non beta is released.  I realized that Title Shot was missing for screenscraper scrapes so I added it.  A few fixes here and there.  I'm not going to do anything with scrape dates any more since I don't find them particularily useful and it's too much work for so little value.  I do want to add media drag/drop back at some point....   


December 6th, 2024
 Version 7.3b is release.... and it was a lot of work.  The media management tool alone was a frustration over many months.  I don't have everything there I would like, but it can still do a lot to make lives easier for managing this stuff.  I also had to slow down since I've been getting painful tendonitis in my hands from too much keyboard work (getting old!).  


December 3rd, 2024
 I've been spending a lot of time working on media management and adding a tool to do various media related tasks (add/cleanup/etc).  I didn't think it would take as long as it did, but it is getting close.  Some more bug fixes coming too, but nothing urgent or I would have released something much sooner.   


November 4th, 2024
 Version 7.1 beta now released with quite a few fixes and a few small improvements.  
 



October 31st, 2024
 I believe Emumovies scraping should be working again after they fixed issues on their end.  I am putting together another release soon which contains a few improvements and also has some bug fixes.  Maybe this weekend I will post something.



October 25th, 2024
 I took a break.. needed it!  I noticed the API URL for emumovies has changed so emumovies scraping is currently not working.  I have asked the folks over there what the new URL is.  I haven't looked at adding other scrapers just yet, I will soon.  I also want to finish fixing the media manager so I can enable it again.  This allows re-adding local media as well as scraping downloaded media from a local folder instead of the internet.


September 30th, 2024
 V7 beta has been re-posted with some necessary bug fixes.  Is there anyone willing to write up some documentation for me please?
 

September 25, 2024

 V7 beta has been removed temporarily until a few issues are resolved.  I'm sorry about that, I will have an update hopefully end of week.


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





