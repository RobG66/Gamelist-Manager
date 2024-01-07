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
- Run specific remote SSH commands on your Batocera host
- Securely save your Batocera credentials
- Map a network drive assistance
- Open images in default editor
- Create a terminal session to your Batocera host (openSSH)
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
    
    

