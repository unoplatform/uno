# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Uno Platform is an open-source cross-platform framework for building .NET applications from a single codebase using the WinUI 3 API. It targets Web (WebAssembly), Desktop (Windows, macOS, Linux via Skia), Mobile (iOS, Android), and Embedded systems.

## Agent instructions

Import **[.github/copilot-instructions.md](.github/copilot-instructions.md)** for detailed agent instructions.

## Specialized Agents

For specific tasks, these focused agents provide deeper guidance:

| Agent | File | Use For |
|-------|------|---------|
| DependencyProperty | `.github/agents/dependency-property-agent.md` | Adding/modifying DependencyProperties |
| Source Generators | `.github/agents/source-generators-agent.md` | XAML/DependencyObject generator work |
| Runtime Tests | `.github/agents/runtime-tests-agent.md` | Creating and running runtime tests |
| WinUI Porting | `.github/agents/winui-porting-agent.md` | Porting WinUI C++ code to C# |
