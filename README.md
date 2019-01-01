# Bastion TAS Tools
Tas tools for the game Bastion.

Hotkeys aren't currently configurable , input file is created for you if it does not exist.

In the directory where the game executable is, you can create it however if you want, it looks for Bastion.rec

Hotkeys:

F1:  to pause/start framestep

F2: To toggle cursor unlocked mode (if you disable this then you can't use the cursor while paused\framestepping), it's for recording 
a clean look of playback, otherwise the cursor will jitter between two states.

F4: start playback , read from Bastion.rec in the directory where the game executable is located.
Note: You may use multiple input files, see Commands section for input files.<br/>But note that multi-level applies from the main input file only, so included input files may not read from another file.
--------------------------------------------------------

Numpad Plus:   Increase Game Speed
Numpad Minus:  Decrease Game Speed
Numpad Divide: Return to normal Game Speed

C:  To copy your cursor location while playing the game (works in paused state\framestepping state.) it will copy it in the format of:
    1,Move,x,y and you can ctrl+v it into the input file for easy use.
    
]: To step one frame (this also reloads the input file so you can make changes to the inputs while framestepping.)

Commands read from input file are in the format of:
   frames, Action
   
Accepted commands are: <br />
   Move, xPos, yPos
   Click (acts as clicking mouse)
   Attack1
   Attack2
   Stop
   Evade
   SecretSkill
   Heal
   Use
   Defend
   Pack (functions as tab)
   Reload
   Menu (escape)
   NextTarget
   PrevTarget
   PCancel (alias for Proceed+Menu on same frame)
   Proceed
   
   Read,FileName.rec - Accepts a sub-command for the input file to read from <br />
           Multi-level files are allowed, but must end in .rec and must be in a folder named  Includes in the main game directory.
   RNG, num
   Random, num
   Seed, num
      The above function the same, where you set the seed for the RNG generator.
   
    



 
