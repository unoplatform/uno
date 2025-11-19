---
uid: Uno.Features.Uno.MCPs
---

# The Uno Platform MCPs

Uno Platform provides two [MCPs](https://modelcontextprotocol.io/docs/getting-started/intro):

- The Uno Platform Remote MCP, providing prompts and up-to-date documentation
- The Uno Platform Local App MCP, providing interactive access to your running application

This document explains how to interact with both those MCPs. You can find further below descriptions of the provided tools and prompts.

## MCP (Remote)

This is a remotely hosted publicly and provides:

- A set of tools to search and fetch Uno Platform documentation
- A set of prompts to create and develop Uno Platform applications.

### Predefined Prompts

The prompts provided by the MCP are automatically registered in your environment when supported.

Here are the currently supported prompts:

- `/new`, used to create a new uno app with the best practices in mind.
- `/init`, used to "prime" your current chat with uno's best practices. It's generally used in an existing app when adding new features.

### Sample Prompts for Uno MCP Servers

You can find common prompts to use with agents in our [getting started](xref:Uno.BuildYourApp.AI.Agents) section.

### Uno MCP Tools

The Uno MCP tools are the following:

- `uno_platform_docs_search` used by Agents to search for specific topics. It returns snippets of relevant information.
- `uno_platform_docs_fetch` used by Agents to get a specific document, grabbed through `uno_platform_docs_search`.
- `uno_platform_agent_rules_init` used by Agents to "prime" the environment on how to interact with Uno Platform apps during development.
- `uno_platform_usage_rules_init` used by Agents to "prime" the environment on how to Uno Platform's APIs in the best way possible

Those tools are suggested to the agent on how to be used best. In general, asking the agent "Make sure to search the Uno Platform docs to answer" will hint it to use those tools.

> [!NOTE]
> You can unselect `uno_platform_agent_rules_init` and `uno_platform_usage_rules_init` in your agent to avoid implicit priming, and you can use the `/init` prompt to achieve a similar result.

## App MCP (Local)

This MCP is running locally and provides agents with the ability to interact with a running app, in order to click, type, analyze or screenshot its content.

These tools give "eyes" and "hands" to Agents in order to validate their assumptions regarding the actions they take, and the code they generate.

### App MCP Tools

The Community license MCP app tools are:

- `uno_app_get_runtime_info`, used to get general information about the running app, such as its PID, OS, Platform, etc...
- `uno_app_get_screenshot`, used to get a screenshot of the running app
- `uno_app_pointer_click`, used to click at an X,Y coordinates in the app
- `uno_app_key_press`, used to type individual keys (possibly with modifiers)
- `uno_app_type_text`, used to type long strings of text in controls
- `uno_app_visualtree_snapshot`, used to get a textual representation of the visual tree of the app
- `uno_app_element_peer_default_action`, used to execute the default automation peer action on a UI element
- `uno_app_close`, used to close the running app

The Pro license App MCP app tools are:

- `uno_app_element_peer_action`, used to invoke a specific element automation peer action
- `uno_app_get_element_datacontext`, used to get a textual representation of the DataContext on a FrameworkElement

## Troubleshooting MCP Servers

You can find additional information about [troubleshooting AI Agents](xref:Uno.UI.CommonIssues.AIAgents) in our docs.