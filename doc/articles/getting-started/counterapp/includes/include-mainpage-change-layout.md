- The **`Width`** and **`Height`** have been set on the **`Image`** to ensure the image is displayed at the correct size. The **Source** property has been set to the path of the image file.

Run the application to see the updated **`MainPage`**. You should see the image displayed in the center of the page. Keep the application running whilst completing the rest of this tutorial. Hot Reload is used to automatically update the running application as you make changes to the application. For more information on Hot Reload, see [Hot Reload](xref:Uno.Features.HotReload).

## Change the Layout

The layout of the application uses a **`StackPanel`** which allows multiple controls to be added as children and will layout them in a vertical stack. An alternative to the **`StackPanel`** that is often used to control layout within an Uno Platform application is the **`Grid`**. The **`Grid`** allows controls to be laid out in rows and columns, and is often used to create more complex layouts.

A **`StackPanel`** is a good choice for this application as we want the controls to be laid out vertically, one above the other. Let's go ahead and add the remaining controls for the counter.
