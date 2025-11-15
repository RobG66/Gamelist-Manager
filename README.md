![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

 **Do you find this program useful?  Show your appreciation by making a donation:**  
 
https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US

https://buymeacoffee.com/silverballb

---
 November 14th, 2025

  I have finished DAT Tools and added two more thems (Cool and Warm).  The 'warm' one was a bit more challening, it may get tweaked later.  That makes 5 total themes for the next release.  I have refactored gamelist saving so it's much faster than before, especially on large gamelists like Mame.  There's been a few bug fixes, tweaks and many other improvements.  There's still a few items on the to-do list so I'm not sure if I will get a release out this weekend or not... But soon! :)  And then I'm going to finally get around to adding media drag/drop to the media view and improving custom folder paths for media.

  Cool Theme:
  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/81699116-3d53-4f90-97e3-ad5915c2c607" />


----
 November 13th, 2025

 I've added a theme engine to my WIP build!  Changing the theme is as easy as picking what you want from a combobox in the settings menu.  Dat tools is nearly there as well.  There's been a lot of visualization tweaks to controls to give everything a more 'modern' appearance, as opposed to being all blocky and square.

 In addition to the Default theme which is how the program currently looks, there are 2 additional themes. I might add 2 more, but this is where I'm at right now.

 Dark Theme:
 <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/8c3c5720-204f-4d1b-80bf-ef0e4b6fdc02" />

Blue Theme:
<img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/55ea3387-ffad-4605-a1a3-0312a3e6e1e3" />


----
 October 29th, 2025

  The scraper page refactor is almost done.  The core functionality is roughly the same, but the aesthetics are greatly improved.  The middle checkbox area can be collapsed or expanded to increase viewing space for the logging while scraping.  A setting will determine if it auto collapses during scrape:

<img width="3839" height="2014" alt="image" src="https://github.com/user-attachments/assets/52e977c3-7ba4-4323-b33b-99dcba660e19" />

  
  <img width="3839" height="2016" alt="image" src="https://github.com/user-attachments/assets/6b73fb3c-ef07-4a79-a729-cb407a8a8479" />


----
 October 26th, 2025

  Dat tools is nearly done so I took a brief detour into updating the gui presentation.  I'm making the scraper presentation more streamlined with 'modern' looking style.  New combobox and checkbox resource dictionaries for a nicer look as well.

  <img width="3839" height="2006" alt="image" src="https://github.com/user-attachments/assets/4eb995ca-f16e-45e1-8b4c-04a6d06c9df7" />


----
 October 22nd, 2025

 Stop by and say hello in the discussions! :)

  DAT Tools continues to imrpove and evolve:

  <img width="3839" height="2011" alt="image" src="https://github.com/user-attachments/assets/2fa86f1b-1a7d-4d65-b5ce-425ca5d817b3" />



----
 October 11th, 2025

  DAT Tools is coming along ok.     
  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/df985ac0-6743-4b15-87a7-ab7fbcd74cf7" />
 
----
 October 4th, 2025

  I did a bit of work on the DAT Tool page tonight.  Getting rid of all the buttons and having a combobox so you can choose what report you want.  There will also be a summary of course.  As always, it's a work in progress.  

  Pro Tip: When you select "All" for scraping, it scrapes all the items in the current datagrid view.  And if you have the view filtered, the scraping then applies to those items you see in the filtered view. Of course to scrape *ALL* gamelist items, just don't have any view filter applied.  Hidden items are excluded by default, but that's just a simple checkbox setting if you want to change that too.
  

  <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/de587f7a-e41d-4d80-ba43-3d528f1bb366" />



----
October 1st, 2025

 Wow, the year has been flying by fast, already October!  

 I've started working on a DAT Tools 'page' so items for different systems like fbneo, mame and whatever else uses a DAT can be managed.  I'm going to migrate all the Mame menu tools into this, because they will also work with fbneo now as well.  Mame can generate it's own dat file, or the output can be directly read from mame.exe -listxml stream.  FBNeo you need to download the dats from their github page.  I will probably put links in there so it's easy for everyone to find.

 Rough In:
 <img width="3840" height="2016" alt="image" src="https://github.com/user-attachments/assets/ccbeba0c-64c1-4f2f-88fc-eded77209170" />


----
September 27th, 2025

 I'm adding more to the "Tools" menu.  Specifically, methods for using dat files to check for missing items, setting names from database description and other stuff as needed.  
 It's very similar to what the mame tools do, but now will be availavble for derivatives like fbneo for example.  There won't be anything released for a while, unless a bug needs fixing in the last release.  

----
September 25th, 2025

 *8.11 out now!  New Gamelist exception error and reset view fixes*

 I might push out a minor bugfix version this weekend.  I did notice the Reset button isn't resetting all the view filters, minor issue.  I would also welcome if someone could write up a usage document, with some tips and tricks using filters, mame tools etc.

  I might look into fixing custom paths using more than 1 level.  It might not need that much effort, but I won't know until I look more into the code.  I also wanted to add themes like Light, Dark, Blue, Retro, etc.  I just need to remove/replace some color hardcodings into a 'default' theme for the current appearance.  

 Media drag/drop would be nice, but you'll have to comment in the discussions if you care about that or not.

----
 September 24th 2025

 A lot of effort went into 8.1, so let me know how it works for you.

----
 September 23rd, 2025

  Soon!  Anyhow, I have improved scraping performance with calculations based upon what you are scraping, if there are present values and if overwrite options are selected or not.  What this means is that if you are not overwriting and there's existing values for all the metadata items you are scraping, the current item will then be skipped.
  
  <img width="1218" height="736" alt="image" src="https://github.com/user-attachments/assets/67173184-6f05-44eb-ac12-a2190eba0df2" />

 Obviously there is no point scraping an item if there's nothing needed, so this can be a big timesaver.  I wanted to get this implemented before next release and now that it is done, I just need to do some more testing in the next few days.  The only thing I am still not quite happy with is the logging window, but I may leave that for next update.


----
 September 20th, 2025

  One of the things that was asked about by a few people was the ability to define your own regional fallback for ScreenScraper scraping.  Sometimes media may exist for one region, but not another so there needs to be some fallback mechanism.  But, what also happens is that some media is *different* based upon the region.  I think Sega Dreamcast media is a good example of this.  There's no "1 solution fits all" I suppose, so I have added the ability for the user to create their own fallback list based upon the available ScreenScraper regions.

  <img width="1332" height="1107" alt="image" src="https://github.com/user-attachments/assets/4ea6547e-8dc0-43cf-a26b-14f579695000" />

 Something else that was requested long ago was custom folder paths.  This was implemented in settings long ago, but only for sub folders 1 level deep.  I'll revisit that code, but not for the upcoming release.    


----
 September 16th, 2025

  Wheel scraping added, but I haven't tested it yet.  I added two new custom view filters consisting of "IS EMPTY" and "IS NOT EMPTY", of which "IS EMPTY" is quite handy to have when trying to sort/view by missing data.  I'm also adding a "Find" function which is similar to the search and replace, except it will just find what you are looking for.  I found myself wanting/needing that, so I added it.  Nothing to release yet, but soon.  Then another break perhaps.  I have so much on my mind lately, just trying to figure out some financial stuff which frankly just isn't good.       

----
 September 15th, 2025

  Well, never say never!  I will have a new v8 release coming soon which will add 'wheel' metadata scraping.  The reason is that my updated Reload theme will have separate wheel logos for the gamelist wheel and use marquee for the cabinet marquee.  The metadata is there, but EmulationStation and its scraping doesn't really support it - yet!  

 There's a few minor bugs I want to fix as well.  ArcadeDB scraping was missing the correct url for wheel scraping (decal as they call it).  How did that get missed for so long???  Or did it change recently?

  Let me know about any other BUGS NOW, so I can fix them.  Anything else has to wait I think. 
  
  <img width="3839" height="2012" alt="image" src="https://github.com/user-attachments/assets/936901e1-15c8-4fb0-9fc7-8f4b48032177" />

<img width="3839" height="2005" alt="image" src="https://github.com/user-attachments/assets/c5aa9b86-8717-46e0-8530-21c81f41bcf1" />

<img width="3839" height="2159" alt="image" src="https://github.com/user-attachments/assets/02ba5f3b-b996-4f18-9cbe-9aabcedebac7" />


----
 August 31st, 2025

  I've decided that the current release of Gamelist Manager 8.01 version will be the final one on this branch.  The complexities, and complications of what I wanted to do have just kept increasing as I have added more to the project.  And I need a refocus......  So it's time to just tear down and rebuild for version 9 which will also start using .net9.  Of course I'm not rewriting everything from scratch - I will lean heavily on what I already have.  But, I want to clean up a lot of things and do a few things differently too.  Now is a good time for feature requests - so use the discussions!      

----
 August 16th
 
 I updated my RELOAD theme and I think it turned out well.  Now after that and working on the doom launcher, I really need to focus on Gamelist Manager...  I'm a bit concerned that I made too many changes under the hood, so I REALLY need to test things out before pushing out a new release. Sometimes one change leads to another which keeps mushrooming...!  And when the period of time increases between releases, it's easy to lose track or motivation.  I feel like this has dragged on too long, but at least there's no outstanding issues in the current release where I need to rush out a fix.  

 Have a great one!

----
 I got sidetracked on another little project.  My friend and I still play doom and I wished there was at least some kind of front end launcher for batocera, but there isn't.  So I made one consisting of a bash script.  It's quite flexible I think and does what it needs to do.  Check it out:  https://github.com/RobG66/BatoDoom-Launcher
Now there's things I want to fix in my reload theme too....  I suppose I will do that too now.

----
 July 28th, 2025

  I have 'gone down the rabbit hole' with refactoring some scraping code, so I've created quite a bit of work for myself.  I suppose it is for the better, but it's just delaying another release which I didn't anticipate happening.  Anyhow, I'm still working on things, if anyone cares....

----
 July 20th, 2025
 
 Work continues, albeit slower than anticipated.  Sometimes I start on one thing and it just mushrooms into more work than I thought.  Or I find something I want to improve or that needs to be fixed because of changes being made.  I'm not sure I will get anything out before end of month, will see how it goes.

----
 July 1st, 2025

  Happy Canada Day!  June flew by fast for sure and I want to get back to work on getting another update out sometime this month.  I have created some
  additional ini files for region and language mapping purposes, as opposed to hard coding that in the program.  I find it gets a bit convoluted 
  because regional information can be determined by either rom name or system type.  And then there's mame arcade games which have their own method by using
  the mame xml metadata name!

----
June 8th, 2025

 Nothing really new to report, no new release this month (probably).  I've just been busy doing other 'things' lately.   

----
May 28th, 2025

 Hmmm!  Somehow I accidentely lost the readme, so here it is back in its full glory! :)   I am glad to say I have been feeling better lately - one day at a time..!   And I'm still thinking about getting back to working on the media drag/drop updating.  I'd like to get it in there sometime, sooner as opposed to later.  Feedback and support continues to be very sparse, but thank you to those few who have contributed.  It does mean a lot!  

----
May 19th, 2025

 I have been hit hard with anxiety the past 3 days.  Living with that for 35+ years has not been easy, but I've had it under control for quite a long time now.. That is until a few days ago and then it was like a freight train hit me.  I think working on this program helps me though, it's a distraction for my mind and I'm still having fun working on it.  Version 8 will be the next release, probably in a few days.   


----
May 12th, 2025

 I updated the button resources for a subtle, stylized look.  I've also added a setting for saving the mame.exe path so you can set it and forget it.  There's now an export of mame names for improving region and language scraping of arcade games that use mame rom names.  I'm bundling that as a new ini file, but it can also easily be created from the Tools menu.  I want to add additional settings for persistent column selection and filtering the media view if you want.  This is just some of the things I am working on for the next version.  If you have an idea, let me know!         

 ![image](https://github.com/user-attachments/assets/061b8bec-9ddb-456a-b031-6f23c334b83b)

![image](https://github.com/user-attachments/assets/0447ad60-3a2a-47a7-a20c-b76e30929069)


----
May 7th, 2025

 Version 7.91 released which has 2 minor fixes.  I was tempted to dive into refactoring code for custom folder names, but thought it's a rabbit hole I don't want to go down right now.  I think the majority of users will just be using the default paths anyhow.  More features for managing media on a per-rom basis, like drag and drop is still something I want to do, maybe I will focus on that next.  Unless something else distracts me - like it always does :)

----
May 6th, 2025

 Version 7.9 is now released (it was a lot of work!).  There are a few minor things I just noticed that will be quick fixes, so expect 7.91 in a day or two.   
 
----
May 2nd, 2025

I was looking at language and regional code in the ES source today.  It's a bit convoluted and involves parsing filenames to extrapolate language and region information from the tags enclosed in brackets within the filename.  I am going to port over some (most) of this logic into a helper class for 7.9.  The goal is to always try to replicate how ES does things to retain maximum compatibility.  I am also incorporating a new INI file for valid file extensions of the various systems.  Once these 2 things are done, I am releasing 7.9...  Next week maybe! 

----
May 1st 2025

 Second update today!  I managed to get UWP applications to launch, such as Photos or Paint3D.  While MS has sunsetted Paint3D, if you search you can still download and install it from microsoft.  It's a simple, decent app for editing PNG images, better than paint.  Here's a screen capture of the context menu.  You will be able to open using default app or select from a list of registered apps.  You can even view the properties of the file or goto the file location.  Notice the subtle drop shadow on images now. They don't look so 'flat' now.
 ![image](https://github.com/user-attachments/assets/fef15764-c917-4c5b-bc39-b604aaffbb18)


----
May 1st, 2025
 I was working on adding some context menus for the media, specifically "Open With" and letting the user pick what apps are registered for that file extension.  I think I'll initially only support win32 apps since UWP (Microsoft Store) is a whole other PITA thing to work with.  I'm hoping to release something next week I think.

----
 April 28th, 2025
  I've been a bit under the weather with vertigo/dizzy spells which is currently attributed to BPPV.  Nothing too serious, but it really, really sucks going through this!  I've still managed to get a bit of work done towards 7.9, but release is maybe a few weeks away yet.  I also added a "buy me a coffee" donation link.  We'll see how that goes.  
  

----
 April 12th 2025
  Work continues on version 7.9, no ETA yet though.  I recently spent a bit of time updating my Reload theme.  It's not quite where I want it to be, but good enough for now.
  Did you know I have only received 1 donation ever?  And it was a nice feeling to receive that, for all the work I have put into this.  I bought a few cheap GOG games with that.  
  There's no obligation to anyone of course, never will be.  I just thought I'd share the metric on how that has been.      

-----

March 27th, 2025
 Version 7.8 released!  Lots of little tweaks and a few important fixes.  The fixes for arcadesystemname will correct issues with automatic Arcade collections.  Media management is still on the radar, but is not ready.

------

March 1st, 2025
 Version 7.7 has been released!  If you have an idea or find an issue, please report it.

------

Gamelist Manager is a comprehensive gamelist management tool for Batocera and Retrobat gamelists.    

It offers functionality to quickly and easily:
  
- Scrape data from ScreenScraper, ArcadeDB and now EmuMovies (media only)
- Scrape caching!
- Configurable persistent settings per system and per scraper
- Configurable filepaths for each media type 
- Quickly load and view gamelist data in a grid table
- Single or bulk change visibility of items
- Identify new and missing gamelist items
- View and change favorites easily
- Fast sorting and filtering of views by different metadata elements 
- Identify and clean up missing, single color or corrupt images and missing videos
- View game media with video playback
- Edit or remove items from the gamelist
- Clear or update scraper dates
- Identify non-playable MAME roms
- Identify MAME clones
- Identify MAME games requiring CHD and check if the file is present
- Identify MAME bootleg games
- Run remote SSH commands on your Batocera host
- Securely save all your credentials using Windows Credential Manager
- Quick drive mapping assistance
- Open images in a default editor (coming)
- Create M3U files from selected items (coming)
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

![image](https://github.com/user-attachments/assets/e9803f5d-6627-4ef0-9d0f-d6c568231a3e)

![image](https://github.com/user-attachments/assets/7d1f0804-ceba-43e0-acea-fdb5b85b3d6c)

![image](https://github.com/user-attachments/assets/a779eeb6-5f98-4f69-a495-dfc96c8c4155)

![image](https://github.com/user-attachments/assets/c50e6337-b713-4e89-b09c-d7011f59da86)

![image](https://github.com/user-attachments/assets/d530b6f7-ca05-47dc-9fc9-257ef65b7f51)

![image](https://github.com/user-attachments/assets/217fb614-9a17-4a22-8789-5dd58029ece7)






