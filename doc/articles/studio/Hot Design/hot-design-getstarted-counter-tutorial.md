---
uid: Uno.HotDesign.GetStarted.CounterTutorial
---

# Create a Counter App with Hot Design™

This tutorial will guide you through using Hot Design to create a simple counter application. The application will include:

- An `Image` at the top.
- A `TextBox` below the image, where you can set the step size for incrementing the counter.
- A `TextBlock` below the `TextBox`, displaying the current counter value.
- A `Button` at the bottom labeled **"Increment Counter by Step Size"**, which updates the counter value based on the step size entered.

<p align="center">
  <img src="Assets/counter-app.png" alt="Counter App Preview" />
</p>

> [!NOTE]
> This tutorial is based on the [XAML + MVUX variant](xref:Uno.Workshop.Counter.XAML.MVUX) of the Counter app tutorial. It demonstrates how to create a simple cross-platform app using Uno Platform. If you prefer to use MVVM you can still complete this **Hot Design** tutorial by switching the MVUX code, with the MVVM code from the [XAML + MVVM variant](xref:Uno.Workshop.Counter.XAML.MVVM). Explore other tutorial variants [here](xref:Uno.Workshop.Counter).
>
> Additionally, as a general note, Hot Design can be used without selecting a specific architectural pattern, such as MVVM or MVUX, making it a versatile tool for various projects. For this particular tutorial, however, we will focus on using MVUX as an example.
>
> [!IMPORTANT]
> **Hot Design™** is currently in **beta**. To start using **Hot Design**, ensure you are signed in with your Uno Platform account. Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in.
>
> - Hot Design is now available on all platforms in beta, with the Desktop platform (`-desktop` target framework) currently offering the most stable and reliable experience. Other platforms are still undergoing stabilization. See the list of [known issues](https://aka.platform.uno/hot-design-known-issues).
> - Hot Design does not support C# Markup and is only available with XAML and .NET 9. Additionally, Hot Design is not supported for the WinAppSDK target framework at this time.
> - Hot Design relies on [Hot Reload](xref:Uno.Platform.Studio.HotReload.Overview) for updates, so be sure to check the [current support for your OS, IDE, and target platforms](xref:Uno.Platform.Studio.HotReload.Overview#supported-features-per-os) before testing.
> - Your input matters! Share your thoughts and help us improve Hot Design. [Find out how to provide feedback here](xref:Uno.Platform.Studio.Feedback).

## Set Up Your Environment for Hot Design

> [!IMPORTANT]
> If you're new to developing with Uno Platform, start by [setting up your environment](xref:Uno.GetStarted). Once your environment is ready, proceed directly to the next section, **Creating the Counter Application**.

For existing applications, take this opportunity to update to the [latest **Uno.Sdk** version](https://www.nuget.org/packages/Uno.Sdk). Refer to our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) for upgrade steps.

> [!IMPORTANT]
> When upgrading to **Uno.Sdk 5.5 or higher**, the `EnableHotReload()` method in `App.xaml.cs` is deprecated and should be replaced with `UseStudio()`.

To start using **Hot Design**, ensure you are signed in with your Uno Platform account. Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in.

Once you're using the **latest stable 5.5 Uno.Sdk version or higher**, you can access **Hot Design** by clicking the **flame** button in the diagnostics overlay that appears over your app.

<p align="center">
  <img src="Assets/enter-hot-design-mode.png" alt="Hot Design flame button to enter in design mode" />
</p>

## Creating the Counter Application

### [Visual Studio](#tab/vs)

- Launch **Visual Studio** and click on **Create new project** on the Start Window. Alternatively, if you're already in Visual Studio, click **New, Project** from the **File** menu.
- Type "Uno Platform" in the search box
- Click **Uno Platform App**, then **Next**
- Name the project `Counter` and click **Create**
At this point you'll enter the **Uno Platform Template Wizard**, giving you options to customize the generated application. For this tutorial, we're only going to configure the presentation framework.
- Select **Blank** in **Presets** selection
- Select the **Presentation** tab and choose **MVUX**
- Click **Create** to complete the wizard
The template will create a solution with a single cross-platform project, named `Counter`, ready to run.

### [Rider](#tab/rider)

- Launch **Rider** and click on **New Solution** on the Start Window
- From the left menu, under the **Uno Platform** section, select **Uno Platform App**
At this point, you'll see options for creating a new Uno app, allowing you to customize the generated application. For this tutorial, we will only configure the presentation framework.
- Name the project `Counter`
- Select **Blank** in **Presets** selection
- Select the **Presentation** tab and choose **MVUX**
- Click **Create** to complete the creation
The template will create a solution with a single cross-platform project, named `Counter`, ready to run.

### [VS Code](#tab/vscode)

- Launch The Live Wizard by clicking [here](https://new.platform.uno/)
- Name the project `Counter` and click **Start**
- Select **Blank** in **Presets** selection
- Select the **Presentation** tab and choose **MVUX**
- Click **Create** to complete the wizard
- Copy the `dotnet new` command and run it from a terminal where you want your solution to be located.
- This will create a new folder called **Counter** containing the new application.
- Next, open the project using Visual Studio Code. In the terminal type the following:

  ```bash
  code ./Counter
  ```

- Visual Studio Code might ask to restore the NuGet packages. Allow it to restore them if asked.
- Once the solution has been loaded, in the status bar at the bottom left of VS Code, `Counter.sln` is selected by default. Select `Counter.csproj` to load the project instead.
![Counter.csproj selection in Visual Studio Code](Assets/vscode-csproj-selection.png)

### [Command Line](#tab/cli)

> [!NOTE]
> If you don't have the Uno Platform dotnet new templates installed, follow [these instructions](https://aka.platform.uno/dotnet-new-templates).

From the command line, run the following command:

```dotnetcli
dotnet new unoapp -preset blank -presentation mvux -o Counter
```

This will create a new folder called **Counter** containing the new application.

---

## Assets

First, we need to add the image file to the application. Download this [SVG image](https://aka.platform.uno/counter-tutorial-svg-uno-logo) (Open this [link](https://aka.platform.uno/counter-tutorial-svg-uno-logo), right-click on the SVG image and select "Save as") and add it to the **Assets** folder. Once added, rebuild the application to ensure the image is included in the application package.

> [!NOTE]
> If you're working in Visual Studio, select the newly added **logo.svg** file in the **Solution Explorer**, open the **Properties** window, and ensure the **Build Action** property is set to **`UnoImage`**. For other IDEs, no further action is required as the template automatically sets the **Build Action** to **`UnoImage`** for all files in the **Assets** folder.  
>
> For more information on **Uno.Resizetizer** functionalities, visit [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted).

## Run the app

Before you run the application, switch the target platform to **Desktop** (`net9.0-desktop`) to enable Hot Design during debugging. For more information on how to switch the target platform, visit the documentation page for your IDE:

- [Visual Studio](xref:Uno.GettingStarted.CreateAnApp.VS2022#debug-the-app)
- [VS Code](xref:Uno.GettingStarted.CreateAnApp.VSCode#debug-the-app)
  > [!IMPORTANT]
  > In the status bar at the bottom left of VS Code, ensure `Counter.csproj` is selected (by default `Counter.sln` is selected).
  >
  > ![Counter.csproj selection in Visual Studio Code](Assets/vscode-csproj-selection.png)
- [Rider](xref:Uno.GettingStarted.CreateAnApp.Rider#debug-the-app)

> [!IMPORTANT]
>
> - **Hot Design** relies on [Hot Reload](xref:Uno.Platform.Studio.HotReload.Overview) for updates, so be sure to check the [current support for your OS, IDE, and target platforms](xref:Uno.Platform.Studio.HotReload.Overview#supported-features-per-os) before testing **with or without the debugger**.
> - **Hot Design** is now available on all platforms in beta, with the **Desktop** platform (`-desktop` target framework) currently offering the most stable and reliable experience. Other platforms are still undergoing stabilization. See the list of [known issues](https://aka.platform.uno/hot-design-known-issues).

Now, let's run the app.

## Sign in with your Uno Platform Account

If is not already previously done, to start using **Hot Design**, ensure you are signed in with your Uno Platform account. Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in.

## Enter Hot Design Mode

To start editing the UI, enter **Hot Design** by clicking the **flame** button in the diagnostics overlay that appears over your app (default position is in the top-left corner of the application window).

> [!NOTE]
> If you don't see the **Hot Design** flame button, ensure that you are [signed in with your Uno Platform Account](xref:Uno.GetStarted.Licensing), enrolled in the current beta, and using the [latest stable 5.5 Uno.Sdk version or higher](https://www.nuget.org/packages/Uno.Sdk).

<p align="center">
  <img src="Assets/enter-hot-design-mode.png" alt="Hot Design flame button to enter design mode" />
</p>

## Change the Layout

We are all set to start adding controls to create our Counter app. Follow the steps below:

> [!NOTE]
> When making changes via **Hot Design**, the XAML will automatically update to reflect your edits. Similarly, any changes made directly to the XAML will be reflected in the design.

### Remove Existing Elements

1. Remove the existing `StackPanel`. In the **Elements** window, select the `StackPanel`, right-click, and choose **Delete StackPanel**.

    ![Removing the StackPanel](Assets/gifs/1-remove-stackpanel.gif)

### Add a `StackPanel`

1. Let's add the container to hold our elements by adding a `StackPanel`. In the **Toolbox** window, search for "StackPanel". Once it appears in the search results, drag it onto the **Canvas**.

    ![StackPanel](Assets/gifs/2-add-stackpanel.gif)

    Alternatively, you can drag the element from the **Toolbox** and drop it onto the **Elements** window.

    ![StackPanel](Assets/gifs/2.1-add-stackpanel.gif)

    Another way would be to select the **existing element** in the **Elements** window where you want to add a new item, then double-click the desired item in the **Toolbox** window to add it as a child of the target.

    ![StackPanel](Assets/gifs/2.2-add-stackpanel.gif)

1. Now, let's edit a property of the `StackPanel` to align it vertically and horizontally to the center. Select the `StackPanel` from the **Elements** window or the **Canvas**. In the **Properties** window, on the right side of the app, find the `VerticalAlignment` property and set it to **Center**, then do the same for `HorizontalAlignment`.

    ![StackPanel Alignment](Assets/gifs/3-stackpanel-alignment.gif)

### Add an `Image` element

1. Next, add an `Image` element to the `StackPanel`. In the **Toolbox** window, search for "Image". Once it appears in the results, drag it onto the `StackPanel` using either the **Canvas** or the **Elements** window.

    > [!NOTE]
    > The image will appear with zero height until a source is set.

1. Now that the `Image` element is added, let's set the source for our `Image` element. In the **Properties** window, locate the `Source` property. Start typing the name of the image we previously added, and the results should appear. Select **Assets/logo.png**.

    ![Image Source](Assets/gifs/4-add-image-source.gif)

1. Now, let's edit some properties to enhance its appearance. In the **Properties** window, use the search button to find properties. Search for "Width" and set its value to **150**. Do the same for `Height`. Our `Image` element is now complete!

    ![Image](Assets/gifs/4.1-search-properties.gif)

### Add a `TextBox` element

1. The next step is to add a `TextBox` that will hold the increasing step value for our Counter app. In the **Toolbox** window, search for "TextBox." Once it appears in the results, drag it onto the `StackPanel`, making sure to place it under the `Image` element.
1. Now, let's set the `TextBox` properties. In the **Properties** window, set the `PlaceholderText` to "Step Size" and set the `TextAlignment` to **Center**. Reset the `Text` property by clicking the **Advanced** button to open the **Advanced Property** flyout, followed by clicking the **Reset** button.

    ![Reset Text](Assets/gifs/3.1-textbox-clear-text.gif)

### Add a `TextBlock` element

1. The next element to add is the `TextBlock`, which will display the current value of our Counter app. In the **Toolbox** window, search for "TextBlock." Once it appears in the results, drag it onto the `StackPanel`, ensuring it is placed under the `TextBox`.
1. Let's edit the `TextBlock` properties. In the **Properties** window, set the `Text` to "Counter: 1"; and, set the `TextAlignment` to **Center**.

### Add a `Button` element

1. The final element is the `Button` that will increment the **Count** value. From the **Toolbox** window, search for "Button" and once the result appears, drag it onto the `StackPanel`, making sure it is added under the `TextBlock` element.
1. Set the `Button` properties. In the **Properties** window, set the `Content` to "Increment Counter by Step Size".

> [!NOTE]
> If there's insufficient room to edit the `Content` property you can resize the **Properties** window by dragging the left edge of the **Properties** window to the left.

![Resize Properties Window](Assets/gifs/4.2-resize-property-view.gif)

### Multi-selection

Hot Design allows you to select multiple elements and edit common properties simultaneously. Let's try it:

1. Hold the **Ctrl** key on your keyboard and click on the `Image`, the `TextBox` and the `TextBlock` (the `Button` should still be selected from the previous step).
2. In the **Properties** window, set `HorizontalAlignment` to **Center** and `Margin` to **12**.

    ![Multi Selection](Assets/gifs/5-multi-selection.gif)

> [!NOTE]
> You can also use multi-selection from the **Elements** window by holding the **Ctrl** key while clicking on each node.

### Style Picker

Hot Design allows you to apply existing styles to your elements for a polished appearance. Let's change the style of our `Button`:

1. Select the `Button`, either from the **Elements** window or the **Canvas**.
2. At the top of the **Properties** window, locate the Style Picker.
3. Choose **ButtonRevealStyle** to apply it.

    ![Button Style](Assets/gifs/6-button-style.gif)

## MainModel and Data Binding

As part of creating the application, we selected MVUX as the presentation framework. This added a reference to [**MVUX**](https://aka.platform.uno/mvux) which is responsible for managing our Models and generating the necessary bindings.
Without closing the application, return to your IDE and add a new class named `MainModel` and paste the following code to the newly created class:

```csharp
namespace Counter;

internal partial record Countable(int Count, int Step)
{
    public Countable Increment() => this with
    {
        Count = Count + Step
    };
}

internal partial record MainModel
{
    public IState<Countable> Countable => State.Value(this, () => new Countable(0, 1));
    public ValueTask IncrementCounter()
            => Countable.UpdateAsync(c => c?.Increment());
}
```

As the application uses MVUX, the `MainModel` class is used to generate a bindable ViewModel, `MainViewModel`.  Modify `MainPage.xaml.cs` to make an instance of `MainViewModel` available to be data bound and connected to the UI. Add `DataContext = new MainViewModel();` to `MainPage.xaml.cs` right after `InitializeComponent();`.

After making these changes in the IDE, save all files and return to Hot Design.

> [!NOTE]
> VS Code and Rider will automatically trigger **Hot Reload** when the files are saved.
>
> In Visual Studio, you can manually trigger **Hot Reload** by clicking the Hot Reload button ![TextBox](Assets/vs-hot-reload-icon.png) on the **Visual Studio top toolbar**.

### Set Binding

#### TextBox

Now, we need to bind the `TextBox`'s `Text` to the `Countable.Step` property of our ViewModel.
In the **Properties** window, locate the `Text` property. Click the **Advanced** button to the right of the `TextBox` to open the **Advanced Property** flyout. Select **Binding**. In the **Path** dropdown, locate the `Countable` property of our ViewModel, click the arrow to expand its properties, and select `Step`. Finally, from the **Mode** dropdown, select **TwoWay**.

![TextBox](Assets/gifs/7-textbox-binding.gif)

#### TextBlock

For the `TextBlock`, bind the `Text` property to `Countable.Count`, just as we did with the `TextBox`. For this step it is not necessary to set the `Mode`.

#### Button

Finally, let's bind the **Command** to the `IncrementCounter` task of our ViewModel.

![Button Command](Assets/gifs/8-button-command.gif)

## Wrap Up

At this point, you should have a working counter application. Click the **Play** button in the **Toolbar**, adjust the step size, and click the button to see the application in action.

> [!NOTE]
> The **Play** button lets you interact with the app directly within **Hot Design**, without needing to leave the editor. Once you're done interacting with the application, you can click the **Pause** button to return to designing your application. If you wish to leave Hot Design and return to the running application, you can click the **Flame** button in the **Toolbar**.

![WrapUp](Assets/gifs/9-wrapup.gif)
