---
uid: Uno.BuildYourApp.AI.Agents
---

# Using AI Agents with Your Uno Platform App

Once your Uno Platform app is running, AI agents can help you develop features, test functionality, and debug issues—all through natural conversation.

> [!TIP]
> For AI-assisted UI development integrated directly into your running app, try **[Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent)** which provides specialized assistance for visual design and development tasks.

## Getting Started

### Prerequisites

1. **A running Uno Platform app** (any platform: Windows, WebAssembly, iOS, Android, etc.)
2. **An AI agent with MCP support** (Claude Desktop, VS Code with Copilot, etc.)
3. **Premium AI model recommended** - Claude Sonnet 4.5, GPT-4, or similar for best results
4. **MCPs enabled** - Verify Uno Platform MCPs are connected in your AI tool

> [!NOTE]
> In Visual Studio 2022/2026, MCPs might not be enabled automatically. [Click the "tools" icon](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022#configuration-example-with-a-github-mcp-server) in the chat window to check both Uno Platform MCPs.

### Your First Prompt

Try this example to see AI agents in action:

```text
Add a button and a TextBlock to my main page. When the button is clicked,
display the current date in the TextBlock. Use your tools to validate 
that the feature works correctly in the running app.
```

The AI agent will:
1. Write the XAML and code-behind
2. Apply the changes to your project
3. Use MCP tools to interact with your running app
4. Click the button and verify the date appears

This demonstrates how AI agents can develop AND test features automatically!

## Understanding Uno Platform MCPs

Uno Platform provides two **[MCP](https://modelcontextprotocol.io/docs/getting-started/intro) (Model Context Protocol) servers** that enhance your AI agent's capabilities:

### 1. Remote Uno MCP - Knowledge & Documentation

**What it provides:**
- 📚 Search and fetch Uno Platform documentation
- 💡 Best practice prompts and guidance  
- 🎯 Pre-configured commands like `/new` and `/init`

**When to use:**
- Learning Uno Platform features
- Getting code examples
- Understanding best practices

### 2. Local App MCP - Live App Interaction

**What it provides:**
- 👁️ Visual inspection (screenshots, visual tree)
- 🖱️ User interactions (click, type, navigate)
- 🔍 Deep analysis (data context, control properties)
- ✅ Automated testing and validation

**Why it matters:**

Traditional testing requires you to write test code manually. The App MCP gives AI agents direct access to your running app, enabling:

- **Autonomous testing** - AI can verify features work correctly
- **Interactive debugging** - AI can explore your app to find issues  
- **Visual validation** - AI can check layouts and styling
- **Data inspection** - AI can analyze bindings and data context

**Works everywhere:** Windows, WebAssembly, macOS, iOS, Android, and Linux

For complete details on all available tools, see [Uno Platform MCPs Reference](xref:Uno.Features.Uno.MCPs).

## Effective Prompts for Development

Here are proven prompts that work well with Uno Platform MCPs. Copy and adapt them to your needs!

> [!IMPORTANT]
> Your AI agent will ask for permission before executing MCP tools. Approve them to enable full functionality.

### Documentation & Learning

**Search Uno Platform Documentation:**

```text
Can you search the Uno Platform documentation and explain what MVUX is?
```

```text
Show me examples from the Uno Platform docs on how to use ListView with data binding
```

```text
What are the Uno Platform best practices for navigation between pages?
```

### Visual Testing & Screenshots

**Capture and Analyze:**

```text
Take a screenshot of the app and tell me if the login button is visible
```

```text
Capture the current state and describe the layout of the main page
```

> [!NOTE]
> Some AI models may not support image analysis in certain environments (like Visual Studio). Screenshots can always be saved to disk for manual review.

### User Interactions

**Clicking Elements:**

```text
Click the "Submit" button at the bottom of the form
```

```text
Can you click the second button on the left side of the screen?
```

**Typing Text:**

```text
Type "Hello Uno Platform!" in the username text box
```

```text
Clear all text from the search box and type "MVUX tutorial"
```

**Keyboard Actions:**

```text
Press Enter in the active text field
```

```text
Select all text in the second textbox and delete it
```

### Data & Structure Analysis

**Inspect DataContext:**

```text
What's the DataContext of the ListView in the center of the screen?
```

```text
Check if there's a User object in the DataContext of the main grid
```

**Examine Visual Tree:**

```text
Show me the visual tree structure of the current page
```

```text
List all the buttons in the visual tree and their names
```

### Feature Development

**Prime Agent with Best Practices:**

Start your session by loading Uno Platform knowledge:

```text
/mcp.uno.init
```

or depending on your AI tool:

```text
/uno.init
```

This loads Uno Platform best practices, patterns, and conventions into the agent's context.

**Then Build Features:**

```text
Add a new Settings page with a dark mode toggle switch
```

```text
Create a user profile page that displays name, email, and avatar
```

```text
Implement a search feature that filters items as I type
```

**With Testing:**

```text
Add a login form and then test it by entering test credentials and 
verifying that the home page loads after clicking login
```

## Tips for Better Results

1. **Be Specific** - Describe what you want clearly and completely
2. **Mention Tools** - Say "use your tools" or "verify in the running app" to trigger MCP usage
3. **Test Iteratively** - Ask AI to test each change to catch issues early
4. **Use Layouts** - Reference screen positions ("top right", "center", "bottom") when clicking
5. **Check Results** - Always ask AI to verify changes worked correctly

## Troubleshooting

**AI isn't using MCP tools:**
- Explicitly say "use your tools" or "search the Uno Platform docs"
- Verify MCPs are enabled in your AI tool settings
- Try a premium AI model (Claude Sonnet 4.5, GPT-4, etc.)

**App MCP can't connect:**
- Ensure your Uno Platform app is running
- Check you're signed into [Uno Platform Studio](xref:Uno.GetStarted.Licensing)
- Restart your AI tool and app if needed

**Interactions failing:**
- Use descriptive positions ("top right button" vs exact coordinates)
- Take a screenshot first to let AI see the current state
- Try simpler interactions and build up complexity

For detailed troubleshooting, see [AI Agents Troubleshooting Guide](xref:Uno.UI.CommonIssues.AIAgents).

## Next Steps

- **[Learn more about Uno Platform MCPs](xref:Uno.Features.Uno.MCPs)** - Complete reference of all tools
- **[Try Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent)** - AI-assisted visual design
- **[Join our Discord](https://aka.platform.uno/discord)** - Share your experiences and get help
