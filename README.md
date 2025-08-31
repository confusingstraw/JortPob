**Setup instructions**

1. Pull the project: `git clone https://github.com/infernoplus/JortPob.git`

2. Open the project in your IDE of choice. I use VS 2022 so that's what I'd recommend for simplicity.

3. Copy the file "settings.example.json" and rename it to "settings.json". This is an untracked file for personal settings.

4. Install ModEngine3 with the installer: https://www.nexusmods.com/eldenringnightreign/mods/213?tab=description

5. Make a new file called `elden-scrolls.me3` and in the same folder make a `mods` folder, open it in a text editor and use this profile.
```toml
profileVersion = 'v1'
[[supports]]
game = 'elden-ring'

[[packages]]
id = 'elden-scrolls'
source = 'mods\'

[[natives]]
path = 'mods\regbinmeme.dll'
```

Pic for reference:
<img width="1392" height="405" alt="image" src="https://github.com/user-attachments/assets/9dd04ba4-fc11-4346-b1fd-2eaecc792b15" />

6. Download this dll that skips regulation.bin file size checks at startup. This is only avaiable in the `?ServerName?` Discord, and the `Elden Scrolls Dev` Discord, for now. Without this the game fails to load. Add it to the `mods` folder you created above.

7. Edit the settings file and set the paths to your game folders. Here is mine for reference. Make sure the OUTPUT_PATH goes to the mods folder you just set up.
<img width="1203" height="241" alt="image" src="https://github.com/user-attachments/assets/4c2df234-4f31-4975-ae97-c4fec46f648e" />

8. Download BSAUnpacker from nexus: https://www.nexusmods.com/skyrimspecialedition/mods/974

9. Run BSAUnpacker, load the Morrowind.BSA from data files and then hit extract all and tell it to ouput to the `Morrowind/Data Files` folder. If done correctly you will have a bunch files in `Morrowind/Data Files/Meshes` like this:
<img width="960" height="755" alt="image" src="https://github.com/user-attachments/assets/6fb8adce-1bf4-426d-914a-233331351aed" />

10. Install Blender: https://studio.blender.org/welcome/

11. Download and install the morrowind blender plugin: https://github.com/Greatness7/io_scene_mw
<img width="1157" height="701" alt="image" src="https://github.com/user-attachments/assets/b9fdd83d-a29f-40d6-bcfc-3af0190f625f" />

12. Download Nord's program NIF2FBX: https://github.com/Nordgaren/NIF2FBX

13. This is a commandline app that converts all nif files in a directory to fbx. Run the program with the parameters: `NIF2FBX.exe "P:\ath\to\blender.exe" "P:\ath\to\morrowwind"`
<img width="2754" height="913" alt="image" src="https://github.com/user-attachments/assets/592451c3-cb7b-4af1-8636-fcb2ae0de5c1" />

14. Download Greatness7's program Tes3Conv: https://github.com/Greatness7/tes3conv

15. This is another command line tool so run it with parameters: `tes3conv.exe "Data Files\Morrowind.esm" "Data Files\Morrowind.json"`. You should now have a Morrowind.json in the same folder as Morrowind.esm.
<img width="961" height="612" alt="image" src="https://github.com/user-attachments/assets/8343e568-d7ed-4967-8a53-b394dce38e01" />

16. Download the `common.emevd.dcx` from the Elden Scrolls Discord server and put it in the `mods/event` folder in your me3 profile.

17. Double click the `elden-scrolls.me3` file to run the game. Go to the church of elleh and you will be transports to Morrowwind.

You can use [Elden Ring Debug Tool](https://github.com/Nordgaren/Elden-Ring-Debug-Tool) or the [TGA Table](https://github.com/The-Grand-Archives/Elden-Ring-CT-TGA) to warp to the Church of Elleh and spawn items in. The latter requires Cheat Engine.
