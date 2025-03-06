---
uid: Uno.HotDesign.Overview
---

# Hot Design™ Overview

> [!IMPORTANT]
> **Hot Design™** is currently in **beta**. To start using **Hot Design**, ensure you are signed in with your Uno Platform account. Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in.
>
> - Hot Design is now available on all platforms in beta, with the Desktop platform (`-desktop` target framework) currently offering the most stable and reliable experience. Other platforms are still undergoing stabilization. See the list of [known issues](https://aka.platform.uno/hot-design-known-issues).
> - Hot Design does not support C# Markup and is only available with XAML and .NET 9. Additionally, Hot Design is not supported for the WinAppSDK target framework at this time.
> - Hot Design relies on [Hot Reload](xref:Uno.Platform.Studio.HotReload.Overview) for updates, so be sure to check the [current support for your OS, IDE, and target platforms](xref:Uno.Platform.Studio.HotReload.Overview#supported-features-per-os) before testing.
> - Your input matters! Share your thoughts and help us improve Hot Design. [Find out how to provide feedback here](xref:Uno.Platform.Studio.Feedback).

Welcome to **Hot Design**, a next-generation runtime visual designer for cross-platform .NET applications!

> [!Video https://www.youtube-nocookie.com/embed/fODrUH0zno0]

## What is Hot Design?

**Hot Design** transforms your live, running app into a real-time visual designer that works with any IDE on any OS. It allows you to make UI changes on the fly without restarting your app or losing state, while seamlessly synchronizing your XAML code and visual designs.

In addition, [Hot Reload](xref:Uno.Features.HotReload) works seamlessly with **Hot Design**, allowing you to see UI changes instantly without rebuilding your app. This boosts productivity, reduces iteration time, and provides real-time feedback for both visual and functional tweaks in your UI. Hot Reload also includes a visual indicator to help you monitor changes as you develop, further enhancing your workflow.

**Hot Design** is part of the **Uno Platform Studio**, a suite of tools designed to streamline your cross-platform app development and boost productivity.

[➜ Learn more about Uno Platform Studio](xref:Uno.Platform.Studio.Overview)

### Key Features

**Hot Design** empowers you to:

- **Achieve the Fastest Inner DevLoop**: With a single click, turn your running app into a visual Designer. Another click returns you to your app, keeping you in your workflow without disruption.
- **Design in Real Time**: Modify your app’s UI instantly while it’s running, enabling fast, interactive development.
- **Leverage Your Favorite IDE**: Seamlessly integrate with Visual Studio, VS Code, or JetBrains Rider on any OS, with IDE-agnostic support.
- **Synchronize Code and Designer**: Reflect changes instantly between the Designer and code, ensuring your live app and XAML remain a single source of truth.
- **Integrate Live Data**: Connect your UI to data sources intuitively and see real-time updates to data bindings as you build, simplifying the process.
- **Work Directly with Real Data**: Skip mock data creation by working with actual data sources from your running app, gaining a true-to-life feel of your app's behavior. Mock data is also supported for added flexibility.
- **Reuse Custom & 3rd-Party Controls**: Incorporate custom and third-party UI components effortlessly while maintaining a consistent look and behavior across platforms.
- **Manage State with Flexibility**: Work seamlessly with MVVM or MVUX to consume real-time data while keeping UI logic separate from core logic.
- **Apply Styles Easily**: Enhance your app’s UI and UX with predefined styles, applied effortlessly in just a few clicks — no coding required.
- **Explore Responsive Layouts**: Test layout options with a single click and instantly visualize how your app adapts to different devices and form factors.
- **Switch Themes Effortlessly**: Toggle between light and dark modes with one click to ensure a consistent user experience across color schemes.
- **Design on Remote Devices**: Fine-tune your UI directly on remote devices or emulators, instantly seeing changes without the need for constant redeployment.
- **Simplify Property Management**: Use Smart Properties to quickly find, modify, and bind key UI properties without leaving the live design environment, saving time and effort.

## Why Hot Design™?

**Hot Design™** brings together runtime UI design, live data integration, and cross-platform development to streamline your app-building process. It empowers you to work more efficiently, stay in the flow, and deliver polished, cross-platform apps with ease.

By simplifying UI development and accelerating your workflow, **Hot Design** helps you stay productive and focus on creating great applications.

**Let’s get started!**

## Next Steps

- **[Get Started Guide](xref:Uno.HotDesign.GetStarted.Guide)**
  Getting started with setting up **Hot Design** and exploring the key areas and features of the visual designer it offers.

- **[Counter App Tutorial](xref:Uno.HotDesign.GetStarted.CounterTutorial)**
  A hands-on walkthrough for building the [Counter App](xref:Uno.Workshop.Counter) using **Hot Design**, showcasing its features and workflow in action.
