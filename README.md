## Editor Formulas Window

A simple window that lets you tap into all the one-method Editor scripts submitted by the Unity community.  
It's like a community-built Swiss army knife that has more tools added every day.  

<img width="330" alt="Editor Formulas Window" src="https://cloud.githubusercontent.com/assets/433535/16903304/9077bc06-4c83-11e6-9122-e0e491cf243e.png">

### Using the window
When you open the window for the first time, it will be empty for 1-2 seconds while it downloads the list of available Formulas from the [Editor Formulas Github repository](https://github.com/VoxelBoy/EditorFormulas). Once the initial download is complete, you will see a list of greyed out buttons for each Formula with a download button next to them, like so:  

<img width="302" alt="Download Formula" src="https://cloud.githubusercontent.com/assets/433535/16903516/801d052c-4c89-11e6-933e-dadb57f061f2.png">

Click the download button next to a Formula and in a few seconds, it will be ready to use.  
Click on the large button with the Formula name to run the Formula.  
If the Formula has parameters, they will be shown below the button.  

<img width="302" alt="Usable Formula" src="https://cloud.githubusercontent.com/assets/433535/16903531/e9eecd46-4c89-11e6-97bb-e325fb2c4de7.png">

Click on the 3 dotted button on the right to access Formula options.  

<img width="488" alt="Formula Options" src="https://cloud.githubusercontent.com/assets/433535/16903551/4df6fd5e-4c8a-11e6-9d05-373a5c3c30a6.png">

### Submitting Formulas
If you want to submit a Formula of your own:  

1. Create a copy of the FormulaTemplate.cs file that you can find in the Editor Formulas/Editor/Formulas folder.  
2. Choose a name for your Formula. Rename your file with the CamelCase version of your Formula name. Don't worry, it will automatically be *nicified* when shown in the Window.
3. Add a public static method to the class within your file and name it **exactly** the same as your file (minus the file extension, of course)
4. Fork the [Editor Formulas Github repository](https://github.com/VoxelBoy/EditorFormulas) and submit your Formula as a pull request.
5. Your Formula will be quickly reviewed. If it's not directly accepted, you will be contacted to correct any issues found.

### License

You are free to use, copy, distribute this software in a share-alike manner. You may not license or sell copies of this software or its derivatives. You can use this software in creating your commercial works since it's an Editor-only tool and it doesn't get included in builds.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.

Download and update icons by Mike Rowe, from Noun Project
https://thenounproject.com/itsmikerowe/

Options icon by Hello Many CA, from Noun Project
https://thenounproject.com/search/?q=options&i=65588
