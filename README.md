CSCRIBE
=======
**Templating software** - v0.2

WHAT IS IT?
-----------
CScribe is software that transforms spreadsheets of data into images or printable files. Its intended use is to create images for playtesting and producing card games, though it could probably be used for other templating purposes as well.

If you want to see CScribe in action, I made a video showing CScribe's basic features, which can be viewed here: http://www.youtube.com/watch?v=e4trs7BrW-Y

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
The Export All option under the Export menu will create an image for every card in your set. If you only want images of certain cards, use the Export Selected option.

Image filenames can either be sequentially numbered (card001.png, card002.png, etc) or each card image can be named after an attribute on the card.

The images produced are always in .png format. More formats may be added in the future.

PRINTING
--------
Similar to exporting images, you can print only selected cards, or every card in your set.

Also similar to images, you can either print a fixed number of each card, or print a number of copies based on an attribute on the card.

SAVING PROJECTS
---------------
To save you from having to import data and set up each tab every time you open the program, you can save and load project files. The actual card data is *not* saved with a project, only that data's source. This means any changes to your cards will be reflected if you reopen a project.
