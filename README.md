# LSO2MIDI
Converts MicroLogic/Logic files to MIDI files

# Background
I had hunderds of old .lso files from the 90's using MicroLogic 2.0.7 (by eMagic).
I wanted a quick & dirty solution for triaging between the different music files, and loading each one of them in DosBox running Windows 3.1 just to listen to them was too manual, so I created this project.

# What it does
(tries to) Convert .lso files from MicroLogic 2.0.7 and Logic Platinum 4.7/5.x to MIDI files

# Caveats 
Code is hacky and buggy. If you find a bug or a runtime crash please send me the .lso so I can fix it (or better yet, fix it and create a pull request :D )

# Overview

There are 3 projects wrapped inside the solution file:

### ConvertLSO 
Command line tool for converting .lso file to .mid file
Usage: 
```
ConvertLSO.exe [LSO file] [MIDI output filename]
```

### MIDIParser
A utility to track problems with generated (or any) MIDI file. Currently very crude.
Usage:
```
MIDIParser.exe [MIDI file]
```

### Music
Library that holds LSO and MIDI files parsing code

