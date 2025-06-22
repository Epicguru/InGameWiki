# Overview

The in-game wiki mod will automatically generate a page for each items you mod adds.
However, you will probably want to add additional information about certain items or buildings.

To do this, go into your Wiki/English folder and create a new file called `Thing_YourDefName.txt` where `YourDefName` is the name of the def of the item you want to edit.

So, for example, if you have a gun def called `LaserGun` then you would create the file `Thing_LaserGun.txt`.

Inside the file, you need to specify tags and content just like with external files. If you have not read the external files documentation yet, read it now.

The format for editing existing pages is identical to external pages, except that certain tags are disabled, such as Title. You do not need to specify a page ID.

# Example complete file
Assuming you have a def called `LaserGun`, here is the file `Thing_LaserGun.txt`:

```
Background: UI/Backgrounds/LaserGunBackground

ENDTAGS

#!This is the laser gun!#

#I am some text about the laser gun...
Here is a picture of the laser gun:#

$UI/Icons/LaserGunIcon$
```
