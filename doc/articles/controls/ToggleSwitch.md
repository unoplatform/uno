---
uid: Uno.Controls.ToggleSwitch
---

# ToggleSwitch in Uno Platform

`ToggleSwitch` represents a switch that can be toggled between two states.

## ToggleSwitch WinUI Default Style

Uno provides a full support of the ToggleSwitch WinUI style.
The ToggleSwitch with the default style looks the same on all platforms, both statically and in motion.
If you need to have a custom design, you can just update the ToggleSwitch WinUI default style for your needs.

## ToggleSwitch Native Default Style

With the `NativeDefaultToggleSwitch` style on Android and iOS, the ToggleSwitch uses the native toggle control of each platform.
Of course, you can still bind to its properties in XAML as you normally would.
This is another powerful option to have: for some apps it makes sense to look as 'native' as possible, for others its desirable to have a rich, customized UI.
You may even want to mix and match different approaches for different screens in your app.

### Native Android ToggleSwitch

The Native Style for ToggleSwitch on Android is based on `SwitchCompat`, a Material Design version of the Switch widget supported by API 7 and above.
It does not make any attempt to use the platform provided widget on those devices which it is available normally.
This ensures the same behavior on all system versions.

#### Platform support on Android

| Property             | `BindableSwitchCompat` |
|----------------------|:----------------------:|
| Checked              |           X            |
| Enabled              |           X            |
| Text                 |           X            |
| TextColor            |           X            |
| TextOff              |                        |
| TextOn               |                        |
| ShowText             |                        |
| SplitTrack           |                        |
| SwitchMinWidth       |                        |
| SwitchPadding        |                        |
| SwitchTextAppearance |                        |
| Thumb                |                        |
| ThumbTextPadding     |                        |
| ThumbTint            |           X            |
| ThumbTintMode        |                        |
| Track                |                        |
| TrackTint            |           X            |
| TrackTintMode        |                        |

#### Native Android ToggleSwitch Style

If you need the simple native style :

```xml
    <android:Style x:Key="NativeDefaultToggleSwitch"
                   TargetType="ToggleSwitch">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ToggleSwitch">
                    <BindableSwitchCompat Checked="{TemplateBinding IsOn, Mode=TwoWay}"
                                          Enabled="{TemplateBinding IsEnabled}"
                                          Text="{TemplateBinding Header}" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </android:Style>
```

If you need the native style but you want to change the Text, ThumbTint and TrackTint colors:

```xml
    <android:Style x:Key="NativeDefaultToggleSwitch"
                   TargetType="ToggleSwitch">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ToggleSwitch">
                    <BindableSwitchCompat Checked="{TemplateBinding IsOn, Mode=TwoWay}"
                                          Enabled="{TemplateBinding IsEnabled}"
                                          Text="{TemplateBinding Header}"
                                          TextColor="{TemplateBinding Foreground}"
                                          ThumbTint="{TemplateBinding BorderBrush}"
                                          TrackTint="{TemplateBinding Background}" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </android:Style>
```

### Native iOS ToggleSwitch

Based on the UISwitch.

#### Platform support on iOS

| Property                                   | BindableUISwitch |
| ------------------------------------------ |:----------------:|
| IsOn                                       |         X        |
| Enabled                                    |         X        |
| OnTintColorBrush                           |         X        |
| TintColorBrush                             |         X        |
| ThumbTintColorBrush                        |         X        |
| OnImage                                    |                  |
| OffImage                                   |                  |

#### Native iOS ToggleSwitch Style

If you need the simple native style:

```xml
<ios:Style x:Key="NativeDefaultToggleSwitch"
           TargetType="ToggleSwitch">
    <!-- Ensures the UISwitch's shadow doesn't get clipped. -->
    <Setter Property="ClipsToBounds"
            Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ToggleSwitch">
                <BindableUISwitch IsOn="{TemplateBinding IsOn, Mode=TwoWay}"
                                  Enabled="{TemplateBinding IsEnabled}" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</ios:Style>
```

If you need the native style but you want to change the Tint, OnTinT and ThumbTint colors:

```xml
<ios:Style x:Key="NativeDefaultToggleSwitch"
            TargetType="ToggleSwitch">
    <!-- Ensures the UISwitch's shadow doesn't get clipped. -->
    <Setter Property="ClipsToBounds"
            Value="False" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ToggleSwitch">
                <BindableUISwitch IsOn="{TemplateBinding IsOn, Mode=TwoWay}"
                                  Enabled="{TemplateBinding IsEnabled}"
                                  TintColorBrush="{TemplateBinding BorderBrush}"
                                  OnTintColorBrush="{TemplateBinding Background}"
                                  ThumbTintColorBrush="{TemplateBinding Foreground}" />
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</ios:Style>
```
