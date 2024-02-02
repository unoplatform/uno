---
uid: Uno.Features.AdaptiveTrigger
---

# Using Adaptive Triggers With Uno Platform Applications

## AdaptiveTrigger Class

Use the `AdaptiveTrigger` class to trigger a `<VisualState.Setters>` target. The target invokes changes in window properties, such as font size or panel orientation.

For more information, please refer to Microsoft's documentation on the [AdaptiveTrigger class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.adaptivetrigger) and the [VisualState class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.visualstate).

## Differences between Uno and WinUI

In a standard WinUI application, the sequential order of the adaptive trigger does not matter. The compiler will execute any `<VisualState.Setters>` target regardless of their order. For more information, see [AdaptiveTriggers' order declared in XAML shouldn't matter GitHub issue](https://github.com/unoplatform/uno/issues/2662).

In an Uno Platform application, the sequential order of the adaptive trigger does matter. In this instance, the `VisualState` will execute the `Setters` when it finds the first matching occurrence. Subsequent triggers may be ignored if the first matching property remains true for the life of the program.

## Sample XAML

Following is an example of three `<VisualState>` blocks. The blocks are non-sequential, ordered from `SmallScreen` to `LargeScreen` to `MediumScreen`. The expected output is to have the font size change as the window size changes.

Each `<VisualState>` block has an adaptive trigger with a `MinWindowWidth` property. The property tells the `VisualState` to execute the `VisualState.Setters` whenever the window width is greater or equal to its integer value.

Example:

```xml
<Page
    x:Class="UnoTutorial1.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:UnoTutorial1"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"    
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup>
                <VisualState x:Name="SmallScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 20 when window width >=200 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="200"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="20"/>
                    </VisualState.Setters>
                </VisualState>

                <VisualState x:Name="LargeScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 40 when window width is >=1000 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="1000"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="40"/>
                    </VisualState.Setters>
                </VisualState>

                <VisualState x:Name="MediumScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 30 when window width >=720 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="720"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="30"/>
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>

        <StackPanel x:Name="stackPanel" Orientation="Vertical">
            <TextBlock x:Name="Block1" Text="Sample text line 1."/>
            <TextBlock Text="Sample text line 2." FontSize="{Binding FontSize, ElementName=Block1, Mode=TwoWay}"/>
            <TextBlock Text="Sample text line 3." FontSize="{Binding FontSize, ElementName=Block1, Mode=TwoWay}"/>
        </StackPanel>
    </Grid>
</Page>
```

## Expected Output

The above code might generate an unexpected output in an Uno application.

`SmallScreen` is listed first in order. Because the property `MinWindowWidth="200"` remains true for the life of the program, the compiler will never execute the `LargeScreen` or `MediumScreen` adaptive triggers. In this instance, the font size never changes.

This example demonstrates why developers must consider the order of their adaptive triggers, as the ordering may generate unexpected outcomes.

The following demonstrates the same code, but re-written in a sequential order to generate the expected output:

```xml
<Page
    x:Class="UnoTutorial1.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:UnoTutorial1"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"    
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup>
                <VisualState x:Name="LargeScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 40 when window width is >=1000 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="1000"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="40"/>
                    </VisualState.Setters>
                </VisualState>

                <VisualState x:Name="MediumScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 30 when window width >=720 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="720"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="30"/>
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="SmallScreen">
                    <VisualState.StateTriggers>
                        <!--Increase FontSize to 20 when window width >=200 effective pixels.-->
                        <AdaptiveTrigger MinWindowWidth="200"/>
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Block1.FontSize" Value="20"/>
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>

        <StackPanel x:Name="stackPanel" Orientation="Vertical">
            <TextBlock x:Name="Block1" Text="Sample text line 1."/>
            <TextBlock Text="Sample text line 2." FontSize="{Binding FontSize, ElementName=Block1, Mode=TwoWay}"/>
            <TextBlock Text="Sample text line 3." FontSize="{Binding FontSize, ElementName=Block1, Mode=TwoWay}"/>
        </StackPanel>
    </Grid>
</Page>
```

## Correct Output

In the above example, the `VisualState` blocks are sequential, ordered from `LargeScreen` to `MediumScreen` to `SmallScreen`. Because of this, the compiler will execute the first true occurrence in the order written.

The `LargeScreen` setter target is triggered when the window width is `>=` 1000 pixels. When this statement becomes false, the `VisualState` executes the next setter target that matches, i.e. the `MediumScreen` block. When the `MediumScreen` adaptive trigger is false, the `VisualState` finally executes the setter target for the `SmallScreen` block.
