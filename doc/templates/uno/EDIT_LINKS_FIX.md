# "Edit this page" links in the Uno DocFX template

This document describes how the Uno-specific DocFX template controls the "Edit this page" link that appears on conceptual and API documentation pages.

## Generated vs. authored pages

Some documentation pages are generated at build time and do not have a corresponding source markdown file in the repository. A common example is the content produced under the `/implemented/` directory (for instance, generated API documentation produced by `DocGenerator.cs`).

For these generated pages, an "Edit this page" link would point to a non-existent file in the repo. The Uno template therefore hides the edit link for generated pages while still showing it for regular authored documentation.

## Template behavior

The Uno DocFX template under `doc/templates/uno/` uses the following components to manage edit links:

- **common.js** &mdash; Exposes the `getImproveTheDocHref(model, gitContribute, gitUrlPattern)` function used by the conceptual template to compute the URL for the "Edit this page" link.
- **conceptual.extension.js** &mdash; Provides extension hooks that allow the template to customize how conceptual models are transformed before rendering.
- **affix.js** &mdash; Handles rendering of the contribution area in the sidebar, including the "Edit this page" link when one is available.

The logic in `common.js`:

- Detects generated documentation pages by checking whether the page path starts with `implemented/` or `articles/implemented/` (supports both `/` and `\` path separators).
- Returns `null` for generated pages so that the contribution UI omits the "Edit this page" link.
- Computes a GitHub edit URL for regular authored pages based on the `model._gitContribute` (or the provided `gitContribute`) settings and the current document path.

When DocFX renders a conceptual page:

1. The `conceptual.html.primary.js` script invokes `common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern)`.
2. If a non-null URL is returned, `affix.js` includes an "Edit this page" link in the contribution section.
3. If `null` is returned (for generated content such as `/implemented/` pages), no edit link is shown.

## Configuration references

The behavior of edit links is influenced by the following files:

- **DocGenerator.cs** &mdash; Generates the `/implemented/` documentation files that are treated as read-only, generated content.
- **docfx.json** &mdash; Provides the `gitContribute` settings and other configuration values used by `common.js` when composing edit URLs.
- **doc/templates/uno/common.js** &mdash; Contains the implementation of `getImproveTheDocHref`.
- **doc/templates/uno/conceptual.extension.js** &mdash; Provides customization hooks used by the conceptual template.
- **doc/templates/uno/component/affix.js** &mdash; Renders the contribution area that may include the edit link.

Use this document as a reference when adjusting edit-link behavior or when modifying the Uno DocFX template.

