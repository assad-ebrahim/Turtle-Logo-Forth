\     Turtle-Logo, written by Assad Ebrahim, Copyright (c) 2016, Mathematical Science & Technologies
\     Available for download from: htkp://www.mathscitech.org/articles/downloads                     
\     Comments, bug-reports, feature-requests to: assad.ebrahim@mathscitech.org                       
\     License: GNU License - Users are permitted to re-use freely provided:                          
\           1/ All modified versions are also made freely available under the GNU License            
\           2/ Attributions are preserved (lines 1-6 and Revision History)                          

\ This is a Turtle logo program for Windows operated entirely with a few keys, and with several advanced features, including macro recording and replay (see _readme.txt).
\ It is written in ~750 lines of Forth code, targeted for F-PC (Forth-PC).
\ A run-time package is provided from the website listed above.  
\ Run "start_turtle_logo.bat" after installation.  Further instructions are provided in the _readme.txt
\ From within F-PC: fload turtle.fs, then: turtle-go
 
\ Revision History
: rev# ." 1.4.4016" ;
: welcome ( -- ) 
\    cr cr ." Welcome to Turtle Logo! (" rev ." , (c) 2016, Assad Ebrahim)"
    cr cr ." INSTRUCTIONS - TURTLE LOGO - (Ver " rev# ." )"
    cr
    cr ."   FWD arrow - moves Turtle one step along current heading, and paints."
    cr ."   BACKSPACE - moves Turtle back one step, and erases"
    cr ."   LEFT/RIGHT arrow - changes Turtle's heading 45 deg counter-/clock- wise"
    cr ."   ' - toggles pen up (to move without painting) or down (to draw again)"
    cr ."   C / X - cycles pen color (fwd/back) through 16 choices."
    cr ."   H - sends Turtle to home position at bottom left of screen."
    cr ."   ; - toggles Turtle's visibility (hide or show)."                            \ not on main screen
    cr
    cr ."   D - toggles through diaries (1-9) to record into"
    cr ."   R - starts/stops recording instructions to Turtle into current diary"       \ can record nested macros, but not recursive macros
    cr ."   1-9 - plays the instructions recorded into corresponding diary"
    cr
    cr ."   CTRL+F5 - takes screenshot (stored in c:\\totalcmd\\turtle-logo\\captures)"
    cr ."   ESC - clears screen -- after confirmation"
    cr ."   ? - shows HELP screen (this one).  Note - your drawing is safe."
    cr ."   Q - quits the program.  Come back again!"
    cr
    cr ." Happy Turtle-ing!  
    cr cr ." For ideas, see web article at: www.mathscitech.org/articles/turtle-logo-forth"
    cr cr cr
;

\ ============ Control keys
\ ASCII codes for key commands
200 constant fwd-key
205 constant right-key
right-key  constant cw-key
203 constant left-key
left-key constant ccw-key
8  constant backspace
backspace constant erase-key
39 constant tick
tick constant pen-updown-key    \ pen up/down
'c' constant color-fwd-key      \ change color
'x' constant color-back-key     \ change color
'd' constant diary-key
'r' constant record-key
'1' constant play-macro-1-key
';' constant show-hide-key      \ turtle hide/view 
'h' constant home-key \ home
27 constant esc
esc constant clear-screen-key
'?' constant help-key
'q' constant quit-key

\ === Misc
: do-nothing ;  \ for readability
: reverse ( a b c -- c b a ) swap rot ;
: arrow ( -- )  '-' emit '>' emit ;
: dump-key ( asc -- ) dup . arrow space emit ;
: show-key ( -- asc ) key dup dump-key ;  \ echoes character and its ascii code
: asc2digit ( asc -- digit )  '0' - ; 
: digit2asc ( digit -- asc )  '0' + ;
: pause-key ( -- ) ." PRESS ANY KEY TO CONTINUE... " key drop ;
: pause-quit ( -- ) key 'q' over = swap 'Q' = or if abort then ;
: pq ( -- ) pause-quit ;   \ abbreviation - useful for single stepping
: exit? ( -- t/f )   \ y or ESC are confirmation
    key 'y' over = ( asc t/f ) over 'Y' = or ( asc t/f) swap esc = or
    if true else false then 
;
: quit? ( -- t/f )   \ q or y are confirmation
    key 'y' over = ( asc t/f ) over 'Y' = or ( asc t/f) over 'q' = or swap 'Q' = or
    if true else false then 
;


\ === Memory
: bytes ( n -- n )  ;                           \ 1 *
2 constant wordsize                             \ 16-bit word length on 8086 chip
: cells ( num -- num*wsz )  wordsize *  ;    
: c@+ ( adr -- adr+ val )   dup 1 + swap c@ ;   \ fetch char and advance
: c!+ ( val adr -- adr+ )   dup 1 + -rot c! ;   \ store char and advance
: @+  ( adr -- adr+ val )   dup 1 cells + swap @ ; \ fetch word and advance
: !+ ( val adr -- adr+ )   dup 1 cells + -rot ! ;   \ store char and advance
: zero-mem  ( adr sz -- ) 0 fill ;            \ fill sz bytes with 0
: blank-mem ( adr sz -- ) BL fill ;           \ fill sz bytes with blanks ($20)
: shl4 ( val -- val ) $10 * ;  \ make hi nybble
: shr4 ( val -- val ) $10 / ;  \ get hi nybble

\ Color encoding: hi nybble holds bgcolor, low nybble holds fgcolor ( for text )
: color-encode-fgbg ( fgcolor bgcolor -- colorcode )  shl4 + ;
: color-decode-fgbg ( colorcode -- fgcolor bgcolor )  
    dup $0F and   \  lo nybble fgcolor
    swap shr4     \   hi nybble bgcolor ) 
;

\ =========  Colors
 0  constant   black        
 1  constant   navy         
 2  constant   green        
 3  constant   teal         
 4  constant   red          
 5  constant   purple        
 6  constant   brown        
 7  constant   silver         
 8  constant   slate      
 9  constant   blue    
 10  constant  lime  
 11  constant  aqua    
 12  constant  orange
 13  constant  pink        
 14  constant  yellow       
 15  constant  white  
\ alternate spellings
 silver constant grey
 silver constant gray
slate constant darkgrey
slate constant darkgray

16 constant num-colors
num-colors 1 + constant num-color-strings \ 1 more for pen-up string
num-colors constant pen-up-colorcode

create screen-color 1 bytes allot   \ holds canvas color codes 0..15
: set-screen-color ( color -- ) screen-color c! ;
: get-screen-color ( -- color ) screen-color c@ ;
white set-screen-color   \ default

create cursor-color 1 bytes allot \ holds cursor color (bg color)
: set-cursor-color ( bgcolor -- ) cursor-color c! ;
: get-cursor-color ( -- bgcolor ) cursor-color c@ ;
silver set-cursor-color   \ default

\ === Foreground/Background color mapping.
\ Color of pen (fgcolor) needs to contrast sufficiently with background to discern.  So background must change dynamically as pen-color changes.
\ Cursor (bgcolor) should also have sufficient contrast to spot it easily.  (ALTERNATIVE: could have it be paint color, then would need fg char to be high contrast or flashing)
\ The design chooses a bgcolor based on the chosen pen-color.
\ Theory of color contrasts: htkp://www.worqx.com/color/color_contrast.htm
\ Color contrast checker tool (contrast ratio): htkp://webaim.org/resources/contrastchecker/
\                                             htkp://juicystudio.com/services/luminositycontrastratio.php#specify

create fgbg-lookup num-color-strings bytes allot   \ lookup (hash) table holds optimized (designer-provided) foreground (text) and background (cursor) color combinations
: fgbg! ( fg bg -- ) swap fgbg-lookup + c! ;  \ store bg color into hash indexed by fg
: set-pen-up-bg-color ( -- ) num-colors silver fgbg! ; \ store in last slot of array
: set-fgbg-palette ( -- )  \ set color palette: given fg-, set optimized bg-color, considering contrast ratio (see Table in documentation)
    \ fg    bg
    black white fgbg!
    navy aqua fgbg!
    green lime fgbg!   \ not great, but ok
    teal aqua fgbg!
    red yellow fgbg!
    purple white fgbg!
    brown silver fgbg!
    silver black fgbg!
    slate silver fgbg!
    blue aqua fgbg!
    lime slate fgbg!
    aqua navy fgbg!
    orange yellow fgbg!
    pink black fgbg!
    yellow brown fgbg!
    white slate fgbg!
    set-pen-up-bg-color
;
: get-optim-bg-color ( fg -- bg ) fgbg-lookup + c@ ;
: set-optim-bg-color ( pen-color-fg -- ) get-optim-bg-color set-cursor-color ;

create pen-up-state 1 cells allot  \ true if pen is up else false
: set-pen-up-state ( -- ) true pen-up-state ! ;
: get-pen-up-state ( -- ) pen-up-state @ ;
: clear-pen-up-state ( -- ) false pen-up-state ! ;

create pen-color 1 bytes allot  \ foreground color, 0..15
: get-pen-color ( -- c ) pen-color c@ ;
: set-pen-color ( c -- )  dup pen-color c! set-optim-bg-color ;     \ automatically change background so turtle can be seen better
: toggle-color ( # -- )  get-pen-color + 16 mod set-pen-color ;     \ toggles through 16 colors, fwd if +1, back if -1
: turtle-color ( asc -- )
    color-fwd-key = if 1 else -1 then \ sign determines forward or backward
    toggle-color
;

create prev-pen-color 1 bytes allot
: save-prev-pen-color ( -- ) get-pen-color prev-pen-color c! ;
: restore-prev-pen-color ( -- c ) prev-pen-color c@ set-pen-color ;

: do-pen-up ( -- )    
    set-pen-up-state   save-prev-pen-color  
    get-screen-color set-pen-color   
    pen-up-colorcode  set-optim-bg-color  \ use a custom appearance when in pen-up-mode
; 
: do-pen-down ( -- )  clear-pen-up-state  restore-prev-pen-color  ;


\ === Strings
: create-string ( ... )    \ creates counted string (zstring) with character count stored in first byte
\ test
\ : tmp " Hello World!" ;
\ tmp create-str str
\ str type
    create  ( addr n  <name> -- )
    here >R 
    dup 1 + bytes allot   ( adr n )  \ counted string
    dup R> c!+ ( ptr n str' )
    swap cmove
does> ( -- ptr n ) 
    c@+
;
: attach-field ( array str n -- array' n )  \ attach string (n bytes) to array of fixed length (L > n) strings, returning new position and n bytes used
    rot 2dup + >R   \ store ptr to position after string
    swap dup >R     \ store n  ( str array n )
    cmove           \ copy 
    R> R> swap      \ restore results
;
: pad-field ( str n record-length -- str' )  \ pad with blanks the end of a field to fill up full field length (assumes n < reclength)
    swap - ( str diff ) 
    2dup + -rot blank-mem 
;  
: get-field ( array index strlen -- ptr len ) tuck * rot + swap ;  \ calculates position of ith string stored in collection of fixed length strings, returns pointer and length

\ === Color Strings
6 constant strlen-color
create color-names   num-color-strings strlen-color * bytes allot
: clear-color-names ( -- ) color-names num-color-strings strlen-color * blank-mem ;
clear-color-names
: get-color-name ( index -- ptr n ) color-names swap strlen-color get-field ;

: attach-color ( array str n -- array' ) attach-field strlen-color pad-field ;
: attach-colors ( -- )
    color-names
    " black"        attach-color
    " navy"         attach-color    \  dark blue
    " green"        attach-color    
    " teal"         attach-color    \  sometimes called aqua
    " red"          attach-color    \  maroon
    " purple"       attach-color    
    " brown"        attach-color    
    " silver"       attach-color  \  grey
    " slate"        attach-color  \  darkgrey
    " blue"         attach-color       \  lt blue
    " lime"         attach-color        \   lt green" 
    " aqua"         attach-color       \  lt aqua  cyan  lt blue
    " orange"       attach-color    
    " pink"         attach-color    \  magenta  fuschia
    " yellow"       attach-color      
    " white"        attach-color    
    " PEN-UP"       attach-color
    drop
;

\ States describing status of Recording (on or off or full etc.) - for menu2
create rec-status 1 bytes allot
0 constant rec-status-ready
1 constant rec-status-recording
2 constant rec-status-full
3 constant rec-status-reminder  \ still recording, i.e. a flash
: set-rec-status ( r -- ) rec-status c! ;
: get-rec-status ( -- r ) rec-status c@ ;

\ Record strings
12 constant strlen-recording
4  constant num-record-messages
create rec-names  num-record-messages strlen-recording * bytes allot
: clear-rec-names ( -- ) rec-names num-record-messages strlen-recording * blank-mem ;
clear-rec-names
: show-record-strings ( -- ) rec-names num-record-messages strlen-recording * type cr pq ;
: attach-rec-status ( array str n -- array' ) attach-field strlen-recording pad-field ;
: attach-recording-strings ( -- )
    rec-names
    " Record to #1"     attach-rec-status   \ change diary names - default is 1
    " RECORDING..."     attach-rec-status
    "  Full!"           attach-rec-status
    " RECORDING..."     attach-rec-status
    drop
;
: get-record-name ( rec-status -- ptr n ) rec-names swap strlen-recording ( array index strlen ) get-field ; 
11 constant diary-num-pos   \ diary num position in string
: update-diary-string ( digit -- ) 1 + digit2asc rec-names diary-num-pos + c! ;

\ === Heading & Compass Directions
\ 9  Compass Directions & Heading Rotation with Keys
\ turtle cycles through the directions (cw, ccw)  (cw from N to NW rotation)
\ cw: N > NE > E > SE > S > SW > W > NW
\ ccw: opposite
\ There are 8 states rotating -- Heading is an integer 0 - 7 which indexes into a state array and gets the heading character
\ The maths of rotation is mod, so this is arithmetic mod-8

create heading 1 allot  \ 0 to 7
: set-heading ( h# -- ) heading c! ;
: init-heading ( -- ) 0 set-heading ;  \ default heading is N=0
: get-heading ( -- h# ) heading c@ ;
: adjust-heading ( h-delta -- )  get-heading + 8 mod set-heading ;  \ each h-delta nudges cw by 45 degrees, negative ccw by 45 deg

\ graphical symbols to show what direction turtle is heading - numbers are stored little endian (small byte first)...
$5E constant  NN   \    ^  alternates: $70 p  $56 V  $5E ^
$2F constant  NE   \    / 
$3E constant  EE   \    >  
$5C constant  SE   \    \  
$76 constant  SS   \    v  alternates: $62 b  $59 Y  $56 V
$2F constant  SW   \    / 
$3C constant  WW   \    < 
$5C constant  NW   \    \ 
: create-heading-symbol
    create ( <name> -- )
     NN c, NE c, EE c, SE c, SS c, SW c, WW c, NW c,   \ headings in cyclic order 
    does> ( h# -- asc )
        + c@
;   
create-heading-symbol  heading-symbol

\ direction names, letters backwards because numbers are stored in lower-endian form, i.e. low byte first
$204E  constant  N-name    
$454E  constant  NE-name   
$4520  constant  E-name    
$4553  constant  SE-name   
$5320  constant  S-name    
$5753  constant  SW-name   
$5720  constant  W-name    
$574E  constant  NW-name   
: create-heading-name
    create ( <name> -- )
        N-name , NE-name , E-name , SE-name , S-name , SW-name , W-name , NW-name ,
    does> ( h# -- ptr )
        swap cells + 
;
create-heading-name  heading-name

\ =========== Graphics.
\ This program is written in a 16-bit Forth ( which requires running in DOSBOX, a DOS emulator ) as a quick way to produce graphics.  
\ This is because in DOS it is easy to set pixels directly from program, which is not the case in modern windows which requires going through a graphics library such as SDL or OpenGL.

\ Text mode graphics works with 25 rows x 80 columns (indices y:0..24 and x:0..79), giving 2,000 usable cells (25x80).  
\ Behind this there are 160 columns but for some reason drawing only works correctly on even columns ( in practice the 80 column indices appear as multiples of 2, i.e. 0,2,4,...,158 )
\ Pixel locations are specified in linear order from 0 to 3998d ($F9E in hex) with the final 3999d cell not visible (even pixel only issue).

\ Two coordinate systems for graphics: PHYSICAL COORDINATES (aka full-screen coordinates)which includes all 25 x 80 cells, and USER COORDINATES, i.e. within a window, (i.e. a subset of the 25 x 80 full-screen).  
\ Two representations: Cartesian: x,y (across, down) vs. Matrix: i,j (row,column).

\ This program uses x,y mostly in user coordinates, converting between physical coordinates using xy2lin and lin2xy.  The exception is for menu-ing system which is exclusively in physical coordinates.

\ Example - FPC uses rows 0-1 (1st line is Forth bar, and 2nd line is erased after command scrolls screen up), so to see a pixel within F-PC start at linear location 320.  
\ Furthermore, FPC uses columns 69-79 of the first 12 rows, with the full 80 cols starting from row 13.

\   Video programming reference: htkp://ece.wpi.edu/~wrm/Courses/EE3803/Labs/roehrl.html
\   Video mode reference: htkp://www.wagemakers.be/english/doc/vga

\ VGA screen resolution / graphics modes
$1 constant text40x25-8color
$3 constant text80x25-8color            \ text mode (8 color)
$8 constant text80x25-16color           \ default F-PC mode
$4 constant graphCGA-320x200-8color
$10 constant graphEGA-640x350-16color
$12 constant graphVGA-640x480-16color   \ default VGA graphics mode

code set-vid-mode ( mode -- )  \ call BIOS procedure to set the desired video mode
    pop ax          \ load AL with video mode number 
    mov ah, # 0     \ load AH with BIOS service (set video mode)
    int $10		    \ call BIOS procedure
next end-code

: set-video-mode ( -- ) text80x25-16color set-vid-mode ;
\ : set-video-mode ( -- ) $12 set-vid-mode ;
\ : set-video-mode ( -- ) graphVGA-640x480-16color set-vid-mode ;

\ === Physical (full-screen) coordinates.  This is what the low-level pixel display functions use.  0,0 is top left pixel of the screen.
   0  constant lin-zero
3998  constant lin-end    \ $F9E hex
 160  constant col-slots  \ note: odd slots are invalid, so j runs from 0..79 in steps of 2
  80  constant num-cols
  25  constant num-rows

 1  constant top-menu-height
 1  constant bottom-menu-height
 1  constant buffer-thickness  \ frame all round

\ === User (screen) area in physical coordinates
 0 top-menu-height + buffer-thickness +                   constant screen-top
 num-rows bottom-menu-height -  buffer-thickness - 1 -    constant screen-bot
 buffer-thickness                                        constant screen-left
 num-cols buffer-thickness - 1 -                         constant screen-right
screen-bot screen-top - 1 +                              constant screen-height
screen-right screen-left - 1 +                           constant screen-width
 3 1 - col-slots *   constant row#3   \ 3rd line ($140) is first one visible in interactive mode

\ === Linear offset in physical coordinates to 0,0 in user coordinates (top left pixel that the user can access, i.e. excluding menus and buffers)
 create lin-offset 1 cells allot
: set-lin-offset ( -- ) screen-top col-slots * screen-left 2 * + lin-offset ! ;
: get-lin-offset ( -- offset ) lin-offset @ ;

: set-interactive-graphics-mode ( -- )  row#3 lin-offset ! ;
set-interactive-graphics-mode       \ default program mode (for testing) 

\ === Coordinate conversion
\ coordinates are in cartesian convention ( x is across, y is down ) not matrix convention ( row i, col j ).  linloc is in physical coordinates.
: xy2lin ( x y -- linloc )  \ user to physical coordinates
\ test: 0 0 xy2lin 
    col-slots *     \ y rows to linear array
    swap 2 *        \ x cols (0..79) to even x-slots (0,2,4,...158) since only evens are visible
    +               \ combine to get linear position
    get-lin-offset + \ plus linear offset to reference to user coordinates (0,0)
;
: lin2xy ( linloc -- x y )  \ physical to user coordinates
    get-lin-offset -   \ remove offset
    col-slots /mod ( x y ) 
    swap 2 /          \ to screen char coords (80 per row)
    swap
;
: xy2lin-physical ( x y -- linloc )  col-slots * swap 2 * + ; \ physical to physical coordinates

create cursor-loc 1 cells allot
: set-location ( x y -- ) xy2lin cursor-loc ! ;
: get-location ( -- x y ) cursor-loc @ lin2xy ;
: init-start-pen-position ( -- x y ) 0 screen-height 1 - 2dup set-location ;
: print-location ( x y -- x y )   2dup ." (x: " swap . ." y: " . ." )" ;    \ TO DO: Add this to status bar

\ === Painting to Screen- Low-level Character Routines
\ color information is encoded into a single byte for use in assembly language routine.  
\ color-code (e.g. $F4) holds background color (bgcolor) in high nybble ($F is white) and foreground (text) color in low nybble ($4 is red)
\ pixel position is in linear encoded form, i.e. row/col are encoded within a single wrap-around array of screen positions

code poke-pixel ( linear-location  color-code  ascii-code  -- )  \ sets a pixel at linloc with given color ascii character.  
    pop cx                  \ load CL with ascii char
    pop ax                  \ load AL with color-code (foreground & background)
    mov ch, al              \ create text pixel (color|char) in (CH|CL)
    pop bx                  \ load BX with screen location
    push ds                 \ preserve data stack
    mov ax, # $B800         \ load AX with graphics address B800 hex (vga memory base address for direct display to screen)
    mov ds, ax              \ set data segment to graphics address (cannot load literal directly, only via register)
    mov 0 [bx], cx          \ display pixel in desired location in graphics memory [B800:bx] (0 data segment offset)
    pop ds                  \ restore data stack
next end-code
\ test: 1000 228 42
\ test: 320 228 42 

code peek-pixel ( linear-location -- color-code ascii-code )
    pop bx              \ load BL with screen location
    push ds             \ preserve data stack
    mov ax, # $B800     \ load AX with graphics address (B800 hex)
    mov ds, ax          \ set data segment to graphics address
    mov cx, 0 [bx]      \ load CX with pixel data from desired location in graphics memory (B800:bx)  (CH|CL)=(color|char)
    pop ds              \ restore data stack
    mov ax, # 0         \ clear AX
    mov al, ch          \ load AX with color-code
    mov ch, # 0         \ clear CH, leaving CL with ascii code
    push ax             \ return color-code
    push cx             \ return ascii code    
next end-code

: set-char ( linloc asc fgcol bgcol -- ) color-encode-fgbg swap poke-pixel ;    \ test: row#3 '*' red yellow
: poke-xy  ( x y asc fgol bgcol -- ) >R >R >R xy2lin R> R> R> set-char ;

: get-char ( linloc -- asc fgcol bgcol ) peek-pixel swap color-decode-fgbg ;    \ test: turtle-init 0 20 read-screen-pixel
: read-screen-pixel ( x y -- asc fgcolor bgcolor ) xy2lin get-char ;

: fill-screen ( color start-loc end-loc -- )  \ color (0-15) or color-name ( black, white, yellow, red, ... )
    begin ( color start end )
        2dup < 
    while ( color start end )
        -rot ( color start) 2dup BL rot dup set-char
        2 +  \ only even cells are visible in this graphics mode.  Odd cells go green
        rot 
    repeat
    2drop drop
;
: black-screen  ( -- ) black lin-zero lin-end fill-screen ;

create border-color 1 bytes allot 
: set-border-color ( color -- ) border-color c! ;
: get-border-color ( -- color ) border-color c@ ;
silver set-border-color 

: hline-draw ( color x1 x2 y -- )
    -rot swap do ( color y ) 
        i over xy2lin-physical bl 3 pick dup set-char
    loop
    2drop 
;
: vline-draw ( color x y1 y2 -- )
    swap do ( color x )
        dup i xy2lin-physical bl 3 pick dup set-char
    loop
    2drop
;
: borders-draw ( color -- )  \ all coords are in physical space
    get-border-color 0 80 1 hline-draw
    get-border-color 0 80 23 hline-draw
    get-border-color 0 1 23 vline-draw
    get-border-color 79 1 23 vline-draw
;


\ === Caching Pixels
3 constant pixel-sz
create pixel pixel-sz bytes allot  \ asc fgcolor bgcolor
: store-pixel ( asc fgcolor bgcolor ptr -- ptr' ) >R reverse R> c!+ c!+ c!+ ;
: cache-pixel ( x y -- ) read-screen-pixel pixel store-pixel drop ;
: fetch-pixel ( pixel -- asc fgcolor bgcolor ptr' ) c@+ swap c@+ swap c@+ swap ;

\ === Painting to Screen- High Level Routines
: show-heading ( x y h# -- ) -rot xy2lin swap heading-symbol get-pen-color get-cursor-color set-char ;  \ draw symbol on screen 
: paint-xy ( x y -- x y ) 2dup xy2lin BL get-pen-color dup ( loc asc fg bg ) set-char ; \ test 3 12
: erase-xy ( x y -- x y ) 2dup xy2lin BL white white set-char ;
: restore-xy ( x y -- x y ) 2dup xy2lin pixel fetch-pixel drop set-char ;

\ === Picture Memory
num-cols num-rows * constant num-pixels-per-screen
num-pixels-per-screen pixel-sz * constant picture-size
create picture picture-size bytes allot      \ memory to hold the picture
: picture-clear ( --- )  picture picture-size zero-mem ;
picture-clear

: save-picture ( -- ) picture num-pixels-per-screen 0 do i 2 * get-char 3 roll store-pixel loop drop ;
: restore-picture ( -- ) picture num-pixels-per-screen 0 do i 2 * swap fetch-pixel >R set-char R> loop drop ;

\ === Paint to Screen -- Menu System
: menu1-text ( -- )  " Turtle Logo [arrows]move/rotate [Bksp]erase [']penup/dn [H]ome [Esc]clear [?]hlp" ;  \ 80 char max
0  constant row#0
: menu2-text ( -- )  " -- [X/C]olor: ------ [R]:------------ [#1-9]play [D]iary [Ctl+F5]capture [Q]uit" ;  \ 80 char max
24 constant row#24
\ character positions in menu2
0  constant menu2-h#
14 constant menu2-c#
25 constant menu2-r#
\ Positions on menu to update information
: menu2-heading-position ( -- loc )  menu2-h# row#24 xy2lin-physical ;
: menu2-color-position ( -- loc )    menu2-c# row#24 xy2lin-physical ;  
: menu2-recording-position ( -- loc) menu2-r# row#24 xy2lin-physical ;

create menu-style 2 bytes allot   \ hold styling
: set-menu-style ( fg bg -- ) swap menu-style c!+ c! ;
: get-menu-style ( -- fg bg ) menu-style c@+ swap c@ ;

: menu1-style ( -- )  white navy set-menu-style ;
: menu2-style ( -- )  white slate set-menu-style ;
: create-menu2-rec-style
    create 
        white c, slate c,  \ if 0 
        red c, yellow c,      \ if 1
        navy c, yellow c,     \ if 2
        white c, red c,       \ if 3
    does> ( -- )
       get-rec-status 2 * bytes + c@+ swap c@ set-menu-style
; 
create-menu2-rec-style menu2-rec-style

: menu-print-to-screen ( ptr nchar loc -- )  
    swap 0 do ( ptr loc )
        dup 2 + -rot  ( loc' ptr loc )                 \ save next position
        swap c@+ rot swap ( loc' ptr' loc asc )         \ fetch char
        get-menu-style set-char                         \ print-char
        swap ( ptr' loc )
        loop 2drop
;

\ create a menu with text, a default writing style, and a row location to show-up as.  in execution displays the menu
: create-menu ( menu-text xt-menu-style row# <menu-name> -- )    
    create ( <name> )                             \ create dictionary entry
    col-slots * , ,                               \ first cell holds lin-offset (physical coords), next cell holds xt of menu writing style
    here >R num-cols bytes allot                  \ allocate memory for string
    R> dup num-cols blank-mem                     \ blank out string
    swap cmove                                    \ load with menu-text
does> ( [adr] -- )                                \ prints menu
    @+ swap      \ read location
    @+ execute   \ run xt to set menu style
    ( loc str | )
    80 rot  ( ptr nchar loc )
    menu-print-to-screen
;
menu1-text ' menu1-style row#0  create-menu     menu1
menu2-text ' menu2-style row#24 create-menu     menu2

: menu2-show-heading ( -- ) get-heading menu2-style heading-name 2 menu2-heading-position menu-print-to-screen ;
: menu2-show-color ( color-index -- ptr nchar loc ) get-color-name menu2-color-position menu2-style menu-print-to-screen ;
: menu2-show-recording-state ( -- ptr nchar loc ) get-rec-status get-record-name menu2-recording-position menu2-rec-style menu-print-to-screen ;

: menu2-refresh ( -- )
    menu2-show-heading 
    get-pen-up-state if pen-up-colorcode else get-pen-color then menu2-show-color 
    menu2-show-recording-state
;

: paint-menus  ( -- ) menu1 menu2 ;
: paint-screen ( color -- ) lin-zero lin-end fill-screen paint-menus borders-draw ;

\ === Show/Hide Turtle
create is-visible 1 cells allot  \ turtle visibility state -- true if visible, false if hidden
: set-visible ( -- ) true is-visible ! ;
: set-hidden  ( -- ) false is-visible ! ;
: is-visible? ( -- t/f) is-visible @ ;
: turtle-hide ( x y -- x y )  restore-xy ;   \ erase current location
: turtle-show ( x y -- x y )  is-visible? if 2dup get-heading show-heading else turtle-hide then ;

\ === Movement Control
\ movement is either forward or backwards along the axis of the current heading, with screen wrap-around
\ moving forward paints, moving backward erases
\ coordinates (x,y) are semi-cartesian: over and down.  (0,0) is the top left of the user space, i.e. excluding the screen border and menus.
\ movement = 1) update coordinates to a new pixel, 2) paint or restore previous pixel, 3) new pixel is cached, 4) show turtle in new pixel.

\ screen wrap-around
: wrap-y ( y -- y* ) screen-height mod ;
: wrap-x ( x -- x* ) screen-width mod ;

\ movement along direction axes
: move-N-fwd   ( x y -- x' y' ) 1 - wrap-y ;
: move-N-back  ( x y -- x' y' ) 1 + wrap-y ;
: move-E-fwd   ( x y -- x' y' ) swap 1 + wrap-x swap ;
: move-E-back  ( x y -- x' y' ) swap 1 - wrap-x swap ;
: move-S-fwd   ( x y -- x' y' ) move-N-back ;
: move-S-back  ( x y -- x' y' ) move-N-fwd ;
: move-W-fwd   ( x y -- x' y' ) move-E-back ;
: move-W-back  ( x y -- x' y' ) move-E-fwd ;
: move-NE-fwd  ( x y -- x' y' ) move-N-fwd move-E-fwd ;
: move-NE-back ( x y -- x' y' ) move-N-back move-E-back ;
: move-SW-fwd  ( x y -- x' y' ) move-NE-back ;
: move-SW-back ( x y -- x' y' ) move-NE-fwd ;
: move-NW-fwd  ( x y -- x' y' ) move-N-fwd move-W-fwd ;
: move-NW-back ( x y -- x' y' ) move-N-back move-W-back ;
: move-SE-fwd  ( x y -- x' y' ) move-NW-back ;
: move-SE-back ( x y -- x' y' ) move-NW-fwd ;

\ stores xt for array of movements in h-index (rotation cycle) order
: create-movement-vector  
    create ( nw w sw s se e ne n <name> -- )
        , , , , , , , ,  \ store 8 xts
    does> ( x y -- x' y' )
        get-heading cells + @ execute
;

' move-NW-fwd   ' move-W-fwd    ' move-SW-fwd    ' move-S-fwd    
' move-SE-fwd    ' move-E-fwd    ' move-NE-fwd    ' move-N-fwd   
create-movement-vector   move-fwd-headings       

' move-NW-back    ' move-W-back    ' move-SW-back    ' move-S-back    
' move-SE-back    ' move-E-back    ' move-NE-back    ' move-N-back
create-movement-vector   move-back-headings

: move-fwd ( x y -- x' y' )  get-pen-up-state if restore-xy else paint-xy then  move-fwd-headings  ;
: move-back ( x y -- x' y' )  erase-xy  move-back-headings  ;  \ erases whether in pen-up mode or not
                                 
\ === Key logger / Diary function
9 constant num-diaries        \ number of distinct recordable macros
create active-diary-num 1 bytes allot  \ digit = 0 to 8
: set-active-diary-num ( digit -- ) dup active-diary-num c! update-diary-string ;
: get-active-diary-num ( -- digit ) active-diary-num c@ ;
0 set-active-diary-num  \ default diary = 0
: next-diary ( -- ) 
    get-active-diary-num 1 + num-diaries mod  ( digit )
    set-active-diary-num 
;

create preserved-diary-num 1 bytes allot  \ used to hold the original diary number so that nested macro playbacks end by showing the original diary number.
: store-active-diary-num ( -- ) get-active-diary-num preserved-diary-num c! ;
: restore-active-diary-num ( -- ) preserved-diary-num c@ set-active-diary-num ;

1000 bytes constant diary-length    \ max number of key presses (bytes) that can be recorded in a single macro
num-diaries diary-length * constant diary-length-total
create   diary   diary-length-total  allot      \ one array containing all diaries
diary diary-length-total zero-mem 

create diary-pos num-diaries cells allot  \ array holding next free place in each diary, which is the stop point for playback .
diary-pos num-diaries cells zero-mem
: get-diary-pos (  -- dentry ) get-active-diary-num cells diary-pos + @ ;

create diary-end num-diaries cells allot                       \ array holding diary ends 
diary-end num-diaries cells zero-mem
: get-diary-end ( -- dentry ) get-active-diary-num cells diary-end + @ ;
: diary-full?  ( dpos -- t/f ) get-diary-end > ;

: set-diary-end ( -- )  \ store diary end position into each cell 
    diary 1 - 
    num-diaries 0 do 
        diary-length + dup 
        i cells diary-end + ! 
    loop drop \ arithmetic increments since diaries are consecutive in memory
;
: set-diary-pos ( dentry -- )  get-active-diary-num cells diary-pos + ! ;
: cue-diary-start ( -- diary-ptr ) get-active-diary-num bytes diary-length * diary + ;  \ returns start position
: init-diary ( -- ) cue-diary-start set-diary-pos ;
: init-diaries ( -- ) 9 0 do init-diary next-diary loop ;
: rewind-diary-pos ( -- ) get-diary-pos 1 - set-diary-pos ;  \ removes last entry

create diary-recording 1 cells allot   \ state variable - true if recording
: is-recording? ( -- t/f ) diary-recording @ ;
: set-diary-recording ( -- ) true diary-recording ! ;
: clear-diary-recording ( -- ) false diary-recording ! ;
: show-recording ( -- )  rec-status-recording set-rec-status menu2-show-recording-state ;
: flash-recording ( -- )  rec-status-reminder set-rec-status menu2-show-recording-state 300 ms show-recording ;

: start-recording ( -- ) set-diary-recording       show-recording                    init-diary ;  \ record macro
: stop-recording ( -- )  clear-diary-recording     rec-status-ready  set-rec-status    menu2-show-recording-state ;
: warn-diary-full ( -- ) 
    rec-status-full set-rec-status menu2-show-recording-state
    700 ms stop-recording
;
: diary-write-entry ( asc -- )  get-diary-pos c!+ dup diary-full? if drop warn-diary-full else set-diary-pos then ;

\ === Key & Macro processing
create key-map 256 cells allot
: create-mapping  ( key-map <name> -- )
    create ,
does> ( x y asc -- x y )
    @ ( key-map ) over cells + @ ( x y asc xt ) execute
;
key-map create-mapping turtle-do
: key-process ( x y asc -- x y ) turtle-do turtle-show menu2-refresh ;

: which-diary? ( asc - d# ) asc2digit 1 - ;
: macro-playback ( x y asc -- x' y' )
    store-active-diary-num 
    which-diary? set-active-diary-num
    cue-diary-start
    begin ( x y diary-pos )   \ read from start position up to current position
        dup get-diary-pos < 
\ save-picture  \ scaffolding
    while
        c@+ ( ptr asc ) swap >R 
\            cr dup dump-key \ scaffolding
            key-process 
            R>  \ restore next pointer
    repeat
\ restore-picture  \ scaffolding
    drop
    restore-active-diary-num 
;

\ === Turtle processing commands
: log-key-if-recording ( asc -- asc ) is-recording? if dup diary-write-entry then ;
: log-key-if-not-current-diary ( asc -- asc ) dup which-diary? get-active-diary-num = if do-nothing else dup diary-write-entry flash-recording then ;
\ These commands are logged if recording is active.
: turtle-move ( x y asc -- x' y' )
    log-key-if-recording ( asc -- asc )
    fwd-key = if move-fwd else move-back then
    2dup cache-pixel 2dup set-location
    set-visible
;
: heading-change ( asc -- )
    log-key-if-recording ( asc -- asc )
    cw-key = if 1 else -1 then adjust-heading
    set-visible
;
: pen-toggle ( x y asc -- x y ) log-key-if-recording  drop get-pen-up-state if do-pen-down else do-pen-up then ;
: color-change ( x y asc -- x y ) log-key-if-recording   get-pen-up-state if do-pen-down drop else turtle-color then ;
: play-macro ( x y asc -- x y )  is-recording? if log-key-if-not-current-diary drop else macro-playback then ; \ if recording, log macro & don't play; else play.  Won't log current active diary (no recursion).

\ These commands are not logged.
: turtle-toggle ( x y asc -- x y ) drop  is-visible? if set-hidden else set-visible then ; \ toggle turtle visibility
: turtle-home ( -- )
    get-pen-up-state if else do-pen-up then 
    turtle-hide                          \ don't leave mark in last position 
    2drop init-start-pen-position init-heading 
    2dup cache-pixel                     \ don't clobber home position
;
: go-home ( x y asc -- x y ) drop turtle-home ;  \ reset turtle to home position

: diary-toggle ( x y asc -- x y ) drop is-recording? if do-nothing else next-diary then ;
: recording-toggle ( x y asc -- x y )  drop is-recording? if stop-recording else start-recording then ;
: help-show ( -- )  save-picture  black-screen welcome pause-key  restore-picture ;
: help-screen ( x y asc -- x y ) drop help-show ;
: carriage-return ( -- ) save-picture cr restore-picture ; \ ensure cursor is at left
: prompt-clear-screen ( -- ) 
    carriage-return
    ." Clear screen? (Press ESC to confirm, or any key to continue drawing.)" 
    exit? 
    if is-recording? if stop-recording then 
       get-screen-color cr   paint-screen   set-visible
    else   cr restore-picture
    then
;
: clear-screen ( x y asc -- x y ) drop prompt-clear-screen ;
: confirm-quit ( -- t/f )
    carriage-return
    ." Are you sure? (Press 'q' to confirm, or any key to continue.)"  
    quit? 
    if true else  cr restore-picture false then
;
: is-quit? ( asc -- asc t/f ) 
    quit-key over = if confirm-quit else false then 
;

\ === Initialization code
: turtle-init ( -- x y )
    set-video-mode
    set-lin-offset
    set-fgbg-palette  attach-colors  \ assign optimized cursor/background colors and create strings
    0 set-active-diary-num   init-diaries  set-diary-end  
    attach-recording-strings   stop-recording
    init-heading   init-start-pen-position ( -- x y ) 
    clear-pen-up-state          \ default = pen-down
    red set-pen-color           \ default pen & bg color
    white dup set-screen-color  \ default screen color 
    silver set-border-color 
    cr 
    paint-screen menu2-refresh
    2dup cache-pixel
    set-visible turtle-show
;

\ === Adding key mappings
: add-key ( xt key -- ) cells key-map + ! ;
\ all non-mapped keys must be ignored.
: ignore-input  ( asc -- ) drop ;
' ignore-input  constant xt-ignore-input 
: init-key-map ( -- ) 255 0 do xt-ignore-input i add-key loop ;  
init-key-map
    ' turtle-move       fwd-key              add-key
    ' turtle-move       erase-key            add-key
    ' heading-change    left-key             add-key
    ' heading-change    right-key            add-key
    ' pen-toggle        pen-updown-key       add-key
    ' color-change      color-fwd-key        add-key
    ' color-change      color-back-key       add-key
    ' turtle-toggle     show-hide-key        add-key
    ' go-home           home-key             add-key
    ' diary-toggle      diary-key            add-key
    ' recording-toggle  record-key           add-key
    ' play-macro        play-macro-1-key     add-key
    ' play-macro        '2'                  add-key
    ' play-macro        '3'                  add-key
    ' play-macro        '4'                  add-key
    ' play-macro        '5'                  add-key
    ' play-macro        '6'                  add-key
    ' play-macro        '7'                  add-key
    ' play-macro        '8'                  add-key
    ' play-macro        '9'                  add-key
    ' clear-screen      clear-screen-key     add-key
    ' help-screen       help-key            add-key

: q ( -- ) 
    is-recording? if stop-recording then
        cr ." Type 'exit' to return to Windows." 
        cr ." (To restart, launch c:\\totalcmd\\turtle-logo\\_start_turtle-logo.bat)" cr
        bye 
;
\ === Main program
: turtle-go ( -- x y )
    help-show
    turtle-init
    begin ( x y )  \ key processing loop
        key ( x y asc )
        is-quit? if drop true else key-process false then  
    until ( x y )
    turtle-hide
    is-recording? if stop-recording then
    q
;
: turtle-logo ( -- ) turtle-go ;   \ alternative

\ ================================================ END OF MAIN PROGRAM
\ ================================================ TESTING - SUPPLEMENTARY CODES
\ === instructions
: kp ( x y asc -- x y ) key-process  ; 
: .. ( -- ) pause-quit ;
: pen-adjust ( -- ) pen-updown-key kp ;
: recording-adjust ( -- ) record-key kp ;
: diary-adjust ( -- ) diary-key kp ;
: turtle-adjust ( -- ) show-hide-key kp ;
: go-home ( -- ) home-key kp ;
: next-color ( -- ) color-fwd-key kp ;
: prev-color ( -- ) color-back-key kp ;

\ === macros
: left-45 ( -- ) left-key kp ;
: left-90 ( -- ) 2 0 do left-45 loop ;
: left-180 ( -- ) 4 0 do left-45 loop ;
: right-45 ( -- ) right-key kp ;
: right-90 ( -- ) 2 0 do right-45 loop ;
: right-180 ( -- ) 4 0 do right-45 loop ;
: fwd-n ( n -- ) 0 do fwd-key kp loop ;
: erase-n ( n -- ) 0 do erase-key kp loop ;

: draw-half-square ( -- )  2 fwd-n   right-90  5 fwd-n  right-90 ;
: house-square ( -- ) draw-half-square draw-half-square ;
: house-roof ( -- ) 3 fwd-n right-45 2 fwd-n right-45 1 fwd-n right-45 2 fwd-n right-45 1 fwd-n ;
: house-complete ( -- ) pen-adjust 2 fwd-n left-90 3 fwd-n left-90 pen-adjust ;
: draw-house ( -- ) house-square house-roof house-complete ;
: draw-spiral ( -- ) 
    4 fwd-n   right-90  8 fwd-n  right-90
    3 fwd-n   right-90  1 fwd-n  color-fwd-key kp  5 fwd-n  right-90
;

\ === test harnesses
: test-show-color-names ( -- ) 
    attach-colors
    color-names 4 strlen-color * ( array nchar )
    4 0 do 
        2dup + -rot tuck ( array' nchar array nchar ) 
        type cr pq 
    loop
    2drop 
;

: test-poke-pixel ( -- )
    row#3 $5F 'S' poke-pixel  \ interactive visible start point
    lin-end $8F 'E' poke-pixel      \ end
    3 1 xy2lin $A0 'A' poke-pixel   
    68 12 xy2lin $C0 'B' poke-pixel
    69 13 xy2lin $40 'C' poke-pixel
;

: test-probe-diary  ( -- )
    0 set-active-diary-num
    init-diaries  set-diary-end
    ." diary # " get-active-diary-num . 
    ." diary-pos: "  get-diary-pos u. 
    ." diary-end: "  get-diary-end u. 
    cr
;

: test-diaries ( -- )  
    1 fwd-n '1' kp ..   \ checking that no playbacks occur, no errors occur, but diary changes with each keypress
    1 fwd-n '2' kp ..
    1 fwd-n '3' kp ..
    1 fwd-n '4' kp ..
    1 fwd-n '5' kp ..
    1 fwd-n '6' kp ..
    1 fwd-n '7' kp ..
    1 fwd-n '8' kp ..
    1 fwd-n '9' kp ..
    right-90
    1 fwd-n '1' kp ..    
;
    
\ TO TEST fullness for diaries, set diaries from 1000 to 15.  Remember to set the diary length back to 1000 chars.
: test-diary-fullness-1-channel ( -- )  \ check that all 9 diaries work correctly.
    record-key kp ..
    8 fwd-n right-90 ..  \ 10 moves
    1 fwd-n right-90 next-color ..  \ 14 moves 
    right-45 .. \ 15th move triggers fullness, does not record, but executes
    left-45 ..  \ corrects 
;
    
: test-diary-fullness ( -- )  \ recording, fullness, and playback.
    turtle-init
    test-diaries
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    test-diary-fullness-1-channel diary-key kp ..
    '1' kp ..
    '2' kp ..
    '3' kp ..
    '4' kp ..
    '5' kp ..
    '6' kp ..
    '7' kp ..
    '8' kp ..
    '9' kp ..
;    

: test-multi-diary-recording ( -- )  \ draws a house all together, then in recordable bits, then by playing the recorded bits, then stitching the recorded bits into one recording
    turtle-init
    draw-house ..
    recording-adjust house-square .. recording-adjust .. diary-adjust ..
    recording-adjust house-roof .. recording-adjust .. diary-adjust ..
    recording-adjust house-complete .. recording-adjust .. diary-adjust ..
\    '1' kp ..
\    '2' kp ..
\    '3' kp ..
    recording-adjust '1' kp .. '2' kp .. '3' kp .. recording-adjust .. diary-adjust ..
    '4' kp ..
;    

    
: test-erase ( -- )
    turtle-init
    right-45    3 fwd-n   3 0 do next-color loop   3 fwd-n   3 0 do next-color loop   3 fwd-n  ..
    right-180 ..
    pen-adjust ..
    7 fwd-n ..
    5 erase-n ..  \ bug!
    right-90 .. 
    1 erase-n .. 
    right-90 .. 
    1 fwd-n ..  \ bug!
;

: test-home-key ( -- )
    turtle-init
    draw-spiral draw-spiral draw-spiral ..
    go-home ..
    6 fwd-n pen-adjust ..
    draw-spiral ..
    go-home ..
    right-45 6 fwd-n ..
    go-home ..
    right-45 8 fwd-n ..
    pen-adjust ..
    go-home ..
    right-45 14 fwd-n ..
    go-home ..
    right-45 14 fwd-n pen-adjust ..
    go-home ..
;

: turtle-test ( -- x y ) 
    turtle-init ( -- x y )
\ testing fwd,right,left
    fwd-key kp right-45  2 fwd-n left-90 2 fwd-n ..   
\ testing erase
    right-45 2 fwd-n .. 
    2 erase-n ..  
\ testing colorfwd/colorback
    3 0 do next-color loop 2 fwd-n ..
    2 0 do prev-color loop 2 fwd-n ..
\ testing pen-up/pen-down
    right-90 8 fwd-n ..
    left-180 pen-adjust .. 
    4 fwd-n ..
    pen-adjust ..
    2 0 do next-color loop ..
    2 fwd-n ..
\ testing home
    go-home ..
    left-90 4 fwd-n ..
\ testing macro-key before any record
    5 0 do next-color loop .. 
    play-macro-1-key kp ..  \ nothing bad should happen
    next-color ..
\ testing recording and pressing macro-key while recording
    recording-adjust ..
    play-macro-1-key kp ..  \ recording notification should flash
    next-color .. 
    1 fwd-n .. 1 fwd-n .. 1 fwd-n .. right-90 .. \ one leg of square
    recording-adjust ..
\ testing macro replay
    play-macro-1-key kp ..
    play-macro-1-key kp ..
    play-macro-1-key kp ..
\ testing clear-screen
    clear-screen-key kp  .. \ press yes
    left-180
    play-macro-1-key kp ..
    play-macro-1-key kp ..
    play-macro-1-key kp ..    
    clear-screen-key kp  .. \ press no
\ testing help
    left-90 left-key kp
    play-macro-1-key kp ..
    play-macro-1-key kp ..
    play-macro-1-key kp ..
    play-macro-1-key kp ..
    help-key kp ..
\ testing hide and the various keys that force visibility
    left-90 7 fwd-n ..
    turtle-adjust ..
    next-color ..
    turtle-adjust ..
    1 fwd-n ..
    turtle-adjust ..    
    right-45 .. 
    turtle-adjust .. 
    go-home ..
    turtle-adjust ..
\ test quit
    quit-key kp
;

: fill-screen-test ( -- )
  16 0 do
    i lin-zero lin-end fill-screen pause-quit
  loop
;
4 constant thickness
: color-bg-loc ( lin -- color )  \ pick background color based on location
    lin2xy drop ( j )
    thickness / 16 /mod drop ( color )  \ cycle through 16 colors
;
: color-demo ( -- )  
    set-video-mode
    get-lin-offset
    begin ( loc )
        dup lin-end < 
    while ( loc )
        dup BL over color-bg-loc dup set-char
        2 +  \ only even cells are visible in this graphics mode.  Odd cells go green
    repeat
    drop
    pause-quit
;

: fgbg-demo ( -- )
    set-video-mode
    set-fgbg-palette
    get-lin-offset
    begin ( loc )
        dup lin-end <
    while ( loc)
        dup 0 heading-symbol over color-bg-loc dup get-optim-bg-color set-char
        2 + 
    repeat
    drop
    pause-quit
;

\ =================================================================
\ TODO:


\ DONE: allow recording multiple macros (diaries).  Toggle diaries using D -- later D prompts to enter the diary number.
\ DONE: allow nested recording of macros (i.e. record macro numbers so macros can be nested within other macros and all played back -- BUGGY - need to maintain a recursion stack for reading the macros back )
\ FIXED: cannot do recursion (i.e. cannot record macro calling itself)

\ Article: Modularity - multiple diaries teaching modularity, which is the heart of programming design, structure
\ Feature: Loop - 'L' then it repeats the next macro asking: Another? until you say no.  Teaches looping.
\ Feature: Consider: support recursive macros (calling themselves?)

\ Feature: Add save and restore keys -- maybe make automatic save just before playing a macro.  So can undo if press the wrong one.
\ Feature: save and then restore, i.e. in case of a catastrophic error!  write to file.
\ Feature: how to prevent accidentaly pressing record and overwriting previously recorded macro?

\ Feature: save macros to file for reloading later (in case program quits).  Allows user to load previous creations, e.g. trees, flowers, birds, sun, house.  
\       Also allows creations to be shared and a community of users to be formed.  
\       Status bar prompts for save to file, load from file, etc.
\ Feature: save screen to disk, load from disk

\ Feature: 't' to switch to text mode, to allow adding text in direction of cursor.  Possibly an auto-date feature when saving the picture.
\ Feature: allow full-screen view - without menus (just the turtle) - or just taking the picture without the menus.
\ Feature: display numbered grid (numbered rows and columns according to elementary math convention: H is the origin (0,0) then positive to right and up)
\ Feature: show coordinate location (trouble shoot with .s on status bar)
\ Feature: code in banner letters (A, B, C, etc.) when in banner text mode ('b')
\ Port to Android! (keys will make it harder)  Maybe bluetooth keyboard.
\ Feature: Port to the Raspberry Pi.
\ Port to 640x380 graphics mode screen instead of text mode
\ Port to windows via Win32 Forth
\ Feature: Add ability to animate, e.g. the splash screen images
\ Installer: Can DOS Box start up with the splash screen?
\ Installer: Tell the user where the installation is going
\ Installer: test the desktop shortcut placement, and the start-menu placement.
\ Application: barack obama ascii art portrait.
\ Feature: Re-mapping the keys to use mnemonics from a non-English language, or extend to create a multi-lingual learning environment
\ Feature: read in a png and essentially digitize that using turtle (as a way to load files)
\ Feature: Add frieze group capabilities (equipping turtle with dance moves), i.e. 
    \ it can do a move (regular playback)
    \ it can do a glide reflection (move plus reflection vertical, i.e. replacement of turn left with turn right and turn right with turn left), 
    \ it can do a spinning hop (move plus pen-up, 180-degree turn, plus pen-down, plus move).
  \ This allows making complex frieze patterns of any kind.  
  \ This is where being able to execute two recordings is handy.  One recording allows for single-generated patterns.  Two recordings allows for double generated patterns.
\ Feature: voice control of the turtle
\ Feature: built in exercises -- shows screen, then asks child to replicate it...

\ BUG: > pen-up > move to middle of screen > clear screen > home > should not leave a red dot
