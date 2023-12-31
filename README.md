Gamelist Manager is a gamelist management tool for Batocera gamelists.  It offers functionality to:

- Quickly load and view gamelist data in a grid table
- Fast sorting and filtering by genre, hidden and visible properties
- Identify missing roms
- Identify missing, single color or corrupt images
- View game meda with video playback
- Edit or delete items from the gamelist
- Save your cleaned up gamelist!

The easiest method of use is to map a network drive to the share  \\batocera\share.  The default credentials are user:root, password:linux

This is version 2.0 which is a c# rewrite of the original powershell tool.  VLCSharp is used for video playback.

Most of the features I inteded to have are available now, but there are a few more things I want to add.  

    - Method of use:
    - map a network drive to your batocera share (\\batocera\share).
    - Load gamelists from the new network drive.
    - I recommend first stopping ES on your batocera.  Connect by SSH (putty) and run: 
    - /etc/init.d/S31emulationstation stop

