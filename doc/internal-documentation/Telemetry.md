# Uno Platform Telemetry Documentation

This document provides a comprehensive overview of telemetry across the Uno Platform ecosystem, including Hot Design, AI Features, Dev Server, Licensing, and IDE Extensions.

## Table of Contents

- [Overview](#overview)
- [Global Telemetry Properties](Telemetry-GlobalProperties.md)
- [Hot Design Telemetry](Telemetry-HotDesign.md)
- [AI Features](Telemetry-AIFeatures.md)
- [Dev Server](Telemetry-DevServer.md)
- [Licensing](Telemetry-Licensing.md)
- [IDE Extensions](Telemetry-IDEExtensions.md)
- [App MCP](Telemetry-AppMCP.md)
- [Privacy & Compliance](Telemetry-Privacy.md)
- [Additional Resources](#additional-resources)

---

## Overview

Telemetry is collected across the Uno Platform ecosystem to understand usage patterns, improve features, and diagnose issues. All telemetry data is collected with privacy in mind, with no personally identifiable information (PII) being transmitted.

All telemetry events use a prefixed naming convention based on the feature area:
- `uno/hot-design` - Hot Design telemetry
- `uno/ai` - AI Features telemetry
- `uno/devserver` - Dev Server telemetry
- `uno/licensing` - Licensing telemetry
- `uno/vscode` - VS Code Extension telemetry
- `uno/rider` - Rider Plugin telemetry
- `uno/visual-studio` - Visual Studio Extension telemetry
- `uno/app-mcp` - App MCP (Model Context Protocol) telemetry

---

## Telemetry Categories

### [Global Telemetry Properties](Telemetry-GlobalProperties.md)

Common properties automatically included with telemetry events across all Uno Platform components:
- **System Information** - OS, version, architecture, kernel version, timestamp
- **Environment Information** - Culture, CI detection, CI provider
- **Version Information** - Uno Platform version, SDK version, target frameworks
- **IDE and Tooling** - IDE type, version, plugin version
- **Application Information** - Session ID, anonymous user ID, machine ID
- **Build Information** - Project type, debug mode, working directory (hashed)

All properties follow strict privacy principles with hashing/anonymization of sensitive data.

[View detailed Global Properties documentation →](Telemetry-GlobalProperties.md)

### [Hot Design Telemetry](Telemetry-HotDesign.md)

**Event Name Prefix:** `uno/hot-design`

Hot Design telemetry tracks server-side sessions and client-initiated analytics events across 60+ client events organized into 8 categories:
- Session and Lifecycle
- Licensing
- UI Controls
- Property Grid
- Undo/Redo
- Toolbox and Elements
- Chat and XAML Generation
- Search

[View detailed Hot Design telemetry documentation →](Telemetry-HotDesign.md)

### [AI Features](Telemetry-AIFeatures.md)

**Event Name Prefix:** `uno/ai`

AI Features use Application Insights with custom telemetry initializers to track design threads and operations, including context fields for design thread IDs, operation phases, and loop iterations.

[View detailed AI Features telemetry documentation →](Telemetry-AIFeatures.md)

### [Dev Server](Telemetry-DevServer.md)

**Event Name Prefix:** `uno/devserver`

Dev Server telemetry tracks server sessions (Root and Connection types) and client connections, including comprehensive app launch tracking with platform, IDE, and plugin information.

[View detailed Dev Server telemetry documentation →](Telemetry-DevServer.md)

### [Licensing](Telemetry-Licensing.md)

**Event Name Prefix:** `uno/licensing`

Licensing telemetry tracks 43 events covering:
- License Manager Events (Client)
- Navigation Events
- License API Events (Server)
- DevServer Licensing Events

[View detailed Licensing telemetry documentation →](Telemetry-Licensing.md)

### [IDE Extensions](Telemetry-IDEExtensions.md)

IDE extensions track extension lifecycle, user interactions, and dev server operations across:
- **Visual Studio Code** (`uno/vscode`) - 6 events
- **Rider** (`uno/rider`) - 10 events
- **Visual Studio** (`uno/visual-studio`) - 8 events

[View detailed IDE Extensions telemetry documentation →](Telemetry-IDEExtensions.md)

### [App MCP](Telemetry-AppMCP.md)

**Event Name Prefix:** `uno/app-mcp`

App MCP (Model Context Protocol) telemetry tracks agent interactions with running Uno applications:
- **Community License Tools** - 8 tools (runtime info, screenshot, pointer click, key press, type text, visual tree snapshot, element peer default action, close)
- **Pro License Tools** - 2 tools (element peer action for specific peer actions, element DataContext)
- **Session Tracking** - Agent type, platform, tools used
- **Error Tracking** - Connection errors, tool execution errors, timeouts
- **Agent Interaction Metrics** - Tool invocation counts, success rates, session durations

[View detailed App MCP telemetry documentation →](Telemetry-AppMCP.md)

### [Privacy & Compliance](Telemetry-Privacy.md)

Comprehensive privacy and compliance information including:
- GDPR Compliance
- Data Collection Policy (what IS and is NOT collected)
- Disabling Telemetry (instructions for all IDEs)
- Data Retention
- Instrumentation Keys
- Contact Information

[View detailed Privacy & Compliance documentation →](Telemetry-Privacy.md)

---

## Additional Resources

### Documentation Links

- **Hot Design Telemetry**: [uno.hotdesign/internal-documentation/Telemetry.md](https://github.com/unoplatform/uno.hotdesign/blob/main/internal-documentation/Telemetry.md)
- **VS Code Extension Telemetry**: [uno.vscode/documentation/Telemetry.md](https://github.com/unoplatform/uno.vscode/blob/main/documentation/Telemetry.md)
- **Rider Plugin Telemetry**: [uno.rider/Telemetry/Telemetry.md](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/Telemetry/Telemetry.md)
- **Visual Studio Extension Telemetry**: [uno.studio/docs/Telemetry.md](https://github.com/unoplatform/uno.studio/blob/main/docs/Telemetry.md)
- **Uno Licensing API**: [unoplatform/uno.licensing](https://github.com/unoplatform/uno.licensing)
- **Uno AI Features**: [unoplatform/uno.ai-private](https://github.com/unoplatform/uno.ai-private)
- **Uno App MCP**: [unoplatform/uno.app-mcp](https://github.com/unoplatform/uno.app-mcp)

### Repository Search Links

- [Hot Design Telemetry (unoplatform/uno.hotdesign)](https://github.com/unoplatform/uno.hotdesign/search?q=telemetry)
- [VS Code Extension (unoplatform/uno.vscode)](https://github.com/unoplatform/uno.vscode/search?q=telemetry)
- [Rider Plugin (unoplatform/uno.rider)](https://github.com/unoplatform/uno.rider/search?q=telemetry)
- [Visual Studio Extension (unoplatform/uno.studio)](https://github.com/unoplatform/uno.studio/search?q=telemetry)
- [Licensing (unoplatform/uno.licensing)](https://github.com/unoplatform/uno.licensing/search?q=telemetry)
- [Dev Server (unoplatform/uno)](https://github.com/unoplatform/uno/search?q=devserver+telemetry)
- [App MCP (unoplatform/uno.app-mcp)](https://github.com/unoplatform/uno.app-mcp/search?q=telemetry)

---

**Note**: Due to the private nature of some repositories, search results may be limited based on your access permissions. For full access to telemetry documentation, ensure you have the necessary permissions to the relevant repositories.
