#DeskCritters

DeskCritters is a Windows desktop pet app I'm making for Terra. The idea is to have little creatures living on your desktop, walking around, doing their own thing, and eventually(hopefully) interacting with your files, icons, and each other.

I started this because I played too much of that one god simulator game where there are pixilated characters going through your game and allat and thought having a bunch of little pixel-art creatures wandering around my desktop would be pretty cool. Turns out getting Windows to work is a lot harder than I expected.(boooo windows)

The project is still in its early stages, but there's a working prototype.

#Download and Setup

DeskCritters currently runs on Windows 10/11.

Go to the Releases page.

Download DeskCritters-v0.3-Windows.zip from the latest release.

Extract the ZIP somewhere on your computer.

Open the extracted folder and run DeskCritters.exe.

Whatever we got so far is here:

Five selectable species: slimes, cats, dogs, dragons, and elves

Three personalities: lazy, normal, and energetic

Random walking, resting, and direction changes

Different movement speeds depending on species

Cats occasionally sprint

Dragging creatures around the screen

Right-click menu for changing species, pausing, and exiting

Experimental desktop-layer integration

Keyboard shortcuts for desktop mode and exiting

Right now, only one creature can be active at a time, and the graphics are still placeholders.

Controls
Drag creature: Left-click and drag

Change species: Right-click

Pause/resume: Right-click menu

Toggle desktop mode: Ctrl + Alt + D

Exit: Ctrl + Alt + Q

Desktop mode is still experimental and might not work properly on every computer. The regular floating mode is the most reliable option for now.

##VISIONS THAT WILL PROBABLY SUCK MY SOAL OUT: 

The biggest thing I'm working on is getting creatures to actually live on the desktop layer, behind the icons, rather than just floating around as separate windows.

After that, I want to add multiple creatures at once, proper pixel-art sprites, more animations, and unique behaviors for each species. Eventually, creatures should be able to climb icons, fly around, interact with each other, and have little homes.

Running From Source

You'll need Windows and the .NET 10 SDK.

git clone https://github.com/AmbushDeAKewlBoi/DeskCritters.git
cd DeskCritters
dotnet run

DeskCritters is written in C# using WPF/XAML, with some Windows API integration for the desktop-layer experiments.

Still very much a work in progress.
