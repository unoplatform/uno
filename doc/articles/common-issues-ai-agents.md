---
uid: Uno.UI.CommonIssues.AIAgents
---

# Issues related to AI Agents

## dnx is not a valid command

The getting started for Claude, Codex, and Copilot CLI use the `dnx` command, which is only available on .NET 10.

If you want to use the App MCP with .NET 9 projects, you'll need to change `dnx` to `uno-devserver` and install it using `dotnet tool install -g uno.devserver`.

## The App MCP does not appear in Visual Studio

1. Open the `.vs/mcp.json` file in Visual Studio. It is located in your solution's root directory.
2. Verify that the Uno MCP server entry is present:

    ```json
    "uno": {
      "url": "https://mcp.platform.uno/v1"
    }
    ```

3. If the entry is missing, add it to the `.vs/mcp.json` file and restart Visual Studio.
4. If the entry exists but the server shows as disconnected, click on "Reconnect" in the file editor:

![Visual Studio MCP reconnect in mcp.json](Assets/vs-mcp-reconnect.png)

The Uno App MCP should now appear in the list of MCPs in Visual Studio.

## The App MCP turns red in Visual Studio 2022/2026

In Visual Studio, the App MCP might turn red on some occasions. To fix this issue, click on the three dots on the right and select `Reload`.

## The uno-app MCP failed to start

The Uno Platform App MCP may fail to start in Claude/Codex/Copilot CLI when it is started in a folder that does not contain an Uno Platform project.

To fix this issue, change directories to a folder that contains the `.sln` or `.slnx` file of your project.

## GitHub Copilot Coding Agent cannot access the App MCP

The Uno Platform App MCP is designed for local development and cannot be used with the GitHub Copilot Coding Agent (Assign to Copilot on GitHub.com). GitHub Copilot Coding Agent only has access to the Remote Uno MCP for documentation and guidance.

If you need to test your application interactively:

1. Pull the pull request branch locally
2. Use [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) or [VS Code Copilot](xref:Uno.GetStarted.vscode) with the App MCP configured
3. Test the changes and provide feedback in the pull request

## MCP not working with GitHub Copilot Coding Agent

If the Uno Platform MCP is not working when using the GitHub Copilot Coding Agent:

1. **Verify the configuration file exists**: Check that `.github/copilot-mcp.json` exists in the repository root
2. **Validate JSON syntax**: Ensure the file is valid JSON with no syntax errors
3. **Check Copilot access**: Verify your GitHub account has Copilot enabled
4. **Try explicit references**: Mention "Search the Uno Platform documentation" in your prompts

For more troubleshooting, see [Get Started with GitHub Copilot Coding Agent](xref:Uno.GetStarted.AI.CopilotCodingAgent).
