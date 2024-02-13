A tool to pick a Twitch chatter and say everything they type out loud

Inspired by DougDoug

# Installation
1. Grab the [latest release](https://github.com/Regynate/PickChatter/releases/latest/download/PickChatter.exe) and extract the contents somewhere. 
2. Launch `PickChatter\PickChatter.exe`
3. Go to the Settings page, click `Connect To Twitch` and follow the prompts

# Usage
1. Click `Pick Random Chatter`
2. Have fun!

Make sure to check the Settings page to customize the picking rules

# OBS Setup
Launch OBS, add a Browser Source. Then select "Local file" and navigate to index.html inside the overlay folder. 
Also you can set it so the audio plays from OBS source instead of from the app in the Settings page.

# Adding avatars
You can put images of chatters into the `overlay\avatars` folder. Then you need to put all the added filenames into the file `avatars.txt`, so it looks like this:

| ![avatars folder](https://github.com/Regynate/PickChatter/assets/64607261/8e8855ec-72b2-4421-9601-f27113355981) |
|:--:| 
| avatars folder |

| ![изображение](https://github.com/Regynate/PickChatter/assets/64607261/fddf62b8-72f2-448d-b2b5-3aa462099718) |
|:--:| 
| avatars.txt |

| ![изображение](https://github.com/Regynate/PickChatter/assets/64607261/1021c6f1-da1e-4638-9e30-ba612cbc71d0) |
|:--:| 
| result |

The app will randomly pick an image for each chatter. You can also change `Default.png`, which is used when there are no chatters.
