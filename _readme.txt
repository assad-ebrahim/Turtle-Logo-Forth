Turtle-Logo, written by Assad Ebrahim, Copyright (c) 2016, Mathematical Science & Technologies  

Comments, bug-reports, feature-requests to: assad.ebrahim@mathscitech.org                       

License: GNU License - Users are permitted to re-use freely provided:                          
           1/ All modified versions are also made freely available under the GNU License            
           2/ Attributions are preserved (lines 1-6 and Revision History)                          


Download & Installation
======================

Available for download from: http://www.mathscitech.org/articles/downloads

Run turtle-logo.exe from any location.  

Install to: c:\totalcmd\

Will create a subfolder: turtle-logo

** Run _start_turtle_logo.bat to launch the program. **

Instructions will be shown at startup.


Key Features
===========
+ Draws directly to screen memory using assembly language set-char routine
+ Polling loop processes in real-time user submitted key presses to control the turtle and activate features
+ Evolved from 4-key (up,rotL,rotR,quit) to 11 key (see revision history)
+ Includes pen-up/pen-down/erase modes.  Pen-up switches pen color to screen bg-color so that cursor can move with appearance of not drawing; pen-down restores the previous color.
+ Uses intelligence for visual usability, e.g. cursor background changes to provide sufficient contrast for all turtle colors
+ Allows macro capability through associating macro to function pointer.  Has complete key processing engine.
+ Provides up to 9 independently recordable macros for playbacks
+ Provides status bars with on-screen help.


Revision History
===============
\ v1.4.4015 - AE - RELEASED, multi-macro record functionality
\ v1.4.4006 - AE - Added multi-macro record functionality (up to 9 diaries). (24 keys -- D + 2-9) (~800 lines of code) (2016-11-17--Thu--06:34, took 5 hours to add)
\ v1.3.4002 - AE - RELEASED, along with website article.  Further streamlined code.  Added turnkey executable. (2016-11-05--Sat--01:30)
\ v1.2.3999 - AE - Fixed various bugs (delete, pen-up).  Further streamlined code. ~750 lines of code. (2016-11-02--Wed--12:27)
\ v1.2.3989 - AE - Replaced if/else key processing with vectored execution.
\ v1.2.3979 - AE - Informed user how to restart logo
\ v1.2.3977 - AE - RELEASE (splash) Introduction of macro language; streamlining test harnesses.  Refactored code to simplify key-processing. (~800 lines) 2016-10-30--Sun--11:20
\ v1.1.3968 - AE - various bug fixes: home now lifts pen-up to protect home pixel
\ v1.1.3967 - AE - optimized cursor pallete for color contrast & usability.  cursor w/ different appearance when pen-up (white on silver) from white brush w/ pen-down (white on dark grey) 
\ v1.1.3966 - AE - Refactored design to use one key-processing loop.  (~700 lines)
\ v1.1.3963 - AE - RELEASE (15-key) Added hide/show turtle (;) so can complete a drawing with showing turtle (~750 lines) 2016-10-20--Thu--21:22
\ v1.0.3952 - AE - RELEASE to web - Added recording information into status bar.  2016-10-17--Mon--04:34  (~800 lines)
\ v0.9.3946 - AE - Added real pen-up/pen-down (i.e. does not erase previous pixels).  Changed back arrow to backspace (back arrow was confusing users).  Restores picture after help screen.  Added white border around picture area. 2016-10-08--Sat--21:24
\ v0.8.3940 - AE - Added menu and status bars with updating information (location, color, TODO: recording status) 2016-10-05--Wed--04:52  (+100 lines of code) 2016-10-06--Thu--07:54
\ v0.7.3934 - AE - RELEASE to web - **stable** 25.Sep.2016 Sun 22:18
\ v0.7.3925 - AE - 14-key - Release.  Added screenshot (Ctrl+F5) feature from DosBox. 2016-09-24--Sat--17:38  (first release to web)
\ v0.7.3924 - AE - 13-key - Release.  Added 'x' to cycle backward through colors.
\ v0.7.3922 - AE - Release.  Bug fixes (2 causes of crashes)
\ v0.7.3919 - AE - Release.  Packaged with DOS Box. 2016-09-22--Thu--23:45
\ v0.6.3918 - AE - Release.  Added two-dimensional wrap-around. (~500 lines)
\ v0.6.3916 - AE - 12-key - Release.  Added 'r' recording toggle, save to numbered macro ('1' by default).  Replay using key processing engine.  Removed 'p'.
\ v0.5.3907 - AE - Release.  Revised processing engine to allow full macro key control; completed example macro feature - 2016-09-04--Sun--14:21  (~600 lines 2016-09-18--Sun--12:19)
\ v0.4.3905 - AE - 11-key - Release.  Added 'p' prototype play macro feature to control turtle programmatically - 2016-09-04--Sun--07:23
\ v0.3.3904 - AE - 10-key - Release.  Added 'ESC' to clear screen; added cursor contrast; several bug fixes - 2016-09-04--Sun--06:10
\ v0.2.3898 - AE - 9-key - Added ? to show help screen - 11:31
\ v0.2.3897 - AE - 8-key - Added pen up/down toggle and back arrow as erase - 2016-09-03--Sat--10:28
\ v0.2.3895 - AE - 6-key - Release.  Added 'h' home to find cursor 16:35
\ v0.1.3892 - AE - 5-key - added pen color change 'c' - 14:39  
\ v0.1.3891 - AE - 4-key - Release.  Removed back arrow (was confusing test users) - up,rotL,rotR,quit - 13:59
\ v0.1.3890 - AE - 5-key - Release. first working version - 4 arrow keys + quit - 2016-08-28--Sun--11:57  (~300 lines)
\ v0.0.3883 - AE - began project - 2016-08-22--Mon--02:19



Major changes
============
Through v1.1.3965:
The core of the program is the keystroke processing loop, which uses two if-else filters to match valid keystrokes to an event handler.  First is key-process which processes keys that should not be recorded into any macro, i.e. clear-screen, show-help, toggle turtle visibility, turtle-home, as well as record-macro and play-macro.  Second is turtle-process, which processes 4 recordable turtle commands: turtle-move, change-heading, change pen color, toggle pen-up/pen-down, and if macro-recording is active, also logs them to diary.  Any key-presses not recognized by either filter are ignored and not retained in the macro log.  During playback of a macro (triggered by key-process), turtle-process is called after each instruction is read.

Key-processing was refactored in v1.1.3966 into a single processing word using Forth’s executable token (function pointer) concept.
