## Editor Formulas Window

A simple window that lets you tap into all the single-class Editor scripts submitted by the Unity community.  
It's like a community-built Swiss army knife that has more tools added every day.  

<img width="330" alt="Editor Formulas Window" src="https://cloud.githubusercontent.com/assets/433535/16903304/9077bc06-4c83-11e6-9122-e0e491cf243e.png">

### Using the window
When you open the window for the first time, it will be empty for 1-2 seconds while it downloads the list of available Formulas from the [Editor Formulas Github repository](https://github.com/VoxelBoy/EditorFormulas). Once the initial download is complete, you will see a list of greyed out buttons for each Formula with a download button next to them, like so:  

<img width="302" alt="Download Formula" src="https://cloud.githubusercontent.com/assets/433535/16903516/801d052c-4c89-11e6-933e-dadb57f061f2.png">

Click the download button next to a Formula and when Unity is done compiling, it will be ready to use.  
Click on the large button with the Formula name to run the Formula.  
If the Formula has parameters, they will be shown below the button.  

<img width="324" alt="Usable Formula" src="https://cloud.githubusercontent.com/assets/433535/18251720/66f67fac-7393-11e6-9f92-c87adcb69c57.png">

Click on the Options button on the right to access Formula options.

<img width="538" alt="Formula Options" src="https://cloud.githubusercontent.com/assets/433535/18251723/6a9fa82c-7393-11e6-8ae6-7fddbf4c646f.png">

### Window options
Click on the dropdown at the top-right of the window to bring up the options menu.

<img width="522" alt="Window Options" src="https://cloud.githubusercontent.com/assets/433535/18251452/d82aa952-7391-11e6-895f-366b5f870e93.png">

Debug Mode toggle enables the window to push a variety of log messages to the console which can be useful to see what went wrong if an operation fails.
Show Hidden Formulas toggle allows you to show or hide the formulas you've chosen to hide.
Show Online Formulas toggle allows you to show or hide formulas you haven't downloaded yet.

### Submitting Formulas
If you want to submit a Formula of your own:  

1. Create a copy of the FormulaTemplate.cs file that you can find in the Editor Formulas/Editor/Formulas folder.  
2. Choose a name for your Formula. Rename the file and the class with the CamelCase version of your Formula name.
3. Modify the FormulaAttribute on the already existing Run method to pass it the formula name, tooltip, and author name.
4. Fork the [Editor Formulas Github repository](https://github.com/VoxelBoy/EditorFormulas) and submit your Formula as a pull request.
5. Your Formula will be quickly reviewed. If it's not directly accepted, you will be contacted to correct any issues found.

### License

You are free to use, copy, distribute this software in a share-alike manner. You may not license or sell copies of this software or its derivatives. You can use this software in creating your commercial works since it's an Editor-only tool and it doesn't get included in builds.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.

Download and update icons by Mike Rowe, from Noun Project
https://thenounproject.com/itsmikerowe/

Options icon by Hello Many CA, from Noun Project
https://thenounproject.com/search/?q=options&i=65588
