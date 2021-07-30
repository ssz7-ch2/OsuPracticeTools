## _Place your custom scripts in scripts.txt file_ ##
You can place them in defaultscripts.txt but they will be overwritten if you redownload the rar and replace all the files.

# Script Format #

`hotkey: command -option value`

Each hotkey can have multiple commands. Separate commands with `|` or add a new line with the same key e.g.  
`0: create -r 1.1`  
`0: create -r 1.2`  

This will run both `create -r 1.1` and `create -r 1.2` when alt + 0 is pressed

## Hotkey Format ##
`modifierKey + key/key`

You can use any keys from [here](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.keys?view=net-5.0) except for control, alt, and shift keys  
e.g. `NumPad1: create -r 1.1`

To use multiple keys, separate each key with `/`  
e.g. `A/1: createmaps -r 1.1`  
_Note: the order does matter_ `A/1` != `1/A`

You can also add c or s before key to require ctrl or shift key to also be pressed down in addition to alt  
e.g. `s + S: creatediffs -spinner`


## Command Format ##

There are currently 10 commands:

Command    |Decription
-----------|-
create     |creates a map
add        |adds a map (to list stored in memory)
del        |removes current map in osu from map list
clearmaps  |clears maps
createmaps |creates maps
adddiff    |adds a practice diff (to list stored in memory)
enddiff    |sets the end time for the last added practice diff
deldiff    |remove last added practice diff
cleardiffs |clears practice diffs
creatediffs|creates practice diffs

__WARNING:__  
List of maps won't be cleared until clearmaps is called or if `addmap` hasn't been called for over ~10 minutes  
List of practice diffs will be cleared when map is changed or if `add` hasn't been called for over ~10 minutes


## Option & Value Format ##

A dash `-` is required before the option  
There must be a space  between option and value

_only create commands have options_ (`create`, `creatediffs`, `createmaps`)

Option |Description
-------|-
r      | _(decimal) between 0.1 and 5_<br />changes the speed/rate of a map
bpm    | _(decimal) bpm / original bpm is between 0.1 and 5_<br />changes the speed/rate of a map to specified bpm<br />overrides value from r
pitch  | changes pitch with rate (nightcore / daycore)
hr     | applies hr to map
flip   | `h`<br />flips map vertically, horizontally if h is included
rs     | removes spinners
cs     | _(decimal) between 0 and 10_<br />changes circle size
ar     | _(decimal) between 0 and 10_<br />changes approach rate
od     | _(decimal) between 0 and 10_<br />changes overall difficulty
hp     | _(decimal) between 0 and 10_<br />changes hp drain rate
f      | _(text) text must be in quotes_ `"` _or_ `'`<br />changes the name format (more info in [Name Format](#name-format) section)

_Note:_  
max and min can be added before cs, ar, or od to specifiy min and max limit  
e.g. `-maxcs 4` (if cs is > 4, changes cs to 4)

HR is applied first, then rate change, then cs/ar/od/hp modifications

__Options specific to `creatediffs`__

Option |Description
-------|-
i      | _(decimal) in seconds<br />creates diffs at intervals of specified value
order  | `time` _or_ `reverse`<br />specifies the index order, default is add order, time for order by time, reverse for order by time reversed
next   | specifies when practice diff ends, default is map end, next is next diff
spinner| use spinners for generating combo
slider | _(integer) duration of slider_<br />use slider for generating combo
gap    | _(integer)_<br />the duration between generated combo and start of practice diff


## NAME FORMAT ##

You can place keywords in brackets to get info like cs or bpm  
e.g. `{CS}`  
Keywords are case sensitive. Capitalized keywords will automatically add a space  
e.g. `a word{CS}` -> `a word CS3.5`

Keyword|Description
-------|-
v      | map difficulty name/version
l      | length of map in mm:ss format
mc     | max combo of map
R      | speed/rate
BPM    | bpm of map
CS     | circle size
AR     | approach rate
OD     | overall difficulty
HP     | hp drain rate

__Specific to `creatediffs`__  

Keyword|Description
-------|-
i      | index of diff in the list
n      | total diffs
s      | start time in mm:ss format
e      | end time in mm:ss format
c      | amount of combo generated
sc     | combo at start of diff
ec     | combo at end of diff