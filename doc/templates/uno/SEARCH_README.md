# Search Implementation - DocSearch v4 + InstantSearch

This directory contains the implementation of Algolia DocSearch v4 with InstantSearch.js for the Uno Platform documentation.

## Implementation Status

### ✅ Completed (Phase 3 & 4)

1. **DocSearch v4 Migration** (`partials/scripts.tmpl.partial`)
   - Updated from DocSearch v3 to v4
   - Increased `maxResultsPerGroup` to 10
   - Added "See all results" footer component
   - Footer links to dedicated search results page

2. **InstantSearch Results Page** (`/search.md`)
   - Full-page search interface using InstantSearch.js v4
   - Features:
     - Real-time search with URL routing
     - Search statistics
     - Result highlighting
     - Pagination
     - Dark mode support
     - Hierarchical breadcrumbs
     - Responsive design

3. **Modern Template Integration** (`docfx.json`)
   - Updated template array to use DocFX modern template
   - Order: `["default", "modern", "templates/uno"]`
   - Preserves Uno customizations while leveraging modern UI

## Configuration

### Algolia Credentials
- **App ID**: PHB9D8WS99
- **Index**: platform (production)
- **API Key**: 7877394996f96cde1a9b795dce3f7787 (search-only public key)

### Staging Environment (for testing)
- **Index**: platform-staging
- **Website**: https://unoplatformdocstaging.z13.web.core.windows.net

## Usage

### Modal Search (DocSearch)
1. Click the search box in the sidebar
2. Type your query
3. View up to 10 results per group
4. Click "See all results for 'query'" to open full results page

### Full Search Page
1. Navigate to `/search.html` or click footer link
2. View all matching results with pagination
3. Results include:
   - Highlighted matching text
   - Hierarchical breadcrumbs
   - Content snippets
   - Direct links to documentation pages

## Next Steps

### 🔧 Phase 1: Fix Crawler Issues (Staging)
**Priority: High** - Required before production deployment

- [ ] Create `platform-staging` index in Algolia
- [ ] Configure crawler for staging environment
- [ ] Fix issues:
  - Remove duplicate entries
  - Clean up outdated/hidden pages
  - Improve recordExtractor
  - Validate H1/H2/H3 hierarchy
  - Fix URL canonical structure
- [ ] Test on staging: https://unoplatformdocstaging.z13.web.core.windows.net

### 🎯 Phase 2: Improve Ranking & Relevance
- [ ] Configure pageRank in Algolia dashboard
- [ ] Add section priority weighting:
  - Get Started (highest)
  - Tutorials
  - Toolkit
  - API Reference
- [ ] Optimize hierarchy grouping
- [ ] Monitor search analytics

### 🚀 Phase 5: Deploy to Production
- [ ] Validate all changes work on staging
- [ ] Switch staging index config → production
- [ ] Update crawler configuration
- [ ] Monitor search metrics
- [ ] Adjust ranking based on analytics

### 💡 Optional Enhancements
- [ ] **Ask AI**: Evaluate Algolia's AI-powered search
  - Check plan eligibility
  - Assess cost implications
  - Test on staging first
  
- [ ] **Algolia MCP Server**: Set up for debugging
  - Point to platform-staging index
  - Connect to ChatGPT/Claude
  - Use for query analysis

## File Structure

```
doc/
├── search.md                           # Full search results page
├── docfx.json                          # DocFX config with modern template
└── templates/uno/
    └── partials/
        ├── scripts.tmpl.partial        # DocSearch v4 initialization
        └── search-page.tmpl.partial    # InstantSearch partial (backup)
```

## Documentation Links

- [DocSearch v4 API](https://docsearch.algolia.com/docs/api/)
- [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js)
- [DocFX Modern Template](https://dotnet.github.io/docfx/docs/template.html)
- [Issue #1588](https://github.com/unoplatform/uno-private/issues/1588)

## Troubleshooting

### Search modal not appearing
- Check browser console for errors
- Verify `#docsearch` element exists in DOM
- Ensure CDN links are accessible

### InstantSearch page not working
- Verify search.html is generated in _site
- Check Algolia credentials
- Test with browser developer tools network tab
- Verify algoliasearch and instantsearch.js libraries load

### Results not relevant
- Review Phase 1 crawler issues
- Check Algolia dashboard for indexing status
- Verify recordExtractor configuration
- Review ranking configuration in Phase 2
