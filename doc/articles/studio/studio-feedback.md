---
uid: Uno.Platform.Studio.Feedback
---

# Providing Feedback for Uno Platform Studio

We deeply value your feedback for **Uno Platform Studio** and its tools (the [**Uno Platform Studio App**](xref:Uno.PlatformStudio.GetStarted), the [**Uno Platform Studio Agent**](xref:Uno.PlatformStudio.Agent), [**Hot Design<sup>®</sup>**](xref:Uno.HotDesign.Overview), [**Hot Reload**](xref:Uno.Platform.Studio.HotReload.Overview), and [**Design-to-Code**](xref:Uno.Figma.GetStarted)) to ensure we can deliver the best possible experience.

> [!IMPORTANT]
> To start using **Hot Design**, ensure you are signed in with your Uno Platform account. **Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in**.
>
> - Hot Design is available on all platforms. See the list of **[current known issues](xref:Uno.HotDesign.Troubleshooting)**.
> - Hot Design **does not support C# Markup** and is **only available with XAML and .NET 9+**.
> - Hot Design is **not available for the WinAppSDK target framework (`netX.0-windowsX.X.X`)** at this time.
> - Hot Design relies on **[Hot Reload](xref:Uno.Platform.Studio.HotReload.Overview)** for updates, so be sure to check the [current support for your OS, IDE, and target platforms](xref:Uno.Platform.Studio.HotReload.Overview#supported-features-per-os) before testing.

Here’s how you can share your feedback:

## 1. GitHub Feedback

Navigate to the [Uno Platform Studio GitHub repository](https://github.com/unoplatform/studio) to:

- **Report issues or bugs**: Share any unexpected behavior or issues you encounter with the tools or documentation.
- **Propose enhancements**: Suggest features or improvements to enhance Uno Platform Studio, its tools, and its documentation.
- **Start discussions**: Engage in conversations about Uno Platform Studio and its tools.

This is also the repository that publishes the [`uno-platform-studio` plugin and skills](xref:Uno.PlatformStudio.Skills), so it is the place to report issues with the **Uno Platform Studio Agent** when you use it in an external agent such as Claude Code, GitHub Copilot, or OpenAI Codex.

For more details, refer to the [feedback guidelines](https://github.com/unoplatform/studio/blob/main/README.md).

## 2. Uno Platform Studio App and Agent Feedback

You can send feedback directly from the [**Uno Platform Studio App**](xref:Uno.PlatformStudio.GetStarted) while you build. Because the [**Uno Platform Studio Agent**](xref:Uno.PlatformStudio.Agent) powers app generation and editing inside the app, this is also how you report issues with the agent's responses. Sending feedback packages a bundle, including the conversation, logs, and current project state, that gives the Uno Platform team the context needed to investigate.

There are two ways to send it:

- **From the More menu**: open **More** in the navigation pane and choose **Send feedback** to start the feedback flow at any time.
- **From a Critical failure card**: when an agent turn ends with a **Critical failure** outcome card in the conversation panel, use the **Report** button on the card to send a bundle for that specific failure.

For the full list of messages and outcomes you may encounter while working, and other ways to recover, see [Troubleshooting](xref:Uno.PlatformStudio.Troubleshooting).

## 3. Hot Design Feedback Menu

You can also provide feedback directly while using **Hot Design** in a live, running application. Use the **Feedback** menu to:

- **Report an issue/bug**
- **Suggest a feature**
- **Ask a question**

Follow these steps to access the feedback menu:

1. Click on the three-dot button in the [Hot Design Toolbar](xref:Uno.HotDesign.GetStarted.Guide#toolbar).
2. Navigate to **Help** > **Feedback**.
3. Choose one of the following options:
   - **Report an issue/bug**
   - **Suggest a feature**
   - **Ask a question**

## Additional Support

For further assistance, visit our [Discord Server](https://platform.uno/uno-discord), where our engineering team and community will be happy to assist you.

For organizations seeking a deeper level of support beyond our community support, please [contact us](https://platform.uno/contact).

We look forward to your feedback and thank you for helping us improve Uno Platform Studio and its tools!
