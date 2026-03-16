---
uid: Uno.GetStarted.AI.Claude
---

# Get Started with Claude Code

This guide will walk you through the setup process for getting started with Claude Code, from environment checks through full configuration for Uno Platform development.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

Uno Platform provides two MCP (Model Context Protocol) servers that give Claude Code specialized capabilities:

- **Remote MCP** — gives Claude access to up-to-date Uno Platform documentation, search, and best-practice prompts.
- **Local App MCP** — lets Claude interact with your running app: taking screenshots, inspecting the visual tree, clicking elements, and typing text.

The Remote MCP helps you write code that follows Uno Platform conventions. The App MCP confirms that code actually works at runtime.

### Registering MCPs (project-level — recommended)

The recommended approach is to add a `.mcp.json` file at your repository root. This keeps the MCP configuration scoped to the project where it's needed, and the App MCP connects more reliably when registered at the project level since it's always running in the context of your Uno Platform solution.

1. Install [Claude Code](https://code.claude.com/docs/en/overview) from the CLI

1. Create a `.mcp.json` file at your repository root:

    ```json
    {
      "mcpServers": {
        "uno": {
          "type": "http",
          "url": "https://mcp.platform.uno/v1"
        },
        "uno-app": {
          "type": "stdio",
          "command": "dotnet",
          "args": ["dnx", "-y", "uno.devserver", "--mcp-app"]
        }
      }
    }
    ```

1. Open Claude Code from your project directory and run:

    ```bash
    /mcp
    ```

    This will show the Uno Platform MCPs available to the agent. Claude Code will prompt you to approve the servers on first use.

> [!TIP]
> Commit `.mcp.json` to your repo. When teammates clone and open Claude Code, they approve the servers once and everything just works.

### Alternative: user-scoped registration

If you prefer the MCPs to be available across all projects without adding a file to each repo, you can register them at the user level instead. This works well for the Remote MCP (documentation). For the App MCP, project-level registration is still recommended for the most consistent experience.

```bash
claude mcp add --scope user --transport http uno https://mcp.platform.uno/v1
claude mcp add --scope user --transport stdio "uno-app" -- dotnet dnx -y uno.devserver --mcp-app
```

> [!IMPORTANT]
> When using user-scoped registration, the `uno-app` MCP [may fail to load](https://github.com/anthropics/claude-code/issues/4384) unless Claude is opened in a folder containing an Uno Platform app.

## Configuring Claude Code for Uno Platform

MCP registration gives Claude access to documentation and your running app. The configuration files below shape *how* Claude behaves — what it's allowed to do, what conventions it follows, and how it formats code.

### `CLAUDE.md` — behavioral instructions

`CLAUDE.md` is a free-form markdown file that Claude Code reads at the start of every session. It's the single highest-impact configuration file for shaping Claude's output.

Place a global `CLAUDE.md` at `~/.claude/CLAUDE.md` for instructions that apply to all projects (including new projects that don't have their own config yet). Place a project-level `CLAUDE.md` at your repo root for project-specific instructions. Project-level overrides user-level when they conflict.

Here is a recommended starter for Uno Platform development:

```markdown
# Stack

- C# / WinUI 3 / XAML / .NET / Uno Platform
- Architecture: MVUX (recommended; MVVM also supported)
- Styling: Material theme (Uno.Material)

# Framework Rules

- Use `x:Bind` over `{Binding}` — this is a framework-level decision
- Use `dotnet new unoapp -preset recommended` for scaffolding
- Always use the latest stable .NET SDK and Uno.Sdk version
- Search the Uno Platform docs (via MCP) before assuming API patterns
- Never hardcode hex colors — use Material color/brush resources
- Never set explicit font sizes — use Material type scale styles

# Session Workflow

- Commit after each meaningful change with a descriptive message
- For complex features, write a spec to a markdown file first, then start a fresh session to implement from the spec
```

> [!TIP]
> Keep `CLAUDE.md` under 300 lines. Don't duplicate what `.editorconfig` and linters already enforce. Claude is an in-context learner — if your codebase follows a pattern, it picks it up from reading your files. Focus on what Claude *can't* infer: stack identity, scaffolding rules, and framework-level decisions.

### `settings.json` — permissions and hooks

`settings.json` controls what Claude Code is allowed to do: which files it can read or write, which commands it can run, and what gets denied.

Place it at `.claude/settings.json` in your project directory (commit this) or at `~/.claude/settings.json` for global defaults.

For an Uno Platform project, this means pre-approving the `dotnet` CLI and `git` while blocking access to sensitive files. You can also wire up hooks — automated commands that fire after specific actions:

```json
{
  "permissions": {
    "allow": [
      "Bash(dotnet *)",
      "Bash(git *)"
    ],
    "deny": [
      "Bash(rm -rf *)",
      "Bash(git push --force *)"
    ]
  },
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "command": "dotnet format --include $CLAUDE_FILE_PATH",
        "filePattern": "*.cs"
      }
    ]
  }
}
```

The `PostToolUse` hook above runs `dotnet format` every time Claude writes a `.cs` file, keeping output consistent with your `.editorconfig` without you having to ask.

### `settings.local.json` — personal overrides

Same format as `settings.json`, but never committed (Claude Code auto-gitignores it). Use this for machine-specific environment variables, API keys, or experimental permissions. Local settings take precedence over shared settings.

### Putting it all together

Here is the full recommended file structure:

```text
~/.claude/
├── settings.json          # Global permissions and deny rules
└── CLAUDE.md              # Global instructions (stack identity, scaffolding)

my-uno-app/
├── .mcp.json              # MCP server registry (commit this)
├── CLAUDE.md              # Project-specific instructions (commit this)
├── .claude/
│   ├── settings.json      # Project permissions and hooks (commit this)
│   └── settings.local.json  # Personal overrides (never committed)
├── src/
└── MyApp.sln
```

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Claude).
