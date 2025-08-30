**Setup instructions**

1. Pull the project: `git clone https://github.com/infernoplus/JortPob.git`

2. Open the project in your IDE of choice. I use VS 2022 so that's what I'd recommend for simplicity.

3. Copy the file "settings.example.json" and rename it to "settings.json". This is an untracked file for personal settings.

4. Edit the settings file and set the paths to your game folders. Here is mine for reference.
<img width="778" height="186" alt="image" src="https://github.com/user-attachments/assets/641b6156-ba75-48d8-a85d-85a818c31eaf" />

5. Download BSAUnpacker from nexus: https://www.nexusmods.com/skyrimspecialedition/mods/974

6. Run BSAUnpacker, load the Morrowind.BSA from data files and then hit extract all and tell it to ouput to the `Morrowind/Data Files` folder. If done correctly you will have a bunch files in `Morrowind/Data Files/Meshes` like this:
<img width="960" height="755" alt="image" src="https://github.com/user-attachments/assets/6fb8adce-1bf4-426d-914a-233331351aed" />

7. Install Blender: https://studio.blender.org/welcome/

8. Download and install the morrowind blender plugin: https://github.com/Greatness7/io_scene_mw
<img width="1157" height="701" alt="image" src="https://github.com/user-attachments/assets/b9fdd83d-a29f-40d6-bcfc-3af0190f625f" />

9. Download Nord's program NIF2FBX: https://github.com/Nordgaren/NIF2FBX

10. This is a commandline app that converts all nif files in a directory to fbx. Run the program with the parameters: `NIF2FBX.exe  "P:/ath/to/morrowwind/Data Files/textures" "P:/ath/to/morrowwind/Data Files/Data Files/meshes" "P:/ath/to/blender.exe"`
<img width="2754" height="913" alt="image" src="https://github.com/user-attachments/assets/592451c3-cb7b-4af1-8636-fcb2ae0de5c1" />

11. Download Greatness7's program Tes3Conv: https://github.com/Greatness7/tes3conv

12. This is another command line tool so run it with parameters: `tes3conv.exe "Data Files\Morrowind.esm" "Data Files\Morrowind.json"`. You should now have a Morrowind.json in the same folder as Morrowind.esm.
<img width="961" height="612" alt="image" src="https://github.com/user-attachments/assets/8343e568-d7ed-4967-8a53-b394dce38e01" />

13. Install ModEngine3: https://www.nexusmods.com/eldenringnightreign/mods/213?tab=description

14. 
