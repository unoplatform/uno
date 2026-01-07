---
uid: Uno.GettingStarted.CreateAnApp.AI.CopilotCodingAgent
---

# Creating an app with the GitHub Copilot Coding Agent

This guide explains how to use the GitHub Copilot Codingt Agent (Assign to Copilot) to create and develop Uno Platform applications directly on GitHub.com.

## Prerequisites

Before you begin, ensure you have:

- A GitHub account with Copilot access enabled
- Basic familiarity with GitHub issues and pull requests
- (Optional) An existing Uno Platform project, or follow the steps below to create one

## Creating a New Uno Platform Project wit the GitHub Copilot Coding Agent

### Option 1: Using GitHub Copilot Coding Agent

1. **Create a repository** on GitHub.com (or use an existing one)
2. **Create an issue** describing what you want to build:

   ```Markdown
   Title: Create a new Uno Platform app
   
   Description:
   Create a new Uno Platform application using the latest template.
   Target .NET 10.0 and include WebAssembly and Desktop support.
   Set up a basic MainPage with a welcome message.
   ```

3. **Create a branch** from the issue or create a new branch manually
4. **Create a pull request** and assign it to Copilot (`@copilot`)
5. **Give instructions** in a pull request comments:

   ```Markdown
   @copilot Please create a new Uno Platform app following the project structure best practices.
   Use the unoapp template targeting net10.0.
   ```

Copilot will generate the necessary project files and structure.

### Option 2: Create Locally First (Recommended)

For initial project setup, it's often easier to create the project locally:

1. Use the [Uno Platform Live Wizard](https://aka.platform.uno/app-wizard) or [`dotnet new`](xref:Uno.GetStarted.dotnet-new):

   ```bash
   dotnet new unoapp --tfm net10.0 -o MyNewApp
   ```

2. Push the project to GitHub:

   ```bash
   cd MyNewApp
   git init
   git add .
   git commit -m "feat: Initial Uno Platform project"
   git remote add origin https://github.com/yourusername/MyNewApp.git
   git push -u origin main
   ```

3. Now you can use the GitHub Copilot Coding Agent for feature development

## Developing Features with the GitHub Copilot Coding Agent

Once you have a project set up:

1. **Create a feature branch** for your work
2. **Create a an issue or pull request** describing the feature
3. **Assign to Copilot** and provide clear instructions

### Example Feature Requests

**Adding a new page:**

```markdown
@copilot Please add a new SettingsPage.xaml with the following features:
- A toggle switch for dark mode
- A text box for user name input
- Save button that persists settings
- Use MVUX for state management
Follow Uno Platform best practices for responsive layout.
```

**Implementing data binding:**

```markdown
@copilot Implement a user profile view with data binding:
- Create a User model with Name, Email, and Avatar properties
- Create a ViewModel using MVUX
- Add a Profile page that displays the user information
- Include an edit mode with input validation
```

**Adding UI controls:**

```markdown
@copilot Add a ListView to MainPage that displays a list of items:
- Use Uno Toolkit CardControl for each item
- Implement pull-to-refresh
- Add item click navigation
- Follow Material design guidelines
```

## Best Practices for Working with the GitHub Copilot Coding Agent

### 1. Be Specific

Good:

```markdown
@copilot Add a Button with a red background and white text that displays a dialog when clicked.
Use ThemeResource colors where possible.
```

Less effective:

```markdown
@copilot Add a button
```

### 2. Reference Uno Platform Concepts

Copilot has access to Uno Platform documentation via MCP. Reference specific features:

```markdown
@copilot Implement navigation using Uno Extensions Navigation.
Use the Region-based approach with TabBar.
```

### 3. Iterate on Feedback

If the initial implementation isn't quite right:

```markdown
@copilot The ListView is working, but the items are too close together.
Please add spacing between items and increase the card elevation.
```

### 4. Ask for Documentation References

```markdown
@copilot Before implementing, please search the Uno Platform docs for best practices
on implementing authentication with MSAL, then implement it.
```

## Working with Platform-specific Code

GitHub Copilot Coding Agent can help with platform-specific implementations:

```markdown
@copilot Add platform-specific code to access the device camera:
- Use conditional compilation for Android, iOS, and WebAssembly
- Follow Uno Platform patterns for platform-specific features
- Add appropriate permissions for Android and iOS
```

## Testing and Validation

While the GitHub Copilot Coding Agent can generate and modify code, it cannot run or test your application. For interactive testing:

1. **Pull the PR branch locally**:

   ```bash
   git fetch origin pull/123/head:pr-123
   git checkout pr-123
   ```

2. **Run and test locally** using your preferred IDE or [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI)

3. **Provide feedback** in the pull request with test results:

   ```markdown
   @copilot The button click handler is working, but the dialog doesn't show.
   The console shows an error: "XamlParseException on line 45".
   Please fix the XAML syntax.
   ```

## Limitations of GitHub Copilot Coding Agent

GitHub Copilot Coding Agent is excellent for:

- ✅ Generating code and project structure
- ✅ Implementing features based on specifications
- ✅ Refactoring and code improvements
- ✅ Documentation and comments
- ✅ Following Uno Platform patterns (via MCP)

However, it cannot:

- ❌ Run your application
- ❌ Take screenshots or interact with UI
- ❌ Test the application behavior
- ❌ Access local resources or file system

For interactive development and testing, use [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) or [VS Code Copilot](xref:Uno.GetStarted.vscode) with the [Uno App MCP](xref:Uno.Features.Uno.MCPs) configured locally.

## Next Steps

- Learn about [Uno Platform MCPs](xref:Uno.Features.Uno.MCPs)
- Set up [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI) for local development with App MCP
- Explore [Building Your App with AI Agents](xref:Uno.BuildYourApp.AI.Agents)
- Review [common issues with AI Agents](xref:Uno.UI.CommonIssues.AIAgents)

## Related Documentation

- [Get Started with GitHub Copilot Coding Agent](xref:Uno.GetStarted.AI.CopilotCodingAgent)
- [Uno Platform MCP Features](xref:Uno.Features.Uno.MCPs)
- [Creating apps with other AI tools](xref:Uno.BuildYourApp.AI.Agents)
