# Algolia Configuration Guide for Uno Platform Documentation

This guide provides step-by-step instructions for configuring Algolia search to fix the issues identified in [Issue #1588](https://github.com/unoplatform/uno-private/issues/1588).

**Target audience:** Team member with Algolia dashboard access  
**Prerequisites:** Login credentials for Algolia dashboard at https://www.algolia.com/

---

## Overview of Issues to Fix

Current problems with search (as per issue #1588):
- ✗ Duplicate entries
- ✗ Missing results for key terms
- ✗ Outdated/hidden pages being indexed
- ✗ Poor ranking (important pages not appearing first)
- ✗ Limited results visibility

**Expected outcome:** Search experience similar to [Avalonia documentation](https://docs.avaloniaui.net/)

---

## Phase 1: Fix Crawler Configuration (CRITICAL)

### 1.1 Create Staging Index

**Location:** Algolia Dashboard → Indices

1. Click **"Create Index"**
2. Name: `platform-staging`
3. Copy settings from `platform` index
4. Click **"Create"**

**Purpose:** Test changes safely before affecting production.

### 1.2 Configure Crawler for Staging

**Location:** Algolia Crawler Dashboard → https://crawler.algolia.com/

#### Create New Crawler Configuration

1. Go to **Crawlers** → Click **"New Crawler"**
2. **Name:** `uno-docs-staging`
3. **Index:** `platform-staging`

#### Configure Start URLs

```json
{
  "startUrls": [
    {
      "url": "https://unoplatformdocstaging.z13.web.core.windows.net/articles/intro.html",
      "tags": ["docs"],
      "selectors_key": "docs"
    }
  ],
  "sitemaps": [
    "https://unoplatformdocstaging.z13.web.core.windows.net/sitemap.xml"
  ]
}
```

#### Configure Stop URLs (Exclude from Indexing)

Add these patterns to **exclude outdated, hidden, or duplicate content**:

```json
{
  "stopUrls": [
    ".*/_site/.*",
    ".*/obj/.*",
    ".*/bin/.*",
    ".*/includes/.*",
    ".*\\.json$",
    ".*\\.xml$",
    ".*/deprecated/.*",
    ".*/obsolete/.*",
    ".*#.*#.*",
    ".*/toc\\.html$"
  ]
}
```

**Why:** These patterns prevent indexing of:
- Build artifacts (`_site/`, `obj/`, `bin/`)
- Include files (reusable content fragments)
- Deprecated documentation
- TOC pages (table of contents - already indexed elsewhere)

#### Configure Selectors (MOST IMPORTANT)

Replace default selectors with this configuration to fix duplicate hierarchy issues:

```json
{
  "selectors": {
    "default": {
      "lvl0": {
        "selector": ".breadcrumb li:nth-last-child(2) a",
        "defaultValue": "Documentation"
      },
      "lvl1": "article h1",
      "lvl2": "article h2",
      "lvl3": "article h3",
      "lvl4": "article h4",
      "lvl5": "article h5",
      "text": "article p, article li, article td"
    },
    "docs": {
      "lvl0": {
        "selector": ".breadcrumb li:nth-last-child(2) a",
        "defaultValue": "Documentation"
      },
      "lvl1": "article h1",
      "lvl2": "article h2",
      "lvl3": "article h3", 
      "lvl4": "article h4",
      "lvl5": "article h5",
      "text": "article p, article li, article td"
    }
  },
  "selectors_exclude": [
    "nav",
    "footer",
    ".navbar",
    ".sidetoc",
    ".affix",
    ".breadcrumb",
    ".sidefilter",
    ".no-search",
    ".code-header",
    ".copy-button",
    "#search-container",
    "#docsearch",
    ".search-results",
    ".contribution",
    ".edit-page"
  ]
}
```

**Why these selectors matter:**
- **lvl0**: Top-level category from breadcrumbs (not repeated nav)
- **lvl1-5**: Page headings in proper hierarchy
- **text**: Paragraph and list content
- **selectors_exclude**: Prevents indexing navigation, footers, and UI chrome (major source of duplicates)

#### Configure Additional Settings

```json
{
  "js_render": true,
  "js_wait": 2,
  "user_agent": "Algolia Crawler",
  "nb_hits_max": 50000,
  "schedule": "at 02:00 every day",
  "max_depth": 10
}
```

**Settings explained:**
- `js_render: true` - Required for modern DocFX template
- `js_wait: 2` - Wait 2 seconds for JavaScript to render
- `schedule` - Run daily at 2 AM
- `max_depth: 10` - Crawl up to 10 levels deep

#### Save and Run Test Crawl

1. Click **"Save"**
2. Click **"Run Test"**
3. Review **Crawler Logs** for errors
4. Check **`platform-staging` index** for results
5. Verify no duplicates by searching for common terms

**Expected results:**
- ~5,000-15,000 records (varies by doc size)
- No duplicate URLs
- Clear hierarchy in results
- All major sections represented

### 1.3 Validate H1/H2/H3 Hierarchy

**Location:** Algolia Dashboard → Indices → `platform-staging` → Browse

**Check for these issues:**

1. **Multiple H1s on Same Page**
   - Search for duplicate `objectID`s
   - Each page should have ONE primary H1
   - Fix: Update doc pages to use single H1

2. **Skipped Heading Levels**
   - H1 → H3 (missing H2)
   - Creates poor hierarchy
   - Fix: Ensure sequential heading levels in docs

3. **Empty Headings**
   - Headings with no text
   - Creates noise in search
   - Fix: Remove or populate empty headings

**Validation query:**
```sql
-- In Algolia Browse, filter by:
hierarchy.lvl1: ""
-- This shows pages with missing H1s
```

---

## Phase 2: Improve Ranking & Relevance

### 2.1 Configure Custom Ranking

**Location:** Algolia Dashboard → Indices → `platform-staging` → Configuration → Ranking and Sorting

#### Set Ranking Formula

Replace default ranking with:

```
1. Typo
2. Geo
3. Words
4. Filters
5. Proximity
6. Attribute
7. Exact
8. Custom (add these ↓)
```

**Add Custom Ranking Attributes (in order):**

```json
[
  "desc(weight.page_rank)",
  "desc(weight.level)",
  "asc(weight.position)"
]
```

**What this does:**
- `weight.page_rank` - Higher value = more important page
- `weight.level` - Prefer higher-level headings (H1 > H2 > H3)
- `weight.position` - Prefer content earlier in page

### 2.2 Configure Searchable Attributes

**Location:** Configuration → Searchable attributes

**Set priority order:**

```json
[
  "unordered(hierarchy.lvl0)",
  "unordered(hierarchy.lvl1)", 
  "unordered(hierarchy.lvl2)",
  "unordered(hierarchy.lvl3)",
  "unordered(hierarchy.lvl4)",
  "unordered(hierarchy.lvl5)",
  "content"
]
```

**What `unordered()` means:**
- Words can match in any order
- Example: "button click" matches "click button"
- Better for natural language queries

### 2.3 Set Attributes for Faceting

**Location:** Configuration → Facets

Add these faceting attributes:

```json
[
  "searchable(hierarchy.lvl0)",
  "searchable(hierarchy.lvl1)",
  "type",
  "language",
  "version"
]
```

**Why:** Enables category filtering in search UI (already implemented in `/search.html`)

### 2.4 Add Page Rank Weighting

You need to add custom metadata to documentation pages. This requires updating docs in the repository.

**High Priority Pages (add to frontmatter):**

#### Get Started Pages
```markdown
---
uid: Uno.GetStarted
_weight: 100
---
```

#### Tutorial Pages
```markdown
---
uid: Uno.Tutorials.FirstApp
_weight: 90
---
```

#### Feature/Control Pages
```markdown
---
uid: Uno.Features.Button
_weight: 80
---
```

#### API Reference
```markdown
---
uid: Uno.API.UIElement
_weight: 70
---
```

**Note:** Once metadata is added, the crawler will pick up `_weight` and use it for `weight.page_rank` in custom ranking.

---

## Phase 3: Add Synonyms

**Location:** Algolia Dashboard → Indices → `platform-staging` → Configuration → Synonyms

### Create Synonym Groups

Click **"Add Synonyms"** and create these groups:

#### Framework Synonyms
```json
{
  "objectID": "uno-framework",
  "type": "synonym",
  "synonyms": ["uno", "uno platform", "unoplatform"]
}
```

```json
{
  "objectID": "xaml",
  "type": "synonym", 
  "synonyms": ["xaml", "xml"]
}
```

```json
{
  "objectID": "wasm",
  "type": "synonym",
  "synonyms": ["wasm", "webassembly", "web assembly"]
}
```

#### Platform Synonyms
```json
{
  "objectID": "ios",
  "type": "synonym",
  "synonyms": ["ios", "apple", "iphone", "ipad"]
}
```

```json
{
  "objectID": "android",
  "type": "synonym",
  "synonyms": ["android", "google"]
}
```

```json
{
  "objectID": "windows",
  "type": "synonym",
  "synonyms": ["windows", "win", "uwp", "winui", "win11", "win10"]
}
```

```json
{
  "objectID": "macos",
  "type": "synonym",
  "synonyms": ["macos", "mac", "osx", "catalina", "monterey"]
}
```

```json
{
  "objectID": "linux",
  "type": "synonym",
  "synonyms": ["linux", "gtk", "ubuntu"]
}
```

#### Common Typos
```json
{
  "objectID": "button-typo",
  "type": "synonym",
  "synonyms": ["button", "buttom", "buton"]
}
```

```json
{
  "objectID": "textblock-typo",
  "type": "synonym",
  "synonyms": ["textblock", "textblok", "text block"]
}
```

```json
{
  "objectID": "databinding",
  "type": "synonym",
  "synonyms": ["databinding", "data binding", "data-binding"]
}
```

#### Technical Terms
```json
{
  "objectID": "ui",
  "type": "synonym",
  "synonyms": ["ui", "user interface"]
}
```

```json
{
  "objectID": "mvvm",
  "type": "synonym",
  "synonyms": ["mvvm", "model view viewmodel", "model-view-viewmodel"]
}
```

```json
{
  "objectID": "nuget",
  "type": "synonym",
  "synonyms": ["nuget", "package manager", "packages"]
}
```

**How to add via Dashboard:**
1. Go to Configuration → Synonyms
2. Click "Add Synonyms"
3. Select "Synonym"
4. Paste the JSON (without objectID - it auto-generates)
5. Click "Save"

**Or bulk import via API/JSON:**
Save all synonym JSON objects to a file and import via dashboard's "Import" button.

---

## Phase 4: Configure Typo Tolerance

**Location:** Configuration → Typo tolerance

### Set Typo Tolerance Rules

```json
{
  "minWordSizefor1Typo": 4,
  "minWordSizefor2Typos": 8,
  "typoTolerance": true
}
```

**What this means:**
- Words with 4+ characters: Allow 1 typo
- Words with 8+ characters: Allow 2 typos
- "buttom" → matches "button"
- "documentatin" → matches "documentation"

### Disable Typo Tolerance for Technical Terms

**Location:** Configuration → Typo tolerance → Disable typo tolerance on words

Add these words (case-sensitive):

```
iOS
XAML
WASM
API
SDK
CLI
HTML
CSS
JSON
XML
HTTP
HTTPS
URI
URL
MVVM
```

**Why:** These terms should match exactly. "IOS" shouldn't match "iOS", "XAML" shouldn't match "XML".

---

## Phase 5: Configure Stop Words

**Location:** Configuration → Stop words

### Add English Stop Words

Enable built-in English stop words:
- Click "Add Language"
- Select "English"
- Click "Add"

### Add Custom Stop Words

```
uno's
platform's
using
using's
```

**Why:** Common words that don't help search relevance.

---

## Phase 6: Optimize Performance

### 6.1 Set Unretrievable Attributes

**Location:** Configuration → Attributes → Unretrievable attributes

Add these to reduce API payload size:

```json
[
  "_tags",
  "content_camel",
  "content_raw",
  "html",
  "full_content"
]
```

**Why:** These attributes aren't displayed in search results, so no need to retrieve them.

### 6.2 Set Attributes to Retrieve

**Location:** Configuration → Attributes → Attributes to retrieve

```json
[
  "hierarchy",
  "content",
  "url",
  "anchor",
  "type",
  "_snippetResult",
  "_highlightResult"
]
```

**Why:** Only retrieve what's needed for display = faster searches.

---

## Phase 7: Testing & Validation

### Test on Staging

**Before promoting to production, validate:**

1. **Search for Common Terms**
   - "getting started" → Should show Get Started guide first
   - "button" → Should show Button control docs
   - "xaml" → Should show XAML-related content
   - "installation" → Should show installation guides

2. **Check for Duplicates**
   - Search for any page title
   - Should only see ONE result per page
   - If duplicates exist, review crawler `selectors_exclude`

3. **Verify No Outdated Content**
   - Search for deprecated features
   - Should NOT appear in results
   - If they do, add to crawler `stopUrls`

4. **Test Typo Tolerance**
   - Search "buttom" → Should show "button" results
   - Search "textblok" → Should show "textblock" results

5. **Test Synonyms**
   - Search "wasm" → Should match "webassembly" docs
   - Search "uwp" → Should match "windows" docs

6. **Check Mobile**
   - Open on phone/tablet
   - Search should be responsive
   - Filters should work

### Validation Checklist

- [ ] Zero duplicate entries for same URL
- [ ] Get Started pages rank first for "getting started"
- [ ] Typos return relevant results
- [ ] Synonyms work (test 3-5)
- [ ] No outdated pages in results
- [ ] Mobile search works
- [ ] Category filters work in `/search.html`
- [ ] "See all results" footer appears in modal
- [ ] Analytics tracking works (check browser console)

---

## Phase 8: Deploy to Production

### 8.1 Copy Staging Config to Production

**Once staging is validated:**

1. **Copy Crawler Configuration**
   - Go to Crawler Dashboard
   - Duplicate `uno-docs-staging` crawler
   - Rename to `uno-docs-production`
   - Change:
     - Start URL: `https://platform.uno/docs/articles/intro.html`
     - Sitemap: `https://platform.uno/docs/sitemap.xml`
     - Index: `platform` (production)
   - Save

2. **Copy Index Configuration**
   - Go to Algolia Dashboard → `platform-staging` → Configuration
   - Click each section and copy settings to `platform` index:
     - Ranking and Sorting
     - Searchable attributes
     - Facets
     - Synonyms
     - Typo tolerance
     - Stop words
     - Attributes to retrieve

3. **Run Production Crawl**
   - Go to Crawler Dashboard → `uno-docs-production`
   - Click "Run Crawler"
   - Monitor progress
   - Validate results in `platform` index

### 8.2 Monitor Production

**For first 2 weeks after launch:**

1. **Daily:** Check Algolia Analytics Dashboard
   - Top searches
   - No-result searches (documentation gaps)
   - Click-through rates

2. **Weekly:** Review search metrics
   - Most searched terms
   - Failed searches (add synonyms if needed)
   - Average position of clicked results

3. **Monthly:** Optimize based on data
   - Update synonyms
   - Adjust ranking weights
   - Remove obsolete content
   - Add new pages to index

---

## Monitoring & Analytics

### Key Metrics to Track

**Available in Algolia Dashboard → Analytics:**

1. **Search Volume**
   - Total searches per day
   - Unique users
   - Searches per session

2. **Top Searches**
   - Most popular queries
   - Trending searches
   - Seasonal patterns

3. **No-Result Searches**
   - **CRITICAL:** These indicate missing documentation
   - Export weekly and review
   - Add content or synonyms for top no-result queries

4. **Click-Through Rate (CTR)**
   - % of searches that result in clicks
   - Target: >70% CTR
   - Low CTR = poor relevance

5. **Click Position**
   - Which position users click (1st, 2nd, 3rd result)
   - Target: Most clicks on positions 1-3
   - If clicks are scattered, improve ranking

### Setting Up Alerts

**Recommended alerts:**

1. **Zero Results Spike**
   - Alert when no-result searches > 10% of total
   - Indicates indexing issue or missing docs

2. **Search Volume Drop**
   - Alert when searches drop > 50% day-over-day
   - Could indicate search is broken

3. **Crawler Failures**
   - Email notification on crawler errors
   - Go to Crawler Dashboard → Settings → Notifications

---

## Troubleshooting

### Problem: Duplicates Still Appearing

**Solution:**
1. Check crawler logs for errors
2. Review `selectors_exclude` - may need to add more
3. Verify pages don't have duplicate H1s
4. Check for URL variations (trailing slash, query params)

### Problem: Important Pages Not Ranking High

**Solution:**
1. Add `_weight` metadata to page frontmatter
2. Verify custom ranking is configured
3. Check if page is being indexed (search by exact URL)
4. Increase `_weight` value for that page category

### Problem: Typos Not Matching

**Solution:**
1. Verify typo tolerance is enabled
2. Check minimum word size settings (default: 4 chars)
3. Ensure word isn't in "disable typo" list
4. Test with different typo variations

### Problem: Synonyms Not Working

**Solution:**
1. Check synonym was saved correctly (Configuration → Synonyms)
2. Verify synonym type is "synonym" not "one-way"
3. Wait 5 minutes for index update
4. Test with exact synonym terms

### Problem: Outdated Pages Still Indexed

**Solution:**
1. Add URL pattern to crawler `stopUrls`
2. Manually delete records from index (Browse → Select → Delete)
3. Re-run crawler
4. Set up 301 redirects for deprecated URLs

### Problem: Search Results Empty

**Solution:**
1. Check API key is correct in code
2. Verify index name matches (`platform`)
3. Check browser console for CORS errors
4. Verify crawler has run successfully
5. Check index record count (should be >1000)

---

## API Keys & Security

### Key Types

**Search-Only API Key (Public)**
- Current: `7877394996f96cde1a9b795dce3f7787`
- Used in: Website code (scripts.tmpl.partial, search.md)
- Safe to expose publicly
- Can only read/search, not modify

**Admin API Key (Private)**
- **NEVER** expose in code
- Used for: Crawler, config changes, bulk operations
- Kept in: Algolia dashboard only

### Rotating Keys

If API key is compromised:

1. Generate new search-only key:
   - Dashboard → API Keys → "All API Keys"
   - Click "New API Key"
   - Permissions: Search
   - Indices: platform
   - Copy new key

2. Update code:
   - Replace in `doc/templates/uno/partials/scripts.tmpl.partial`
   - Replace in `doc/search.md`
   - Commit and deploy

3. Delete old key:
   - Dashboard → API Keys
   - Find old key → Delete

---

## Support & Resources

### Algolia Documentation
- [DocSearch Guide](https://docsearch.algolia.com/docs/what-is-docsearch)
- [Crawler Documentation](https://www.algolia.com/doc/tools/crawler/getting-started/overview/)
- [Ranking Guide](https://www.algolia.com/doc/guides/managing-results/relevance-overview/)
- [InstantSearch.js](https://www.algolia.com/doc/api-reference/widgets/js/)

### Internal Documentation
- Implementation guide: `doc/templates/uno/SEARCH_README.md`
- Quick fixes: `doc/templates/uno/QUICK_IMPLEMENTATION_GUIDE.md`
- Recommendations: `doc/templates/uno/SEARCH_RECOMMENDATIONS.md`

### Getting Help
- **Algolia Support**: support@algolia.com
- **Community Forum**: https://discourse.algolia.com/
- **GitHub Issue**: unoplatform/uno-private#1588

---

## Estimated Time

**Phase 1 (Crawler):** 2-3 hours
- Creating staging index: 15 min
- Configuring crawler: 1 hour
- Testing and refinement: 1-2 hours

**Phase 2 (Ranking):** 1 hour
- Configuring ranking formula: 15 min
- Setting searchable attributes: 15 min
- Testing: 30 min

**Phase 3 (Synonyms):** 30 min
- Adding synonym groups: 30 min

**Phase 4-6 (Other configs):** 1 hour
- Typo tolerance: 15 min
- Stop words: 15 min
- Performance optimization: 30 min

**Phase 7 (Testing):** 2-3 hours
- Comprehensive testing: 2-3 hours

**Phase 8 (Production):** 1 hour
- Deploying to production: 1 hour

**Total:** ~7-9 hours for complete implementation

---

## Success Criteria

After completing all phases, you should see:

✅ **Zero duplicate results** for the same page  
✅ **Get Started guides appear first** for "getting started"  
✅ **Typos return relevant results** (buttom → button)  
✅ **Synonyms work** (wasm → webassembly docs)  
✅ **No outdated content** in search results  
✅ **Mobile search works smoothly**  
✅ **Category filters functional** in /search.html  
✅ **Analytics tracking active** (check dashboard)  
✅ **"See all results" footer** in search modal  
✅ **Click-through rate >70%**

---

## Next Steps After Configuration

Once Algolia is properly configured:

1. **Monitor for 2 weeks** - Collect analytics data
2. **Review no-result searches** - Identify documentation gaps
3. **Iterate on synonyms** - Add based on failed searches
4. **Adjust ranking** - Based on click patterns
5. **A/B test** - Try different configurations
6. **Document learnings** - Share with team

---

**Questions?** Contact the documentation team or refer to internal docs in `doc/templates/uno/`
