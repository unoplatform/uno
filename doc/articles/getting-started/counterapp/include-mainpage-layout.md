This defines a page with a background set to the theme resource **ApplicationPageBackgroundThemeBrush**, meaning it will adapt to the theme (**Dark** or **Light**) of the application.

The page contains a **`StackPanel`**, which will lay out controls in a vertical stack and is aligned in the center of the page, both horizontally and vertically. The **`StackPanel`** contains a single **`TextBlock`** control, which displays the text `Hello Uno Platform` and is aligned in the horizontal center of the **StackPanel**.

## Add a Control

We're going to replace the existing **`TextBlock`** with an **`Image`** but before we can do this, we need to add the image file to the application. Download this [SVG image](Assets/logo.svg) and add it to the **Assets** folder inside the **Counter** project. At this point, you should rebuild the application in order for the image to be included in the application package.

> [!NOTE]
> If you're working in Visual Studio, select the newly added **logo.svg** file in the **Solution Explorer**, open the **Properties** tool window, and make sure the **Build Action** property is set to **`UnoImage`**. For other IDEs, no further action is required as the template automatically sets the **Build Action** to **`UnoImage`** for all files in the **Assets** folder.

Including SVG files with the **`UnoImage`** build action will use **Uno.Resizetizer** to convert the SVG file to a PNG file for each platform. The generated PNG files will be included in the application package and used at runtime. For more information on using **Uno.Resizetizer** in Uno Platform, see [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted).

Now that we have the image file, we can replace the **`TextBlock`** with an **`Image`**.
