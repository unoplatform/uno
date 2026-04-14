---
uid: Uno.Features.Accessibility.ControlsReference
---

# Controls accessibility reference

This page describes the expected screen reader behavior for each standard control, the XAML properties that affect its accessibility, and keyboard interactions.

> [!TIP]
> Test these behaviors using the `AccessibilityScreenReaderPage` sample in the Uno SamplesApp. It includes working examples of every control listed below.

## Buttons

**ARIA role:** `button`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "button" | "Save, button" |
| Disabled state as "dimmed" or "unavailable" | "Save, button, dimmed" |

```xml
<Button Content="Save"
        AutomationProperties.Name="Save document" />

<!-- Disabled button announces its state -->
<Button Content="Submit" IsEnabled="False" />

<!-- Button with supplemental description -->
<Button Content="Delete"
        AutomationProperties.HelpText="Permanently removes the selected items" />
```

**Keyboard:** `Tab` to focus, `Space` or `Enter` to activate.

## CheckBox

**ARIA role:** `checkbox`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "checkbox" + checked state | "Agree to terms, checkbox, unchecked" |
| State change on toggle | "checked" / "unchecked" |

```xml
<CheckBox Content="Agree to terms" />
```

**Keyboard:** `Tab` to focus, `Space` to toggle.

## RadioButton

**ARIA role:** `radio`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "radio button" + selected state | "Small, radio button, selected, 1 of 3" |

```xml
<StackPanel>
    <RadioButton Content="Small" GroupName="Size" />
    <RadioButton Content="Medium" GroupName="Size" />
    <RadioButton Content="Large" GroupName="Size" />
</StackPanel>
```

**Keyboard:** `Tab` to enter the group, arrow keys to move between options.

## ToggleButton

**ARIA role:** `button` (with pressed state)

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "toggle button" + pressed state | "Bold, toggle button, pressed" |

```xml
<ToggleButton Content="Bold" />
```

**Keyboard:** `Tab` to focus, `Space` to toggle.

## ToggleSwitch

**ARIA role:** `checkbox` (with on/off state)

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "switch" + on/off state | "Notifications, switch, on" |

```xml
<ToggleSwitch Header="Notifications" />
```

**Keyboard:** `Tab` to focus, `Space` to toggle.

## Slider

**ARIA role:** `slider`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + current value + range | "Volume, 50, slider" |
| Value change on adjustment | "55" |

```xml
<Slider AutomationProperties.Name="Volume"
        Minimum="0" Maximum="100" Value="50" />
```

**Keyboard:** `Tab` to focus, arrow keys to adjust value. On macOS VoiceOver, use `VO+Up/Down`.

## TextBox

**ARIA role:** `textbox`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "text field" | "Display name, text field" |
| Typed characters are read back | "a", "b", "c" |

```xml
<TextBox AutomationProperties.Name="Display name"
         AutomationProperties.HelpText="Enter the name shown on your profile" />

<!-- TextBox with Header (used as accessible name automatically) -->
<TextBox Header="Email address" />

<!-- Multi-line text box announced as "text area" -->
<TextBox AcceptsReturn="True"
         AutomationProperties.Name="Description" />

<!-- Required field -->
<TextBox AutomationProperties.Name="Full name"
         AutomationProperties.IsRequiredForForm="True" />
```

**Keyboard:** `Tab` to focus. Type to enter text. Screen readers may switch to "focus mode" (NVDA) or "edit mode".

## PasswordBox

**ARIA role:** `textbox` (password)

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "secure text field" | "Password, secure text field" |
| Characters are **not** read back | (silence while typing) |

```xml
<PasswordBox AutomationProperties.Name="Password" />
```

**Keyboard:** Same as `TextBox`. Characters are masked both visually and in screen reader output.

## ComboBox

**ARIA role:** `combobox`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + selected value + "combo box" + collapsed/expanded | "Country, United States, combo box, collapsed" |
| State change on open/close | "expanded" / "collapsed" |

```xml
<ComboBox AutomationProperties.Name="Country">
    <ComboBoxItem Content="United States" />
    <ComboBoxItem Content="Canada" />
    <ComboBoxItem Content="Mexico" />
</ComboBox>
```

**Keyboard:** `Tab` to focus, `Space` or `Enter` to open, arrow keys to navigate items, `Enter` to select, `Escape` to close.

## HyperlinkButton / Links

**ARIA role:** `link`

| What the screen reader announces | Example |
|----------------------------------|---------|
| Label + "link" | "Privacy policy, link" |

```xml
<HyperlinkButton Content="Privacy policy"
                 NavigateUri="https://example.com/privacy" />
```

**Keyboard:** `Tab` to focus, `Enter` to activate. NVDA users can press `K` to jump between links.

## ListView / ListViewItem

**ARIA roles:** `listbox` (container), `option` (items)

| What the screen reader announces | Example |
|----------------------------------|---------|
| Item content + position in set | "Inbox, 1 of 5" |

```xml
<ListView AutomationProperties.Name="Folders">
    <ListViewItem Content="Inbox" />
    <ListViewItem Content="Sent" />
    <ListViewItem Content="Drafts" />
</ListView>
```

`PositionInSet` and `SizeOfSet` are set automatically by the framework for list items.

**Keyboard:** `Tab` to enter the list, arrow keys to navigate items, `Space` to select.

## Headings

Headings are not controls — they are `TextBlock` elements with `AutomationProperties.HeadingLevel` set. They help screen reader users navigate the page structure.

```xml
<TextBlock Text="Settings"
           AutomationProperties.HeadingLevel="Level1" />
<TextBlock Text="Account"
           AutomationProperties.HeadingLevel="Level2" />
```

| What the screen reader announces | Example |
|----------------------------------|---------|
| Text + "heading level X" | "Settings, heading level 1" |

Headings are **not** reachable via `Tab` (they are not interactive). Use screen reader heading navigation:

- **VoiceOver:** `VO+Cmd+H` / `VO+Cmd+Shift+H`
- **NVDA/Narrator:** `H` / `Shift+H`, or `1`–`6` for specific levels

## Landmarks

Landmarks are structural containers with `AutomationProperties.LandmarkType` set. They help screen reader users jump between major page regions.

```xml
<StackPanel AutomationProperties.LandmarkType="Navigation"
            AutomationProperties.Name="Main menu">
    <!-- Navigation content -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Main">
    <!-- Main content -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Search"
            AutomationProperties.Name="Site search">
    <!-- Search UI -->
</StackPanel>

<StackPanel AutomationProperties.LandmarkType="Custom"
            AutomationProperties.LocalizedLandmarkType="Quick actions">
    <!-- Custom landmark -->
</StackPanel>
```

Landmarks are also **not** reachable via `Tab`. Use screen reader landmark navigation:

- **VoiceOver:** `VO+U` > Landmarks
- **NVDA/Narrator:** `D` / `Shift+D`

## Live regions

Live regions announce content changes automatically without requiring the user to navigate to them.

```xml
<TextBlock x:Name="StatusMessage"
           Text="Ready"
           AutomationProperties.LiveSetting="Polite" />

<!-- In code-behind: StatusMessage.Text = "3 items saved"; -->
<!-- Screen reader announces "3 items saved" automatically -->
```

| LiveSetting | Behavior |
|-------------|----------|
| `Polite` | Announced after current speech finishes |
| `Assertive` | Interrupts current speech immediately |

**Testing:** Navigate away from the live region (Tab to something else), then trigger the content change. The screen reader should announce the new content without you moving focus.

## Image

**ARIA role:** `img`

```xml
<!-- Informative image — provide a name -->
<Image Source="product.png"
       AutomationProperties.Name="Red running shoes, side view" />

<!-- Decorative image — hide from screen readers -->
<Image Source="divider.png"
       AutomationProperties.AccessibilityView="Raw" />
```

## See also

- [Accessibility overview](index.md)
- [AutomationProperties reference](automation-properties.md)
- [Testing with screen readers](testing-with-screen-readers.md)
