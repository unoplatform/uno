---
uid: Uno.GetStarted.AI.CopilotCodingAgent
---

# Get Started with the GitHub Copilot Coding Agent

This guide will walk you through using the GitHub Copilot Coding Agent with Uno Platform MCPs.

## What is the GitHub Copilot Coding Agent

GitHub Copilot Coding Agent allows you to assign issues or pull requests directly to Copilot on GitHub.com (Assign to Copilot). When you assign a pull request to Copilot, it can help you implement features, fix bugs, and make changes to your codebase directly in the pull request.

This is different from:

- **GitHub Copilot CLI**: A command-line interface for interacting with Copilot
- **GitHub Copilot in VS Code**: The IDE extension that provides code completions and chat

## Uno Platform MCP Integration

The Uno Platform repository is pre-configured with the **Uno Platform MCP** (Model Context Protocol) server, which provides:

- **Documentation Search**: Access to up-to-date Uno Platform documentation
- **Best Practices**: Guidance on Uno Platform development patterns
- **API Information**: Details about Uno Platform APIs and features

### How It Works

When you assign an issue or pull request to Copilot in a Uno Platform repository:

1. Copilot automatically connects to the Uno Platform MCP server
2. It can search and fetch Uno Platform documentation as needed
3. It uses Uno-specific knowledge to help implement features correctly

The MCP configuration is defined in `.github/copilot-mcp.json` and requires no additional setup.

## Using the GitHub Copilot Coding Agent with Uno Platform

### Prerequisites

- A GitHub account with Copilot access
- Access to a repository with the Uno Platform (or your own repository with `.github/copilot-mcp.json` configured)

### Steps

1. **Create or open an issue or pull request** on GitHub.com
2. **Assign to Copilot**: Click the "Assign to Copilot" button or add `@copilot` as an assignee
3. **Describe your task**: In a comment, describe what you want Copilot to do:
   - "Add a new button that displays a dialog"
   - "Fix the layout issue in the MainPage.xaml"
   - "Implement data binding for the user profile"
4. **Review the changes**: Copilot will analyze the code, search the Uno Platform documentation via MCP if needed, and propose changes
5. **Iterate**: Provide feedback or additional instructions in follow-up comments

### Example Usage

```markdown
@copilot Please add a ListView with data binding following Uno Platform best practices.
Make sure to use MVUX for state management.
```

Copilot will use the Uno Platform MCP to:

- Search for ListView documentation
- Find MVUX patterns and examples
- Generate code that follows Uno Platform conventions

## Uno App MCP (Local Development)

> [!NOTE]
> The **Uno App MCP** (which provides interactive access to running applications) is designed for local development environments and is not available with GitHub Copilot Coding Agent. It can only be used with local AI agents like:
> 
> - GitHub Copilot CLI
> - VS Code Copilot
> - Claude Desktop
> - Cursor
> - Codex

For interactive app testing during development, use the [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) or [VS Code](xref:Uno.GetStarted.vscode) with the App MCP configured locally.

## Setting Up MCP in Your Own Repository

If you want to enable Uno Platform MCP in your own repository:

1. Create a `.github/copilot-mcp.json` file in your repository:

```json
{
  "$schema": "https://platform.openai.com/docs/mcp/configuration.json",
  "mcpServers": {
    "uno": {
      "type": "sse",
      "url": "https://mcp.platform.uno/v1",
      "metadata": {
        "description": "Uno Platform MCP - Provides Uno Platform documentation search and development guidance"
      }
    }
  }
}
```

2. Commit and push the file to your repository
3. Copilot will automatically use this configuration when assigned to an issue or pull request

## Troubleshooting

### MCP Not Working with GitHub Copilot Coding Agent

- **Verify the configuration file**: Ensure `.github/copilot-mcp.json` exists and is valid JSON
- **Check Copilot access**: Ensure your GitHub account has Copilot enabled
- **Repository permissions**: The repository must have Copilot enabled for the organization/account

### Copilot Not Using Uno Documentation

- Try explicitly mentioning in your comment: "Search the Uno Platform documentation for..."
- Ask Copilot to explain its approach: "What resources are you using to implement this?"

### Need Interactive App Testing

- For features that require running and testing the app, use [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) with the local Uno App MCP instead
- The App MCP provides tools to click, type, screenshot, and interact with your running app

## Next Steps

- Learn more about [Uno Platform MCPs](xref:Uno.Features.Uno.MCPs)
- Set up [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) for local development
- Explore [Building Your App with AI Agents](xref:Uno.BuildYourApp.AI.Agents)
- Troubleshoot issues with [AI Agents](xref:Uno.UI.CommonIssues.AIAgents)

## Additional Resources

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/)
- [Uno Platform Documentation](https://platform.uno/docs/)
