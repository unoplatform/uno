---
uid: Uno.Features.Uno.MCPs
---

# The Uno Platform MCPs

**Uno Platform MCPs** (Model Context Protocol servers) supercharge your AI coding assistants with specialized knowledge and capabilities for Uno Platform development.

Uno Platform provides two [MCPs](https://modelcontextprotocol.io/docs/getting-started/intro):

- **Uno Platform MCP** - Provides up-to-date Uno Platform documentation and best practice prompts
- **Uno Platform App MCP** - Gives your AI assistant "eyes" and "hands" to interact with your running application

## Why Use Uno Platform MCPs?

- **Faster Development**: Get instant answers from Uno Platform documentation without leaving your IDE
- **Intelligent Assistance**: AI agents understand Uno Platform best practices and patterns
- **Live App Testing**: AI can see, click, and interact with your running app to validate changes
- **Reduced Context Switching**: Keep your focus on coding while AI handles documentation lookups

This guide shows you how to use both MCPs effectively in your development workflow.

## Uno Platform MCP

The Uno Platform MCP is a cloud-hosted service that provides your AI assistant with:

- **Documentation Search**: Find relevant Uno Platform documentation instantly
- **Documentation Retrieval**: Fetch complete documentation pages on demand
- **Best Practice Prompts**: Pre-configured commands to guide AI with Uno Platform patterns

### Quick Start Prompts

These prompts are automatically available when your AI agent supports MCP (e.g., Claude Desktop, VS Code with extensions):

#### `/new` - Create a New Uno Platform App

Use this when starting a fresh project. The AI will guide you through creating an Uno Platform application with current best practices.

**Example:**
```text
/new
```

Then describe your app:
```text
I want to build a todo list app with MVUX and Material theme
```

#### `/init` - Prime Agent for Existing Project

Use this at the start of a coding session in an existing app. It loads Uno Platform best practices into the AI's context.

**Example:**
```text
/init
```

Then ask for features:
```text
Add a settings page with dark mode toggle
```

### Effective Prompting Tips

To get the best results from the Uno Platform App MCP:

1. **Mention "Uno Platform documentation"** when you want specific information:
   ```text
   Search the Uno Platform documentation for MVUX binding syntax
   ```

2. **Ask for examples** to get code snippets:
   ```text
   Show me an example from the docs of using ListView with MVUX
   ```

3. **Request best practices** for guidance:
   ```text
   What are the Uno Platform best practices for navigation?
   ```

For more sample prompts and scenarios, see our [AI Agents Getting Started Guide](xref:Uno.BuildYourApp.AI.Agents).

### Understanding Uno Platform MCP Tools

The Uno Platform MCP provides these tools to AI agents (you don't call them directly):

| Tool | Purpose | When Used |
|------|---------|----------|
| `uno_platform_docs_search` | Search documentation | When you ask about Uno Platform features |
| `uno_platform_docs_fetch` | Retrieve full documents | After finding relevant topics |
| `uno_platform_agent_rules_init` | Load development best practices | Via `/init` prompt |
| `uno_platform_usage_rules_init` | Load API usage patterns | Via `/init` prompt |

**How to trigger these tools:**

Simply mention what you need in natural language:
- "Search the Uno Platform docs for..." → triggers `docs_search`
- "Show me the full documentation on..." → triggers `docs_fetch`
- Use `/init` prompt → triggers rule initialization

> [!TIP]
> You can disable automatic rule priming in your AI agent settings if you prefer manual control. Use the `/init` prompt when you want to explicitly load Uno Platform best practices.

## Uno Platform App MCP

The **Uno Platform App MCP** runs on your machine and connects to your Uno Platform application while it's running. This gives AI assistants the ability to:

- 👁️ **See** your app through screenshots and visual tree inspection
- 🖱️ **Interact** by clicking buttons and typing text
- 🔍 **Analyze** the UI structure and data context
- ✅ **Validate** that code changes work as expected

### How It Works

1. **Run your Uno Platform app** (any platform: Windows, WebAssembly, iOS, Android, etc.)
2. **The Uno Platform App MCP automatically connects** to your running application
3. **Your AI assistant can now interact** with the app to help you develop and test

### What You Can Do

#### Test Features Automatically

```text
Can you click the "Add Item" button and verify that a new item appears in the list?
```

#### Get Visual Feedback

```text
Take a screenshot of the app and tell me if the button looks centered
```

#### Debug Issues

```text
Click on the second text box and check what its DataContext is
```

#### Validate Layouts

```text
Show me the visual tree of the main page so I can see the layout structure
```

### Available Tools by License

#### Community Edition (Free)

| Tool | What It Does | Example Prompt |
|------|--------------|----------------|
| Get Runtime Info | Shows app details (OS, platform, PID) | "What platform is the app running on?" |
| Take Screenshot | Captures current app state | "Take a screenshot of the app" |
| Click | Clicks at coordinates | "Click the button in the center" |
| Press Key | Sends keyboard input | "Press Enter key" |
| Type Text | Types longer text | "Type 'Hello World' in the text box" |
| Get Visual Tree | Shows UI element hierarchy | "Show me the visual tree" |
| Invoke Element Action | Triggers default automation action | "Click the submit button" |
| Close App | Closes the application | "Close the app" |

#### Pro Edition

| Tool | What It Does | Example Prompt |
|------|--------------|----------------|
| Advanced Element Actions | Invokes specific automation peer actions | "Expand the tree view item" |
| Get DataContext | Retrieves element's data binding context | "What's the DataContext of this grid?" |

> [!NOTE]
> Pro features require a **[Uno Platform Studio Pro subscription](https://platform.uno/select-subscription/)**.

### Getting Started with Uno Platform App MCP

1. **Start your Uno Platform app** in debug or release mode
2. **Open your AI assistant** (e.g., Claude Desktop, VS Code)
3. **Verify MCP connection** - Look for the Uno App MCP in your AI tool's MCP list
4. **Start asking questions** - The AI can now interact with your running app

### Practical Examples

**Example 1: Testing a Form**
```text
I just added a new registration form. Can you:
1. Type "john@example.com" in the email field
2. Type "password123" in the password field  
3. Click the "Register" button
4. Tell me if a success message appears
```

**Example 2: Debugging Layout Issues**
```text
The logout button should be in the top right corner.
Can you take a screenshot and tell me where it actually is?
```

**Example 3: Verifying Data Binding**
```text
I bound a list to ItemsControl. Can you check the visual tree 
and tell me if the items are rendering correctly?
```

## Troubleshooting

### Common Issues

**Uno Platform App MCP not connecting:**
- Ensure your Uno Platform app is running
- Check that you're signed into [Uno Platform Studio](xref:Uno.GetStarted.Licensing)
- Verify MCPs are enabled in your AI tool settings

**Uno Platform MCP not responding:**
- Check your internet connection
- Verify the MCP server is listed in your AI tool's configuration

**AI not using the tools:**
- Try explicitly mentioning "use your tools" or "search the Uno Platform docs"
- Use premium AI models (e.g,. Claude Sonnet 4.5, GPT-4) for best results
- In Visual Studio 2022/2026, [enable MCPs via the tools icon](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022#configuration-example-with-a-github-mcp-server)

### Need More Help?

For detailed troubleshooting steps, see our [AI Agents Troubleshooting Guide](xref:Uno.UI.CommonIssues.AIAgents).

## Next Steps

- **[Try the sample prompts](xref:Uno.BuildYourApp.AI.Agents)** to see MCPs in action
- **[Learn about Hot Design® Agent](xref:Uno.HotDesign.Agent)** for AI-assisted visual design
- **[Join our Discord](https://aka.platform.uno/discord)** to share your MCP experiences
