CSCRIBE
=======
*Templating software* - v0.2
*NOTE - As of 0.2, exporting images and printing are temporarily disabled.*
I know, I know, those are the two main functions of the program. WIP software, y'all.

WHAT IS IT?
-----------
CScribe is software that transforms spreadsheets of data into images or printable files. Its intended use is to create images for playtesting and producing card games, though it could probably be used for other templating purposes as well.

HOW DOES IT WORK?
-----------------
The templating process involves 3 basic steps:
* Importing data from a properly formated Excel (2007+) or Google Spreadsheet.
* Optionally transforming that data into something more useful with a python script.
* Loading a .xaml template that describes the visual look and feel of each row of data.

IMPORTING DATA
--------------
Successfully importing data requires the source spreadsheet to be formatted properly.

Each tab in the source spreadsheet is considered a different card _type_. Each type may have its own layout and script.

The top row of each tab is assumed to be header data, and should contain a useful name for each column, as the names used here will be referenced in your .xaml files.

Every tab is assumed to have a "Name" and "Count" column. These will be used by the program to differentiate the cards in the card list, and to know how many copies of each card to print.

TRANSFORMING DATA WITH PYTHON
-----------------------------
After importing your data, you can transform that data with a python script. This can be useful to add additional derived data from a card. For example, say your game requires the background image of each card to be based on its casting cost, as in Magic: the Gathering. You could add an additional column to your spreadsheet that specifies the correct background image for each card, or you could write a python script that infers the correct background image for you, and adds that information to each card. This keeps your spreadsheet clean of redundant data, and provides extra flexibility to your .xaml layouts.

To add a python script to a tab, simply click the "Browse..." button near the Script text box on the left of the screen after importing data. This will pop up a dialog allowing you to select the script to run.

HOW PYTHON WORKS
----------------
Python scripts should define a "Transform" function that takes (card, options) arguments. This function is run once for each card. The card object passed to this function can be manipulated directly, and new or changed attributes will be accessible in your layouts.

Python scripts may also define an "Options" dict. These options will be presented to the user, and will be accessible in the "options" argument passed to the transform function. They are intended to allow template designers to ask the user for information that is unique to each user, for example, the location of card image files on their hard drive. The "Options" dict is made up of option name -> option type pairs. The available option types are "file", "dir", and "string". The option type simply determines whether an option has an accompanying "Browse" button, and whether that button allows the user to pick files or folders.

XAML LAYOUTS
------------
The actual hard part of laying out what data goes where on a card is done by .xaml, an xml based language created by Microsoft to make Windows applications. This means that your layout files have access to the same powerful array of layout controls that every Windows program does, and can use pre-existing tools like Visual Studio or XamlPad to create those layouts. I've been having luck with Kaxaml.

How to actually write a .xaml file is a large and incredibly complicated topic (as I found out actually writing the UI for this program), and outside the scope of this document, but I hope to write some layout tutorials in the future.

The controls I've found most useful to actually create .xaml files are Grid and StackPanel for laying out where data goes, and Label, Image, TextBlock, and Border for actually presenting the data.

EXPORTING TO IMAGES
-------------------
*Disabled currently*
To create images of your cards, select the cards you want to make images of (ctrl+a works to select everything on a tab), and hit the "Export Selected..." button. This will generate images in the program's directory. The cards will be named after the tab, and numbered sequentially (tab001.png, tab002.png, etc). This means it's pretty easy to overwrite your own images if you're only exporting a couple images at a time, and means you have to manually move files around a lot. I'll fix this in the future.

PRINTING
--------
*Disabled currently*
Printing cards directly works, but is a little unintuitive. Select the cards you want to print, just like exporting images. Then hit "Print Selected..." This will open the print dialog, but this one won't actually print anything. It's just used to get paper size. Configure your printing options the way you want, then press Print. A Print Preview window will appear; from here, you can hit Print, and it will actually work.

Right now, the program will always try and put 8 cards on a portrait page, and 9 on a landscape. This is dumb, but, it's the way it works right now. My apologies.
