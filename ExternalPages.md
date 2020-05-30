# What are external pages.
External pages are unique wiki pages that do not depend on or link to a particular item in the game. For example, a 'getting started' page would be an external page.

# Creating external pages.
In your mod folder, create a new folder called `Wiki`, and inside that folder another one called `English`, so that you end up with this:
```
YourModName/
   About/
   Textures/
   Defs/
   ...
   Wiki/
       English/
       SomeOtherLanguage/
```
This gives support for multiple languages.
To create a new external wiki page, simply create a new .txt file inside the English folder. You can name it whatever you like.
This page will now be loaded automatically.

# Understanding tags
There are two parts to an external page file: the tags and the content. Here I will explain the tags.
The tags give information and properties to the page, such as it's ID, title, background, icon etc.

All external pages must have at least an ID, and ideally a title.

# Tag format
Tags are written at the beginning of the txt file, each one on it's own line.

The ID tag would be written like this:

`ID: MyPageID`

The title tag would look like this:

`Title: My page title`

Here is a list of all the tags and their function:
- **ID**
  - Specifies a unique ID to the page. Must be unique between all loaded mods. Should not be translated.
- **Title**
  - Specifies page title, as seen in-game. Should be translated.
- **Icon**
  - Specifies the icon to be used, such as 'UI/Icons/MyIcon', just like within defs.
- **Background**
  - Specifies the background image to be used, such as 'UI/Background/MyBG', just like within defs.
- **RequiredResearch**
  - Specifies a ResearchDef that is required to be completed for this page to not be considered a spoiler. For example `Microelectronics`.
- **Description**
  - Provides a page description, that will be seen in-game. Should be translated.
- **AlwaysSpoiler**
  - Indicates that this page is always considered a spoiler, so will be hidden by default.

All tags apart from ID are optional.

Once you have written all the tags you want, you must write `ENDTAGS` on a new line. This tells the wiki that you have finished writing tags and have started the content section.

# Page content
Now that the tags are done, you can begin filling in the page content.

The page parser works on a 'block based' system, where each element is surrounded by a particular set of characters.

For example, to add a simple line of text into the wiki page, you would write this:

```
#I am a new line#
```
Here the _#_ symbol tells the wiki that the content within is text.

Here are the other available symbols and what they do:

- **$**
  - Puts an image into the wiki page. Content must be an image path. For example: `$Path/To/My/Image$`. You can also optionally specify a fixed image size, like so: `$Path/To/My/Image:64,64$` would make the image display at 64x64 pixels.
- **@**
  - Adds a link to a ThingDef. This link will automatically open the corresponding wiki page, or the default inspector if the page does not exist (such as for vanilla items). For example: `@MyGunDef@` would add a link to the gun that your mod added.
- **~**
  - Adds a link to an external page. The content should be the external page ID. For example: `~MyPageID~` adds a link to the page with id *MyPageID*.
  
 You can escape any of these characters by putting a backwards slash before it, like so: `\#`.
 
 You **cannot** put tags inside of an existing block. For example,
 
 ```#This is some text @DefName@ more text#```
 
 is invalid because the link cannot be placed inside the text block. It must be placed before or after.
 
 # Example complete file
 Here is an example of a complete file, MyPage.txt:
 ```
 ID: MyPageID
 Title: Example Page
 Icon: Icons/MyIcon
 Description: This is an example page.
 
 ENDTAGS
 
 #This is an example page!#
 
 #Here is some text,
 and here is a new line.
 Take a look at this image:#
 
 $Images/MyImage$
 
 #Check out this item:#
 
 @MyCoolItemDef@
 
 #Now read this other page:#
 
 ~SomeOtherPageID~
 ```
