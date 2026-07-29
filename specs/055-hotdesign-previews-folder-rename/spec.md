# Feature Specification: Align Hot Design Previews Folder MSBuild Property

**Repo**: `uno` (Uno.Sdk)
**Created**: 2026-07-30
**Status**: Documenting current behavior
**Issue**: unoplatform/uno#23907 (related: unoplatform/uno.hotdesign#7408)
**Input**: The Uno SDK still emits the legacy MSBuild property `HotDesignStoriesFolder`, a leftover from the pre-rename "Stories" era. Hot Design (`uno.hotdesign`) has since moved to `HotDesignPreviewsFolder`, and today the combination only works because Hot Design carries a compatibility fallback that bridges the old name to the new one.

---

## Overview

The Uno SDK defines a single MSBuild property that names the folder holding a project's Hot Design design-time content, and excludes that folder from compilation in Release (Optimize) builds. This property is the SDK-facing input that Hot Design consumes to locate an application's Previews folder at runtime.

## Current behavior

Two SDK targets files reference the folder property, both under the legacy `HotDesignStoriesFolder` name:

- `src/Uno.Sdk/targets/Uno.Common.targets` — defines the default:

  ```xml
  <!-- Define the default folder for Hot Design Stories, so they can be excluded in Release -->
  <PropertyGroup Condition="'$(UnoDisableHotDesign)' != 'true'">
    <HotDesignStoriesFolder Condition="'$(HotDesignStoriesFolder)' == ''">HotDesignStories</HotDesignStoriesFolder>
  </PropertyGroup>
  ```

- `src/Uno.Sdk/targets/Uno.DefaultItems.targets` — excludes the folder in Optimize builds:

  ```xml
  <!-- Exclude the Stories folder when in Optimize (aka Release) build configuration -->
  <DefaultItemExcludes Condition=" '$(Optimize)' == 'true' AND '$(HotDesignStoriesFolder)' != '' ">$(DefaultItemExcludes);$(HotDesignStoriesFolder)/**</DefaultItemExcludes>
  ```

## Downstream dependency (Hot Design)

Hot Design's `Uno.UI.HotDesign.props` expects `HotDesignPreviewsFolder`. Because the SDK sets only `HotDesignStoriesFolder`, Hot Design bridges the names:

```xml
<HotDesignPreviewsFolder Condition="'$(HotDesignPreviewsFolder)' == '' AND '$(HotDesignStoriesFolder)' != ''">$(HotDesignStoriesFolder)</HotDesignPreviewsFolder>
```

It then computes an absolute `ApplicationPreviewsFolder` from that value, bakes it into assembly metadata, and reads it at runtime.

## The misalignment

The SDK exposes `HotDesignStoriesFolder`; Hot Design's real contract is `HotDesignPreviewsFolder`. The two only line up because of the bridge. The bridge is scheduled for removal (unoplatform/uno.hotdesign#7408), which makes the SDK-side rename a prerequisite.

This document records the current state; the target state is captured in the following revision.
