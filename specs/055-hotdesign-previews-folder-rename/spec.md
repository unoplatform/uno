# Feature Specification: Align Hot Design Previews Folder MSBuild Property

**Repo**: `uno` (Uno.Sdk)
**Created**: 2026-07-30
**Status**: Approved — target design
**Issue**: unoplatform/uno#23907 (related: unoplatform/uno.hotdesign#7408)
**Input**: The Uno SDK emits the legacy MSBuild property `HotDesignStoriesFolder`, a leftover from the pre-rename "Stories" era. Hot Design (`uno.hotdesign`) has moved to `HotDesignPreviewsFolder` and today only works via a compatibility bridge. This spec aligns the SDK-facing name to `HotDesignPreviewsFolder`.

---

## Overview

The Uno SDK defines a single MSBuild property that names the folder holding a project's Hot Design design-time content, and excludes that folder from compilation in Release (Optimize) builds. This property is the SDK-facing input that Hot Design consumes to locate an application's Previews folder at runtime.

## Target (aligned) behavior

Both SDK targets files reference the folder property under the aligned `HotDesignPreviewsFolder` name, with a default of `HotDesignPreviews`:

- `src/Uno.Sdk/targets/Uno.Common.targets` — defines the default:

  ```xml
  <!-- Define the default folder for Hot Design previews, so they can be excluded in Release -->
  <PropertyGroup Condition="'$(UnoDisableHotDesign)' != 'true'">
    <HotDesignPreviewsFolder Condition="'$(HotDesignPreviewsFolder)' == ''">HotDesignPreviews</HotDesignPreviewsFolder>
  </PropertyGroup>
  ```

- `src/Uno.Sdk/targets/Uno.DefaultItems.targets` — excludes the folder in Optimize builds:

  ```xml
  <!-- Exclude the previews folder when in Optimize (aka Release) build configuration -->
  <DefaultItemExcludes Condition=" '$(Optimize)' == 'true' AND '$(HotDesignPreviewsFolder)' != '' ">$(DefaultItemExcludes);$(HotDesignPreviewsFolder)/**</DefaultItemExcludes>
  ```

## Downstream dependency (Hot Design)

With the SDK setting `HotDesignPreviewsFolder` directly, Hot Design's `Uno.UI.HotDesign.props` no longer needs its compatibility bridge (removed in unoplatform/uno.hotdesign#7408). Hot Design continues to compute the internal absolute `ApplicationPreviewsFolder` from `HotDesignPreviewsFolder`, bake it into assembly metadata, and read it at runtime — that contract is unchanged.

## Decision: hard rename, no fallback

The rename is a **hard rename**: the SDK stops emitting `HotDesignStoriesFolder` entirely rather than keeping a permanent alias. This removes the dual-name ambiguity at the source. The consequence is a coordinated, breaking change (see below), accepted deliberately over carrying a long-lived compatibility alias.

## Breaking change and migration

- Projects that explicitly set `HotDesignStoriesFolder` must rename the property to `HotDesignPreviewsFolder`.
- Apps relying on the default folder name will see the auto-excluded folder change from `HotDesignStories` to `HotDesignPreviews`. Rename the folder, or set `HotDesignPreviewsFolder` explicitly, to keep it excluded from Release builds.

## Coordination

The SDK change ships **first**. Hot Design's bridge removal (unoplatform/uno.hotdesign#7408) only becomes safe once an aligned SDK is in use; until then Hot Design's bridge keeps older SDKs working.

## Changes in this repo

- `src/Uno.Sdk/targets/Uno.Common.targets` — rename `HotDesignStoriesFolder` → `HotDesignPreviewsFolder`; default `HotDesignStories` → `HotDesignPreviews`.
- `src/Uno.Sdk/targets/Uno.DefaultItems.targets` — rename the property reference in the Optimize-build `DefaultItemExcludes`.
