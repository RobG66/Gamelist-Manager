<h1>Gamelist Manager 2</h1>

![Untitled](https://github.com/RobG66/Gamelist-Manager/assets/91415974/ad16f981-788e-47e9-90c5-32e0dcf60b5a)
![Untitled2](https://github.com/RobG66/Gamelist-Manager/assets/91415974/32050796-01e7-4155-893c-d84a5c864872)
![Untitled3](https://github.com/RobG66/Gamelist-Manager/assets/91415974/32eb947d-1673-48f4-a266-f59112c4ec58)

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
- Save your cleaned up gamelist!

The easiest method of use is to map a network drive to the share  \\batocera\share.  The default credentials are user:root, password:linux

VLCSharp is used for video playback because it supports the different codecs required for various video file playback.  I am only building for 64bit to reduce source and release size.    

Most of the essential features I inteded to have are available now, but there are a few more things I want to add.  I want to create a handle for the missing/corrupt/single color images so you can either delete them or just get a list of the offending files.  

Scraping support is a possibility if there's an API or perhaps I can make my own web scraper.  

    - Method of use:
    - map a network drive to your batocera share (\\batocera\share).
    - Load gamelists from the new network drive.
    - I recommend first stopping ES on your batocera.  Connect by SSH (putty) and run: 
    - /etc/init.d/S31emulationstation stop

