---
uid: Uno.HotDesign.Toolbar
---

# Hot Design® Toolbar

The **Hot Design® Toolbar** is your main control panel when working in the Uno Platform’s live design environment. 


Positioned at the top of the interactive canvas, it provides fast, intuitive access to tools that help you:

- Instantly **enter or exit** Hot Design mode to work directly with a live app.
- **Play and pause** the running application without restarting.
- **Switch themes** and **simulate different device sizes** to test UI responsiveness.
- Use **undo and redo** to iterate quickly and safely.
- Monitor **connection status** and Hot Reload activity.
- Access **layout and view options** to customize your workspace.

Whether you're fine-tuning the layout, testing behavior, or previewing responsiveness, the Toolbar keeps your most important actions one click away, helping you iterate faster and stay focused on design.


<p align="center">
  <img src="Assets/studio-toolbar.png" alt="Hot Design Toolbar" />
</p>

Below is a breakdown of every button and what it does:

---

## 1. Diagnostics Overlay  <img src="Assets/Diagnostics-Overlay.png" alt="Diagnostics Overlay - Hot Design" />

Hot Design® surfaces a small **Diagnostics Overlay** on top of your running app window. 


This overlay:

- Hosts the **Enter Hot Design** flame button.  Entry point to the Hot Design®.
- Shows current **connection status** and **Hot Reload** activity.  [LINK TO HOT RELOAD]
- Keeps essential controls always within reach—without cluttering the main canvas. [LINK TO CANVAS]

By default, the overlay appears in the **top-left** corner, but it won’t block your UI:

### 1.1 Repositioning the Overlay

You can **drag** the entire overlay (click and hold its header area) to any edge of the window—move it left, right, or top—to free up space while you edit.

#### Quick Visual Guide

<p align="center">
	<img src="Assets/gifs/ToolBar-Diagnostics-Overlay.gif" height="600" alt="GIF showing how to use Diagnostic Overlay to Enter Hot Design" />
</p>

---

## 2. Enter & Exit Hot Design

Within the **Diagnostics Overlay**, you’ll see the familiar **flame icon**:

1. **Enter Hot Design** <img src="Assets/toolbar-hot-design-enter-icon.png" alt="Enter Hot Design icon" height="30" />
   - Click the flame icon to launch **Hot Design mode**.  
   - Your running app’s UI becomes live–editable: move, style, and tweak XAML directly in the canvas.  


2. **Exit Hot Design** <img src="Assets/toolbar-hot-design-icon.png" alt="Exit Hot Design icon" height="30" />
   - When you’re done editing, click the same flame icon (now highlighted) to leave Hot Design mode.  
   - You’ll return to the usual code editor, with your app still running in the background.  

### Why Toggle?
  
 - Keep your focus: switch instantly between design without rebuilding and your live app.  
 - Stay in context: your last UI state remains visible when you exit.  


#### Quick Visual Guide

<p align="center">
  <img src="Assets/gifs/ToolBar-Enable-Disable.gif" alt="Demo: clicking the flame icon to enter and exit Hot Design mode" />
</p>

---

## 3. Play & Pause

The **Play/Pause** toggle lets you switch between editing your app live in Hot Design and previewing the final, running experience.


1. **Play**: Activate Hot Design mode.  <img src="Assets/toolbar-play.png" alt="Play icon" height="30" />  
  While playing, you can use Hot Design to:  
   - Adjust properties in the **Property Grid**. [LINK TO PROPERTYGRID DOCS]
   - Drag new controls from the **Toolbox** onto the canvas.  [LINK TO TOOLBOX]
   - Navigate and select elements in the **Elements**.  [link to hierarchy]
   - Interact directly with UI elements on the **canvas** (resize, move, style). [LINK TO CANVAS]


2. **Pause**: Deactivate Hot Design editing and return to the live app view.   <img src="Assets/toolbar-pause.png" alt="Pause icon" height="30" />
   - See all your XAML changes applied in the running application.  
   - Test animations, data bindings, and navigation exactly as end users will.
   - You can click on any Panels to return to Play mode.

### When to Use Each Mode

- **Play** 
 
  Use this mode to iteratively **build and refine** your UI. 
  Every tweak you make—whether adjusting a margin in the Property Grid or dragging a new button onto the canvas—applies instantly.  

- **Pause**  

  Switch here when you want a **true preview** of the running app. 
  It’s the best way to validate behavior, animations, and overall look & feel after you’ve made a series of edits.


#### Quick Visual Guide

<p align="center">
  <img src="Assets/gifs/ToolBar-Play-Pause.gif" alt="Demo: toggling Play and Pause in Hot Design" />
</p>

---

## 4. Undo & Redo

Easily roll back or reapply changes you’ve made in Hot Design:

- **Undo**  <img src="Assets/toolbar-undo.png" alt="Undo icon" height="30" />
  Reverts your last action—whether you moved a control, tweaked a margin, or updated a style. 
  Perfect for moments when a tweak didn’t turn out as expected.

- **Redo**  <img src="Assets/toolbar-redo.png" alt="Redo icon" height="30" />
  Restores an action you just undid. 
  Handy if you hit Undo too many times or want to compare before/after states without redoing your work manually.
  Or when the next change is better than the previous one.

### Examples of use 

  - You adjust a button’s Backgroun and it breaks your layout: hit **Undo** to quickly get back.  
  - You realize the previous Background was better: hit **Redo** to reapply it.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/gifs/ToolBar-Undo-Redo.gif" alt="Demo: using Undo and Redo in Hot Design" />
</p>

---

## 5. Designer Settins <img src="Assets/toolbar-form-factor.png" alt="Designer Settins icon" height="30" />

The **Designer Settings** area lets you tailor the Hot Design canvas to match any target device or viewing preference. 

<p align="center">
	<img src="Assets/DesignerSettings.png" alt="Designer Settins" width="500" />
</p>

So you can test responsive layouts and comfortable viewing:

### Form Factor

  Click the device-frame icon to choose phone, tablet, desktop, or watch simulations.
  Or you can change to a custom-size and use exact width/height to match any target screen.
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
    Use custom input fields where you can enter any width and height, or easyly toggle between width and height.
	Use this to match unusual screen sizes or prototype future device form factors.
	
<p align="center">
	<img src="Assets/DesignerSettings-FormFactor.png" alt="Designer Settins - FormFactor" width="500" />
</p>

#### Keyboard Shortcuts

  Here are the convenient key combinations you can use anywhere in Hot Design to control Form Factor custom Size:
  
  - **Ctrl + Shift + 0** - Window Size
  Set to the Size of the full window.
  
  - **Ctrl + Shift + 1** - Canvas Size
  Set to the Content area size - Calculated if you change the content area (eg. closing the Property Grid Panel - Shortcut: Control + Shift + P)

<p align="center">
	<img src="Assets/DesignerSettings-FormFactor-Shortcut.png" alt="Designer Settins - Form Factor ShortCut" width="500" />
</p>


### Zoom
  On the zoom menu you can select a custom percentage or use the Slider to set the Zoom level.
  Great for focusing on fine details or getting an overview of the full layout.

<p align="center">
	<img src="Assets/DesignerSettings-Zoom.png" alt="Designer Settins - Zoom" width="500" />
</p>


#### Keyboard Shortcuts

  Here are the convenient key combinations you can use anywhere in Hot Design to control zoom and fitting:

  - **Ctrl + Plus ( + )** - Zoom in  
  - **Ctrl + Minus ( – )** - Zoom out  
  - **Ctrl + 0** - Fit the canvas to your window  
  - **Ctrl + 1** - Zoom to 100%  
  - **Ctrl + 2** - Zoom to 200%  
  - **Ctrl + 3** - Zoom to 300%  
  - **Ctrl + 9** - Toggle **Auto-Fit** on or off  
  
> **Tip:** You can also hold **Ctrl** and scroll the mouse wheel to zoom in or out dynamically.  


<p align="center">
	<img src="Assets/DesignerSettings-ZoomPanel.png" alt="Designer Settins - Zoom Panel" width="500" />
</p>


### Auto-Fit

  When **Auto-Fit** is enable the entire canvas scales to your window. 
  Great for focusing on fine details or getting an overview of the full layout.

<p align="center">
	<img src="Assets/DesignerSettings-AutoFit.png" alt="Designer Settins - FormFactor" width="500" />
</p>


### Why it matters

 - Verify that your UI adapts across screens without pixel-perfect builds.  
 - Maintain legibility while designing by zooming in on tricky details or zooming out for context.

#### Quick Visual Guide

<p align="center">
  <img src="Assets/gifs/ToolBar-DesignerSettings.gif" alt="Demo: changing form factor and zoom level" />
</p>

---


## 6. Theme Toggle

Light/Dark

| Icon | Action                                       |
|:----:|:---------------------------------------------|
| <img src="Assets/toolbar-light-theme.png" alt="Light theme icon" height="30" /> | **Light Theme**: Render your app with the system light theme. |
| <img src="Assets/toolbar-dark-theme.png" alt="Dark theme icon" height="30" />   | **Dark Theme**: Render your app with the system dark theme. |

---

## 7. Connection & Hot Reload Status

Dev Server status/notifications

<img src="Assets/toolbar-connection-status.png" alt="Connection status icon" height="30" />

- **Connection status**: Indicates whether Hot Design is currently connected to the running app.  
- **Hot Reload indicator**: Flashes when XAML or code changes are pushed live; hover for details on the last update.

---

## 8. More Options

<img src="Assets/toolbar-more-options.png" alt="More options icon" height="30" />

Click to open the **More Options** menu, where you can:

- Toggle visibility of **Tool Windows** (Properties, Assets, Live Visual Tree).  
- Reset the Hot Design canvas to its default layout.  
- Access **Settings** for fine-tuning behavior (e.g., auto-reload on save, custom hotkeys).  
- View **About** information and version details.

More Options
Windows
In App / Show All / Show Panels
Resizable window panel sections
Help
Documentation
Feedback
Discord server
YouTube channel
Privacy Policy
Terms of Use

---

## Getting Started

1. **Open your Uno Platform project** in Visual Studio.  
2. **Build and run** in Debug configuration.  
3. **Click the Enter Hot Design** button on the Toolbar to attach the live designer.  
4. **Make XAML changes** and watch them apply instantly.  
5. Use **Play/Pause**, **Form Factor**, and **Theme** toggles to explore and refine your UI across contexts.

---

## Next Steps

- Explore more on **[???](xref:Uno.HotDesign.::;)**  