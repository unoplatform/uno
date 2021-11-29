# Design and Build Uno Applications With Figma
It is now possible to use Figma to design and build Uno applications. Designers can use the highly collaborative Figma environment to fine-tune the User Experience (UX) while putting in place building blocks of an application following the Material Design language.
Using the [Uno Platform Figma Plugin](https://www.figma.com/community/plugin/1045528009520465828/Uno-Platform), it is possible to visualize how the application will render and export actual XAML to use in Visual Studio for the application. This application could be ready to localize, ready for accessibility and can optionally make use of Uno Extensions (Reactive, Navigation...).
## Designer's Side
https://www.youtube.com/watch?v=5G73e21yN_w
### Quick Start
1. Duplicate the [Uno Material Toolkit Document](https://www.figma.com/community/file/1045411863379988189).
2. Open the document and go to the _Getting started_ page. There's a lot of information there to adjust the colors, the typography and how to start a new project.
3. Open the _Theme_ page and ensure you have **nothing selected** in the document. On the right-hand side of Figma, there's the `Color Styles` section where it is possible to adjust colors for the styles of the application. When passing the mouse over a specific color, there's a _Edit style_ button appearing. This button allows picking another color for the chosen style.
4. Open the _Example app_ page. You can rename it to fit your application name or even create new ones as needed.
5. Creating a new page: Open the _Assets_ pane (at the top/left of the screen) and open the `Patterns & Templates / Page templates` section. Pick one of the templates and drag it on the design surface.
6. Adding components to an existing page: Open the _Assets_ page (at the top/left of the screen) and open the `Components` section. Pick a desired component and drag it inside an existing page on the design surface.
### Tips for designers
* Time should be taken to ensure the _Resizing_ section is set to appropriate values for each component. This is important to ensure proper layout of the result.
## Developer's Side
1. The  [Uno Platform Figma Plugin](https://www.figma.com/community/plugin/1045528009520465828/Uno-Platform) must be installed for the current user.
2. Open the document saved by the designer. Since a plugin will be used, **Edition privileges are required** by figma to run any plugin on a document.
3. Open the _Example app_ page (it may be renamed to something else by the designer) and right-click on a designed screen.
4. Open the `Plugins`-> `Uno Platform`. The initialization could take few seconds.
5. Pick the second tab (the "PLAY ICON") called `Preview`.
6. Click the Refresh button at the button of the plugin.
7. After few seconds, a XAML rendered version of the selected page should be displayed.
8. Pick the third tab called `Xaml`. An editor will display the generated XAML. An `export XAML` button at the bottom right-hand of the plugin will copy the XAML into the clipboard, which can be used to create a XAML Page in VisualStudio.
### Tips for developers
* The XAML exported using the `Export XAML` button is different than the XAML presented in the editor. The version presented in the editor is simplified and doesn't specify XML namespaces. The Export will contain everything required in VisualStudio to compile, including the `<Page>` root declaration and the `x:Class` clause.
* It is possible to change the namespace of the application in the first tab called `Properties`. By default a namespace will be created from the document's name. The same namespace is used for the whole Figma document.
* The generated screen (application page) is always for currently selected _Frame_ in Figma. An option in the settings called `Auto Sync` will redo the generation each time the selection changes.