# Converter for converting 0.5.1.X Usermaps to 0.6 (WIP)
This is a new fork trying to make that old code actually work.

The current state is: Some maps work, some don't. Some crash on load, others crash on starting a new round. Some are missing items. Others work pretty ok.

Here's a rundown of changes to the original code made so far (details follow after):
- Used tag_list.csv from 0.5.1.1 and 0.6 to generate a map.csv that maps old tag IDs to new ones by correlating the second column (which in the case of 0.6 is the name). This was done with PHP code, which is included under php/mapforgeitems
- Loaded this csv with some code from https://github.com/ElDewrito/ForgeMapFixer into a Dictionary and made the code update old tags where necessary
- Added a scrollable TextBlock element for debugging output, helpful for development
- Updated the Canvas folder with new maps from 0.6 that had everything (including emergency spawns) stripped from them, leaving only a map options with disabled barriers and a single spawn
- Added a function that carries the map options over to the new map to disable barriers, to make possible loading of maps that have been created out-of-bounds. You might get locked out otherwise.
- Changed default values for the Budget entries that get created, which fixed some issues with objects not being grabbable. For newly created budget entries, the total object count gets updated with each element thats copied to the new map.
- Added serveral outputs to the debugging TextBox.

Here's some more details:

## map.csv
Okay so, 0.6 includes a tag_list.csv, which correlates tag ids (4-digit hex codes) with some kind of object names. This looks like this:
` 0x00002E90,objects\multi\spawning\respawn_point `

This file is included here under php/mapforgeitems/0.6.csv

I was given a similar csv file for 0.5.1.1, included here in the same folder as the other one as 0.5.1.1.csv.

What I did in PHP was to correlate the second column to create a map from old tag ids to new ones. You can read the exact code yourself, it's not too complicated. 

I generated a resulting map.csv, which looks like this:
``` 0001,
0002,
0003,
0004,
0005,
0006,
0007,
0008,
0009,
000A,
000B,27C7
000C,
000D,
000E,
000F,
0010,
0011,
0012,
```
To the left is the old tag ID. To the right is the new tag ID. If no correlation was found  (read: no tag id with the same named object existed in 0.6), then the right side was left blank. If a correlation was found and the tag id was **not** the same as in 0.5.1.1, the right side contains the new tag ID. 

If both tag IDs are identical, it is not listed in this file.

So what practically happens is, the converter checks each old tag id against this list. 
- If the right side is blank, the object is simply ditched.
- If the right side has a value, then the tag ID of the object is updated to that value.
- If the tag id is not found in the list, then the object is copied over with the original tag ID.

The converter does inform you about whenever an object is ditched or when a tag is updated, in the new TextBox for debugging. Simply to make it easier to find errors etc. I imagine this can be ditched in the final versions once everything works smooth.

### The problem with the map.csv
The problem is, the automatic generation of that list is not perfect. E.g. I've been told some objects have kept the same tag ID from 0.5.1.1 to 0.6, but they were named (while they weren't named before).

Also, the 0.6 list does not seem to contain unnamed objects at all, which led the auto-generator to conclude the object doesn't exist in 0.6, while it very well may.

**For example**:
`` 0x0000444C,0x444C``

This line is from the 0.5.1.1 csv, but the 4444C hex code does not appear in the 0.6 csv. Yet I've been told that this object still exists. (have not personally verified)

**Second example**:

`` 0x00004DF8,objects\levels\dlc\bunkerworld\drum_55gal_bunker\drum_55gal_bunker ``
This is the line from the 0.6 csv.

`` 0x00004DF8,0x4DF8 ``
And this is the line from the 0.5.1.1 csv.

Again I've been told the tag index stayed the same, yet now it is named. But my auto-generator cannot automatedly recognize that.

**Third example**:
```0x00004E20,objects\levels\dlc\warehouse\bridge\bridge 0x00004E21,objects\levels\dlc\warehouse\bridge\bridge 0x00004E22,objects\levels\dlc\warehouse\bridge\bridge```

As you can surely tell, all the names are identical, yet there may be differences between the objects. It's not a problemwhen the tag hasn't changed (I just use the same one then), but when it *has* changed, how to tell which one is the new one?

### The challenge regarding the map.csv
Obviously, some amount of handiwork will have to be invested to find and fix errors in the map.csv. Until then, some objects just won't transfer properly. And I suspect that problems with this map.csv may also be responsible for some converted maps crashing.

## Updated canvas maps

This converter uses .map files in the *Canvas* folder as a kind of template for converting older maps. Basically, those are *sandbox.map* files that actually work in 0.6. They are prefixed with a map ID which correlates to the base map.

Here's a list:

   mapID   |  Base map   | 
 --- | --- |
   30   |   Last Resort | 
   31   |   Icebox| 
   310   |   High Ground| 
   320  |   Guardian| 
   340   |   Valhalla | 
   380   |   Narrows | 
   390   |   The Pit| 
   400   |   Sandtrap | 
   410  |   Standoff | 
   705   |   Diamondback| 
   
I took a pack of clean forge maps from dewritohub that had everything removed from them, save for map options with unlocked barriers and a single spawn. Replaced the old ones with these. It *may* or *may not* work better with maps that just had the canvas-command run without invisible spawns removed, for whatever reason. You can try that if you want, I haven't done so.

## Transfer of map options with unlocked barriers
I added a loop that basically does nothing more than search for the map options element (hex tag index 5728) in the maps in the Canvas folder and copies that one over to the target map, so that all barriers in the converted map are disabled by default. It does this by saving the first empty placement index in a variable and then just putting it in there.

## Budget entries
A sandbox.map file (simplistically speaking) works like this: You have an array of placements, and you have an array of budget entries.

There are 640 placements and 256 budget entries. This is fixed, the numbers cannot be bigger. That's why all sandbox.map files are identical in size.

A **placement** is an actual object placed on the map. It contains various information like the position, respawn time, team association, rotation, EngineFlags and PlacementFlags (I don't know much about those, but they seem to be bitfields) and apparently some so-far unknown values as well.

Meanwhile, a **budget entry** contains information about the actual object type, like the cost, minimum and maximum amount  that can exist of that object, the current amount of that object on the map, but *most importantly*, the **tag index**.

Every **placement** references a **budget entry**. Without a budget entry, you basically would have no idea what kind of object has actually been placed.

For example, this means that you can't have more than 256 different types of objects on your map, as each separate type of object requires its own budget entry. On the other hand, 100 identical placements of a crate would only require a single budget entry of that crate.

Okay, so, what this converter does in a nutshell is, it copies over all **budget entries** from the corresponding map in the *Canvas* folder to the new map, then searches those budget entries for the tag IDs of the **placements** that are to be copied. When found, the index of that budget entry is set for the to-be-copied placement and it is added to the new placements.

When no budget entry with that tag ID can be found, a new one is created. The code for this looks like this:
```
var entry = new BudgetEntry()
 {
     TagIndex = tagIndex,
     Cost = 1,
     RuntimeMin = 0,
     RuntimeMax = 255,
     CountOnMap = 1,
     DesignTimeMax = 255
 }; 
 ```

Now, in the original version of this converter, the cost was set to -1, and all other values except tagIndex to 0. I don't know why. This seems to work better. Setting it to -1 seems to have caused some issues in the converted map where new objects couldn't be spawned, the current object count was negative and objects could not be grabbed. 

Don't ask me why it was that way, but now it works better for the maps I tested. Maybe there was a hidden purpose to it that will crystallize later.

### Updating the count of the object in the budget entry
Currently, due to how the original code is written, only additional budget entries are actually written to the file. The ones that existed before are not updated, just copied over. In other words, it's not currently possible to update the count of objects for which the budget entry already exists in the map in the *Canvas* folder. 

I have modified the code to do it for all other objects though.

I am not sure whether this has any real impact (other than informative). It seems easy enough to modify the code to rewrite all the budget entries (like it does with the placements), but again, maybe there is a purpose to it. Will find out eventually.

Also, it may (or may not) be necessary to find the correct values to set for each tag index regarding the maximum amount of it on the map, and the runtime limit as well. I am not sure if this is of any importance. It may not be.

## Scenario objects

Okay, this is just from my limited understanding. When you opened a base map in Forge in 0.5.1.1, then all objects that were on it by default, were called **scenario objects**. Basically, they are the default stock objects on each map, like warthogs, spawns, etc.

In 0.6 Forge, you can delete those and thus gain more budget for your own items. 

In 0.5.1.1, apparently, deleting scenario objects had no influence on how many you could place yourself.

Partially due to the way this Converter is written and partially due to me replacing the maps in the *Canvas* folder with almost complete blanks, scenario objects are not currently ported. So any map that was an extension of a base map will come out incomplete. 

To go into more detail, the converter uses two separate loops at the moment, one for scenario objects (unable to create new budget entries) and a second one for all other objects (able to create new budget entries). 

Since it cannot create new budget entries, and since the maps in the *Canvas* folder likely lack the budget entries for the default objects, they just get discarded.

I think it may be possible to merge both loops into one, due to 0.6 Forge apparently treating all objects as equal to each other. I guess I will just have to try that, I simply don't know at this point. 

For now, **don't be surprised** if default map elements (for example spawn points and vehicles) from the **base map** (for example Standoff or Valhalla) are not converted.  This should not affect spawn points placed by the map creator!

## Examples of conversions so far & current state of development

To give some hope, here's screenshots taken in ElDewrito 0.6 of 0.5.1.1 maps that were converted with this converter:
![PacMaze Map](https://i.imgur.com/Calhu1n.jpg)    |    ![Donkey Kong Map Picture 1](https://i.imgur.com/64KQki8.jpg) |
--- | ---  
![Donkey Kong Map Picture 2](https://i.imgur.com/CWoR7Jh.jpg)    |    ![Some kind of Get out of my house map that we weirdly couldnt find again after converting](https://i.imgur.com/P26qSNe.jpg)

### What works
- Some maps can be converted and loaded in ElDewrito 0.6 Forge, with only a few discarded objects. Objects are (thanks to updated default budget entries) grabbable and editable and new objects can be spawned.
- Those maps also seem to be able to be loaded in Multiplayer, but too few tests have been done to see how stable. Since this is still a WIP, assume that they arent completely stable yet. If you need the map urgently, I *for now* suggest trying this converter and then making a prefab out of everything that came out right, and inserting that prefab into a fresh map and going from there, just to be safe.

### What doesn't work
- Some maps crash upon loading, reason is unknown
- Some maps crash upon starting a new round in Forge, reason is unknown
- Some object types (tag ids) don't transfer. Maps that use a lot of such objects will come out pretty useless. **This should be fixable with a moderated map.csv.**

### What may or may not work (not really tested)
- Timers on maps seem to somewhat work, but further tests are needed to see how reliably

## Possible further complications / challenges
From what I hear, 0.5.1.1 had a few mods available for Forge that extended its capabilities, and also mods for the general game that seemed to have included tag changes and been relatively popular.

I can imagine that depending on whether a map was making use of those mods, the conversion process may have to be varied. For example, tag changes in those mods may require a different map.csv (currently that requires a recompile).

Maybe this can be automated. I don't know.
