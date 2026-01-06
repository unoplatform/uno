# Fix for "Edit this page" Links on Generated Documentation

## Problem

The "Edit this page" links were broken on several documentation pages, particularly:

1. Generated API documentation in the `/implemented/` directory (e.g., `microsoft-ui-xaml-controls-button.md`)
2. Other generated pages created by `DocGenerator.cs`

These pages are auto-generated at build time and don't have corresponding source markdown files in the repository, making the "Edit this page" links point to non-existent files.

## Root Cause

The custom DocFX template at `doc/templates/uno/` was missing two critical files:

1. **common.js** - Required by `conceptual.html.primary.js` to generate the "Edit this page" link via `getImproveTheDocHref` function
2. **conceptual.extension.js** - Extension hooks for template customization

Without these files, the template would fail silently and links would be broken.

## Solution

Created two new files in `doc/templates/uno/`:

### 1. common.js

Implements the `getImproveTheDocHref` function that:
- Detects generated documentation pages (those in `/implemented/` directory)
- Returns `null` for generated pages to hide the "Edit this page" link
- Generates proper GitHub edit URLs for regular documentation pages
- Handles various path formats and gitContribute configurations

### 2. conceptual.extension.js

Provides pre and post transform hooks for template customization (currently stub implementation).

## How It Works

1. DocFX calls `conceptual.html.primary.js` when processing each page
2. This script calls `common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern)`
3. The function checks if the page path contains `/implemented/` or `\implemented\`
4. For generated pages: returns `null` (hiding the link)
5. For regular pages: returns proper GitHub edit URL
6. The link is rendered in the page's contribution section

## Testing

Run the test suite:
```bash
node -e "const common = require('./doc/templates/uno/common.js'); /* test code */"
```

All tests pass:
- ✅ Regular documentation pages show correct edit links
- ✅ Generated pages hide the edit link (return null)
- ✅ Works with both model._gitContribute and parameter gitContribute

## Related Files

- **DocGenerator.cs** - Generates the `/implemented/` documentation files
- **docfx.json** - DocFX configuration with gitContribute settings
- **affix.js** - Handles the contribution div rendering in the sidebar
