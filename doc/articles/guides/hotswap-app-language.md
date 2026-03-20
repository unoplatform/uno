---
uid: Uno.Tutorials.ChangeAppLanguage
---

# How to change app language at runtime

This guide will walk you through the necessary steps for changing app language at runtime.

> [!TIP]
> The complete source code that goes along with this guide is available in the [unoplatform/Uno.Samples](https://github.com/unoplatform/Uno.Samples) GitHub repository - [RuntimeCultureSwitching sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/LocalizationSamples/RuntimeCultureSwitching)

## Prerequisites

Make sure to setup your environment first by [following our instructions](xref:Uno.GetStarted).

## Steps to change application language at runtime

> [!NOTE]
> This guide is an extension of ["How to use localization"](localization.md), and will build on top the sample from that guide.
> Make sure you have completed the previous guide or downloaded the [full localization sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/LocalizationSamples/Localization), before continuing.

1. Add two new pages to the `Localization.Shared` project by:
    Right-click on `Localization.Shared` > Add > New Item ... > Visual C# > XAML > Blank Page
    And, name them `Page1` and `LanguageSettings`
1. Add some content to the two new pages:

    * `Page1.xaml`:

        ```xml
        <Page x:Class="UnoLocalization.Page1"
              xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:toolkit="using:Uno.UI.Toolkit">

            <StackPanel toolkit:VisibleBoundsPadding.PaddingMask="Top">
                <TextBlock x:Uid="Page1_Title" Text="Page one" FontSize="30" />

                <Button x:Uid="Page1_GoBack" Content="Go back" Click="GoBack" />
            </StackPanel>
        </Page>
        ```

    * `Page1.xaml.cs`:

        ```csharp
        private void GoBack(object sender, RoutedEventArgs e) => Frame.GoBack();
        ```

    * `LanguageSettings.xaml`:

        ```xml
        <Page x:Class="UnoLocalization.LanguageSettings"
              xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
              xmlns:toolkit="using:Uno.UI.Toolkit">

            <StackPanel toolkit:VisibleBoundsPadding.PaddingMask="Top">
                <TextBlock x:Uid="LanguageSettings_Title" Text="Language Settings" FontSize="30" />

                <Button Content="English" Click="SetAppLanguage" Tag="en" />
                <Button Content="Français" Click="SetAppLanguage" Tag="fr" />

                <Button x:Uid="LanguageSettings_GoBack"
                        Content="Go back"
                        Click="GoBack"
                        Margin="0,12,0,0"/>
            </StackPanel>
        </Page>
        ```

    * `LanguageSettings.xaml.cs`:

        ```csharp
        private void SetAppLanguage(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string tag)
            {
                // Change the app language
                Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = tag;

                // Clear the back-navigation stack, and send the user to MainPage
                // This is done because any loaded pages (MainPage(in back-stack) and LanguageSettings (current active page))
                // will stay in the previous language until reloaded.
                Frame.BackStack.Clear();
                Frame.Navigate(typeof(MainPage));
            }
        }

        private void GoBack(object sender, RoutedEventArgs e) => Frame.GoBack();
        ```

1. Add two new buttons to `MainPage` for navigation:
    * `MainPage.xaml`:

        ```xml
        <StackPanel toolkit:VisibleBoundsPadding.PaddingMask="Top">
            <TextBlock x:Uid="MainPage_IntroText" Text="Hello, world!" Margin="20" FontSize="30" />
            <TextBlock x:Name="CodeBehindText" Text="This text will be replaced" />

            <Button x:Uid="MainPage_GotoNextPage" Content="Next Page" Click="GotoNextPage" />
            <Button x:Uid="MainPage_GotoLanguageSettings" Content="Language Settings" Click="GotoLanguageSettings" />
        </StackPanel>
        ```

    * `MainPage.xaml.cs`:

        ```csharp
        private void GotoNextPage(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(Page1));
        private void GotoLanguageSettings(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(LanguageSettings));
        ```

1. Add a new folder `fr` under the Strings folder by: Right-click on String > Add > New Folder

1. Add a new resource file `Resources.resw` under the `fr` folder by: Right-click on `fr` > Add > New Item ... > Visual C# > Xaml > Resources File

1. Add the localization strings for the new elements:
    Open both `Strings\en\Resources.resw` and `Strings\fr\Resources.resw`, and add these:

    |Name|Value in `en\Resources.resw`|Value in `fr\Resources.resw`|
    |-|-|-|
    |MainPage_GotoNextPage.Content|`Next Page`|`Page suivante`|
    |MainPage_GotoLanguageSettings.Content|`Language Settings`|`Paramètres de langue`|
    |Page1_Title.Text|`Page in english`|`Page en français`|
    |Page1_GoBack.Content|`Go back`|`Retourner`|
    |LanguageSettings_Title.Text|`Language Settings`|`Paramètres de langue`|
    |LanguageSettings_GoBack.Content|`Go back`|`Retourner`|

## Get the complete code

See the completed sample on GitHub: [RuntimeCultureSwitching](https://github.com/unoplatform/Uno.Samples/tree/master/UI/LocalizationSamples/RuntimeCultureSwitching)

## Additional Resources

* [Globalization and localization](https://learn.microsoft.com/windows/uwp/design/globalizing/globalizing-portal)

---

[!include[getting-help](../includes/getting-help.md)]
