---
uid: Uno.BuildYourApp.AI.Agents
---

# Prompting in your App

Once you have a running app, you're setup to have your agent help you develop features.

In the agent chat, ask the following:

  > [!IMPORTANT]
  > The MCP tools are best used by agents when using premium models like Claude 4.5, GPT-5-Codex or GPT-5.

  ```text
  Add a button and a textblock in my main page. Have a button click
  display the current date in the new TextBlock. Make sure to validate
  the app's runtime behavior using your tools.
  ```

Your AI agent will act and use the tools provided by Uno Platform.

## Using the MCP tools

Uno Platform provides two **[MCP](https://modelcontextprotocol.io/docs/getting-started/intro) (Model Context Protocol) servers**, the **Remote Uno MCP** and the **Local App MCP**.

For more detailed information, you can read further on [the tools offered by Uno Platform MCPs](xref:Uno.Features.Uno.MCPs).

### MCP (Remote)

This is hosted publicly and provides:

- A set of tools to search and fetch Uno Platform documentation
- A set of prompts to create and develop Uno Platform applications.

### App MCP (Local)

The Uno App MCP is a runtime service for the Uno Platform that allows large language models (LLMs) or other agent-based tools to intelligently interact with live Uno applications across platforms such as Windows, WebAssembly, macOS, iOS, Android, and Linux, in a real runtime context.

Rather than relying on static UI captures or screenshots, Uno App MCP exposes structured application state (like the Visual Tree, data context, and control properties), enabling rich, meaningful automation and inspection.

It allows tools and agents to:

- Attach to or stop a running Uno application session
- Inspect the application state, such as the visual tree, data context, taking screenshots, not just visual output
- Perform actions like clicking on controls, typing text, navigating pages, or invoking automation peers
- Leverage those interactions for higher-level automation: UI testing, adaptive exploration, telemetry, and intelligent workflow orchestration

In other words: while traditional Uno tests are written and maintained by developers, Uno App MCP opens the door for AI agents or other automation systems to drive and reason about the app autonomously, with deeper context and less manual scripting, across all Uno-supported platforms.

## Sample prompts for MCP tools

In your agent, there are some phrases that can be used to nudge the agent to use the tools.

> [!NOTE]
> Your agent will execute tools from our MCPs, you will need to approve them at your convenience.
> [!NOTE]
> In Visual Studio 2022/2026 MCPs might not be enabled automatically. Make sure to [click the "tools" icon](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022#configuration-example-with-a-github-mcp-server) in the chat window to check both uno platform MCPs.

- To ask the agent to explain what MVUX is:

  ```text
  Can you use the uno platform documentation to explain what MVUX is?
  ```

- To ask the agent to take a screenshot and analyze it:

    ```text
    Can you take a screenshot of the running app and describe what you see?
    ```

    > [!NOTE]
    > Some models don't yet support analyzing images, particularly in Visual Studio. Saving a screenshot on disk is always available.

- To ask the agent to click on a specific element on screen:

    ```text
    Can you click the second button on the left, close to the middle of the screen?
    ```

- To ask the agent to click on a specific element on screen:

    ```text
    Can you type "Hello Uno Platform, the MCP is talking!" in the third textbox on the screen?
    ```

- To ask to delete all the text of a textbox

    ```text
    Can you select all the text of the second textbox and delete all of it?
    ```

- To analyze the DataContext of the app:

    ```text
    Can you tell me if there's an object of type "XYZ" in the app's 
    DataContext, starting from the grid in the middle of the screen?
    ```

- To create a more specialized Uno Platform agent:

    ```text
    /init
    ```

    Which primes the agent with Uno Platform's best practices.

    Then ask the agent to build a new feature:

    ```text
    Let's add a new page in my app that shows the "About" information.
    ```

All those phrases will hint the model to use one or more of the tools to answer your request.

## Troubleshooting MCP Servers

You can find additional information about [troubleshooting AI Agents](xref:Uno.UI.CommonIssues.AIAgents) in our docs.
