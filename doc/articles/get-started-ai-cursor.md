---
uid: Uno.GetStarted.AI.Cursor
---

# Get Started with Cursor

This guide will walk you through the setup process for getting started with Cursor.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Cursor

Install [Cursor](https://cursor.com/docs).

> [!NOTE]
> The Uno Platform extension is not functional in Cursor at this time.

There is nothing specific to set up for Cursor itself. Optionally install the Uno Platform Skills below, then create your first app and use the Uno MCPs.

## Setting up the Uno Platform Skills

Uno Platform ships a catalog of agent skills covering areas such as MVUX, navigation, Uno Toolkit controls, theming, and UI testing. Cursor doesn't support the plugin format at this time, so copy the skills into Cursor's skills directory instead:

```bash
git clone https://github.com/unoplatform/studio.git
mkdir -p ~/.cursor/skills
cp -r studio/skills/uno-* ~/.cursor/skills/
```

To make the skills available in a single project only, copy them to `.cursor/skills/` at the project root instead.

Once installed, Cursor automatically selects the relevant skills as it works on your prompts. For the full skill catalog and other installation options, see [Skills & Plugins](xref:Uno.PlatformStudio.Skills).

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Cursor).

## Uno TechBite

Getting Started with Uno Platform & Cursor IDE - Complete Setup Guide:
> [!Video https://www.youtube-nocookie.com/embed/H-7WcTKAY3s]
