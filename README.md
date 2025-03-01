![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

Donations: https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US

March 1st, 2025
 Version 7.7 has been released!  If you have an idea or find an issue, please report it. 



Gamelist Manager is a comprehensive gamelist management tool for Batocera gamelists.    

It offers functionality to quickly and easily:
  
- Scrape data from ScreenScraper, ArcadeDB and now EmuMovies (media only)
- Configurable persistent settings per system and per scraper
- Configurable filepaths for each media type 
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





