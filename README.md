![gamelistmanager](https://github.com/RobG66/Gamelist-Manager/assets/91415974/42f6a366-00f5-4f1f-bb43-76816006d47b)

Donations: https://paypal.me/RGanshorn?country.x=CA&locale.x=en_US

March 1st, 2025
 Version 7.7 has been released!  If you have an idea or find an issue, please report it.



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

![image](https://github.com/user-attachments/assets/e9803f5d-6627-4ef0-9d0f-d6c568231a3e)

![image](https://github.com/user-attachments/assets/7d1f0804-ceba-43e0-acea-fdb5b85b3d6c)

![image](https://github.com/user-attachments/assets/a779eeb6-5f98-4f69-a495-dfc96c8c4155)

![image](https://github.com/user-attachments/assets/c50e6337-b713-4e89-b09c-d7011f59da86)

![image](https://github.com/user-attachments/assets/d530b6f7-ca05-47dc-9fc9-257ef65b7f51)

![image](https://github.com/user-attachments/assets/217fb614-9a17-4a22-8789-5dd58029ece7)







