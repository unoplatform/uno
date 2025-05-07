---
uid: Uno.HotDesign.Toolbar
---

# Toolbar

The **Hot Design<sup>®</sup> Toolbar** is your main control panel when working in the Uno Platform’s live design environment.

<p align="center">
  <img src="Assets/studio-toolbar.png" alt="Hot Design Toolbar" />
</p>

Positioned at the top of the interactive canvas, it provides fast, intuitive access to tools that help you:

- Instantly **enter or exit** Hot Design<sup>®</sup> mode to work directly with a live app.
- **Play and pause** the running application without restarting.
- **Switch themes** and **simulate different device sizes** to test UI responsiveness.
- Use **undo and redo** to iterate quickly and safely.
- Monitor **connection status** and Hot Reload activity.
- Access **layout and view options** to customize your workspace.

Whether you're fine-tuning the layout, testing behavior, or previewing responsiveness, the Toolbar keeps your most important actions one click away, helping you iterate faster and stay focused on design.

Below is a breakdown of every button and what it does:

## 1. Diagnostics Overlay

Hot Design<sup>®</sup> surfaces a small **Diagnostics Overlay** on top of your running app window.

<p align="center">
  <img src="Assets/Diagnostics-Overlay.png" alt="Diagnostics Overlay - Hot Design" />
</p>

This overlay:

- Hosts the **Enter Hot Design<sup>®</sup>** flame button.  Entry point to the Hot Design<sup>®</sup>.
- Shows current **connection status** and [Hot Reload](xref:Uno.Platform.Studio.HotReload.Overview) activity.
- Keeps essential controls always within reach—without cluttering the main [Canvas](xref:Uno.HotDesign.Canvas).

By default, the overlay appears in the **top-left** corner, but it won’t block your UI:

### 1.1 Repositioning the Overlay

You can **drag** the entire overlay (click and hold its header area) to any edge of the window—move it left, right, or top—to free up space while you edit.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Diagnostics-Overlay.gif" height="600" alt="How to use Diagnostic Overlay to Enter Hot Design" />
</p>

## 2. Enter & Exit Hot Design<sup>®</sup>

Within the **Diagnostics Overlay**, you’ll see the familiar **flame icon**:

**Enter Hot Design<sup>®</sup>**

- Click the flame icon to launch **Hot Design mode**.
- Your running app’s UI becomes live–editable: move, style, and tweak XAML directly in the canvas.

<p align="center">
 <img src="Assets/toolbar-hot-design-enter-icon.png" alt="Enter Hot Design icon" height="30" />
</p>

**Exit Hot Design<sup>®</sup>**

- When you’re done editing, click the same flame icon (now highlighted) to leave Hot Design<sup>®</sup> mode.
- You’ll return to the usual code editor, with your app still running in the background.

<p align="center">
  <img src="Assets/toolbar-hot-design-icon.png" alt="Exit Hot Design icon" height="30" />
</p>

### 1.1 Why Toggle?

- Keep your focus: switch instantly between design without rebuilding and your live app.
- Stay in context: your last UI state remains visible when you exit.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Enable-Disable.gif" alt="Demo: clicking the flame icon to enter and exit Hot Design mode" />
</p>

## 3. Play & Pause

The **Play/Pause** toggle lets you switch between editing your app live in Hot Design<sup>®</sup> and previewing the final, running experience.

**Play**: Activate Hot Design<sup>®</sup> mode.
  While playing, you can use Hot Design<sup>®</sup> to:

- Adjust properties in the [Property Grid](xref:Uno.HotDesign.Properties).
- Drag new controls from the [Toolbox](xref:Uno.HotDesign.Toolbox) onto the [Canvas](xref:Uno.HotDesign.Canvas).
- Navigate and select elements in the [Elements](xref:Uno.HotDesign.Elements).
- Interact directly with UI elements on the [Canvas](xref:Uno.HotDesign.Canvas) (resize, move, style).

<p align="center">
 <img src="Assets/toolbar-play.png" alt="Play icon" height="30" />
</p>

**Pause**: Deactivate Hot Design<sup>®</sup> editing and return to the live app view.

- See all your XAML changes applied in the running application.
- Test animations, data bindings, and navigation exactly as end users will.
- You can click on any Panels to return to Play mode.

<p align="center">
  <img src="Assets/toolbar-pause.png" alt="Pause icon" height="30" />
</p>

### When to Use Each Mode

- **Play**
  Use this mode to iteratively **build and refine** your UI.
  Every tweak you make—whether adjusting a margin in the Property Grid or dragging a new button onto the canvas—applies instantly.

- **Pause**
  Switch here when you want a **true preview** of the running app.
  It’s the best way to validate behavior, animations, and overall look & feel after you’ve made a series of edits.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Play-Pause.gif" alt="Demo: toggling Play and Pause in Hot Design" />
</p>

## 4. Undo & Redo

Easily roll back or reapply changes you’ve made in Hot Design:

- **Undo**
  Reverts your last action—whether you moved a control, tweaked a margin, or updated a style.
  Perfect for moments when a tweak didn’t turn out as expected.

<p align="center">
  <img src="Assets/toolbar-undo.png" alt="Undo icon" height="30" />
</p>

- **Redo**
  Restores an action you just undid.
  Handy if you hit Undo too many times or want to compare before/after states without redoing your work manually.
  Or when the next change is better than the previous one.

<p align="center">
  <img src="Assets/toolbar-redo.png" alt="Redo icon" height="30" />
</p>

### Examples of use

- You adjust a button’s Background and it breaks your layout: hit **Undo** to quickly get back.
- You realize the previous Background was better: hit **Redo** to reapply it.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Undo-Redo.gif" alt="Demo: using Undo and Redo in Hot Design" />
</p>

## 5. Designer Settings

The **Designer Settings** area lets you tailor the Hot Design<sup>®</sup> canvas to match any target device or viewing preference.

<p align="center">
  <img src="Assets/toolbar-form-factor.png" alt="Designer Settings icon" height="30" />
</p>

This is the full window when click on the Designer Settings,
where you can test responsive layouts and comfortable viewing:

<p align="center">
  <img src="Assets/DesignerSettings.png" alt="Designer Settings" width="500" />
</p>

### Form Factor

Click the device-frame icon to choose phone, tablet, desktop, or watch simulations.
Or you can change to a custom-size and use exact width/height to match any target screen.

<p align="center">
  <img src="Assets/DesignerSettings-FormFactor.png" alt="Designer Settings - FormFactor" width="500" />
</p>

Adjust how you view and test your UI across devices and scales:

- **Narrowest (IoT)**
Simulates very small screens (149 × 298 px).

Ideal for embedded or IoT scenarios where every pixel counts and UI elements must be ultra-compact.

- **Narrow (Phone)**
Emulates a typical smartphone portrait view (390 × 844 px).

Use this to verify touch targets, navigation bars, and mobile-specific layouts.

- **Normal (Tablet)**
Represents a standard tablet portrait orientation (768 × 1 024 px).

Great for multi-pane designs, responsive grids, and ensuring content scales gracefully.

- **Wide (Laptop)**
Mimics a laptop or small desktop window in landscape (768 × 1 024 px).

Useful for verifying menus, toolbars, and wider aspect ratios.

- **Widest (Desktop)**
Covers large desktop and high-resolution displays (1 080 × 1 920 px).

Perfect for full-screen layouts, complex dashboards, and widescreen presentations.

- **Custom**
Use custom input fields where you can enter any width and height, or easily toggle between width and height.

Use this to match unusual screen sizes or prototype future device form factors.

#### Keyboard Shortcuts

Here are the convenient key combinations you can use anywhere in Hot Design<sup>®</sup> to control Form Factor custom Size:

- **Ctrl + Shift + 0** - Window Size
Set to the Size of the full window.

- **Ctrl + Shift + 1** - Canvas Size
Set to the Content area size - Calculated if you change the content area (eg. closing the Property Grid Panel - Shortcut: Control + Shift + P)

<p align="center">
  <img src="Assets/DesignerSettings-FormFactor-Shortcut.png" alt="Designer Settings - Form Factor ShortCut" width="500" />
</p>

### Zoom

On the zoom menu you can select a custom percentage or use the Slider to set the Zoom level.

Great for focusing on fine details or getting an overview of the full layout.

<p align="center">
  <img src="Assets/DesignerSettings-Zoom.png" alt="Designer Settings - Zoom" width="500" />
</p>

#### Keyboard Shortcuts

Here are the convenient key combinations you can use anywhere in Hot Design<sup>®</sup> to control zoom and fitting:

- **Ctrl + Plus ( + )** - Zoom in
- **Ctrl + Minus ( – )** - Zoom out
- **Ctrl + 0** - Fit the canvas to your window
- **Ctrl + 1** - Zoom to 100%
- **Ctrl + 2** - Zoom to 200%
- **Ctrl + 3** - Zoom to 300%
- **Ctrl + 9** - Toggle **Auto-Fit** on or off

> **Tip:** You can also hold **Ctrl** and scroll the mouse wheel to zoom in or out dynamically.

<p align="center">
  <img src="Assets/DesignerSettings-ZoomPanel.png" alt="Designer Settings - Zoom Panel" width="500" />
</p>

### Auto-Fit

When **Auto-Fit** is enable the entire canvas scales to your window.
Great for focusing on fine details or getting an overview of the full layout.

<p align="center">
  <img src="Assets/DesignerSettings-AutoFit.png" alt="Designer Settings - FormFactor" width="500" />
</p>

### Why it matters

- Verify that your UI adapts across screens without pixel-perfect builds.
- Maintain legibility while designing by zooming in on tricky details or zooming out for context.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-DesignerSettings.gif" alt="Demo: changing form factor and zoom level" />
</p>

## 6. Theme Toggle

Easily switch between **Light** and **Dark** modes within the designer, with no need to change your operating system or IDE theme.

This option helps you preview and validate how your layouts will appear in both light and dark environments, making it easier to spot issues like missing contrast, transparency glitches, or unreadable text.

- **Light Theme**
Renders the canvas using the system **light** theme for bright, clean visuals.

<p align="center">
  <img src="Assets/toolbar-light-theme.png" alt="Light theme icon" height="24" />
</p>

- **Dark Theme**
Renders the canvas using the system **dark** theme for a modern, eye-friendly UI.

<p align="center">
  <img src="Assets/toolbar-dark-theme.png" alt="Dark theme icon" height="24" />
</p>

### Why it matters

Designs that look good in one theme can break in another. This toggle lets you verify your UI against both modes quickly—ensuring contrast ratios, icons, and styles adapt properly across the board.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Theme.gif" alt="Demo: changing Theme Light/Dark" />
</p>

## 7. Connection & Hot Reload Status

The connection status icon gives you real-time feedback on whether Hot Design<sup>®</sup> is actively connected to your app and if Hot Reload is functioning correctly.

<p align="center">
  <img src="Assets/toolbar-connection-status.png" alt="Connection status icon" height="30" />
</p>

The icon updates dynamically to reflect the current state:

- **Initializing** – Hot Design<sup>®</sup> is setting up the connection to your running app.
- **Ready** – Connected and ready to push live updates as you work.
- **Unlicensed** – Hot Reload is unavailable due to a missing or invalid license.
- **Disabled** – Hot Reload has been turned off or failed to initialize.

Clicking the status icon opens the **event log panel**, which shows a timeline of key events during your session.

This log provides:

- Detailed **status updates**, including successful reloads, warnings, and errors.
- **Timestamps** for each event, helping you trace what happened and when.
- Insights to help you understand and troubleshoot any issues with sync or live changes.

### What to look for

- The **status icon** visually changes depending on the current connection state.
- A **Hot Reload notification** appears when important events occur (e.g., failed update, reload blocked).
- Hover over events in the log panel to get extra context and error messages.

Use this feature to keep track of your live design updates and identify any problems quickly.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Events.gif" alt="Demo: Hot Reload Status and Event Panel" />
</p>

## 8. More Options

Clicking the three-dot icon opens the **More Options** menu, giving you full control over your Hot Design<sup>®</sup> workspace, access to help resources, and quick links for feedback.

<p align="center">
  <img src="Assets/toolbar-more-options.png" alt="More options icon" height="30" />
</p>

Here you can find:

- **Windows**
  Toggle panel visibility to focus on what matters:
  - **Show Tools In-App**: dock Properties, Assets, Live Visual Tree inside the canvas or disable to open a external window.
  - **All**: show or hide every tool panel at once (Ctrl + Shift + A)
  - **Toolbox**: toggle the ToolBox (List of Elements to Drag and Drop to your application) visibility (Ctrl + Shift + T)
  - **Elements**: toggle the Elements (Live Visual Tree of all Elements) visibility (Ctrl + Shift + E)
  - **Properties**: toggle the Property Grid (Where you can change all Properties values) visibility (Ctrl + Shift + P)

- **Help**
  - **Documentation**

    Find guidance and tutorials:
    - **Overview**: high-level Hot Design<sup>®</sup> guide
    - **Getting Started**: step-by-step introduction
    - **Counter App Tutorial**: hands-on example
  
  - **Feedback**  
    Share your experience or suggest improvements:
    - **Report an issue/bug**: open an external window to Create new **Bug** issue on GitHub
    - **Suggest a feature**:  open an external window to Create **Enhancement Request** new  issue on GitHub
    - **Ask a question**: open an external window to Uno Platform Studio Discussions page

  - **Community & Resources**
    Quick links to stay connected:
    - **Discord server**: open an external window to Discord Community
    - **YouTube channel**: open an external window to Youtube channel, where you can see the more recent Uno Platform videos.

  - **Legal & About**
    View policies and version info:
    - **Privacy Policy**: open an external window to Uno Platform Privacy Policy
    - **Terms of Use**:  open an external window to Terms of Use for Uno Platform Websites & Apps

### Quick Visual Guide

<p align="center">
  <img src="Assets/ToolBar-Help.gif" alt="Demo: opening the More Options menu" />
</p>

## Next

- [Toolbox](xref:Uno.HotDesign.Toolbox)
