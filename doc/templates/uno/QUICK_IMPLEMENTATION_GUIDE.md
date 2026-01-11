# Quick Implementation Guide: Priority Search Improvements

This guide provides copy-paste ready code snippets for the highest-priority search improvements.

## 1. Add Keyboard Shortcut to Open Search (5 minutes)

Add this to `scripts.tmpl.partial` after the DocSearch initialization:

```javascript
// Keyboard shortcut: Ctrl/Cmd + K to focus search
document.addEventListener('keydown', (e) => {
    // Check for Ctrl+K or Cmd+K
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.querySelector('#docsearch input, .DocSearch-Button');
        if (searchInput) {
            searchInput.click();
            searchInput.focus();
        }
    }
    // Also support '/' key like GitHub
    if (e.key === '/' && document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA') {
        e.preventDefault();
        const searchInput = document.querySelector('#docsearch input, .DocSearch-Button');
        if (searchInput) {
            searchInput.click();
            searchInput.focus();
        }
    }
});
```

## 2. Track Search Analytics with Google Analytics (10 minutes)

Add this to your search page initialization in `search.md`:

```javascript
// Add after search.start()
search.use(() => ({
    onStateChange({ uiState, setUiState }) {
        // Track searches
        if (uiState.platform && uiState.platform.query) {
            const query = uiState.platform.query;
            
            // Google Analytics 4
            if (window.gtag) {
                gtag('event', 'search', {
                    search_term: query,
                });
            }
            
            // Google Analytics Universal
            if (window.ga) {
                ga('send', 'event', 'Search', 'query', query);
            }
        }
    },
}));

// Track result clicks
document.addEventListener('click', (e) => {
    const hitLink = e.target.closest('.hit-title a');
    if (hitLink && window.gtag) {
        const query = new URLSearchParams(window.location.search).get('q');
        gtag('event', 'select_content', {
            content_type: 'search_result',
            search_term: query,
            link_url: hitLink.href,
        });
    }
});
```

## 3. Add Search Tips Panel (5 minutes)

Add this HTML to your search page layout:

```html
<aside class="search-help">
    <h3>Search Tips</h3>
    <dl>
        <dt><kbd>Ctrl</kbd>+<kbd>K</kbd> or <kbd>/</kbd></dt>
        <dd>Open search from anywhere</dd>
        
        <dt><code>"exact phrase"</code></dt>
        <dd>Search for exact phrase</dd>
        
        <dt><code>-exclude</code></dt>
        <dd>Exclude terms from results</dd>
        
        <dt>Use filters</dt>
        <dd>Narrow down by category</dd>
    </dl>
</aside>
```

Add CSS:

```css
.search-help {
    background: #f8f9fa;
    border-radius: 8px;
    padding: 1.5rem;
    margin: 2rem 0;
}

.search-help h3 {
    margin-top: 0;
    font-size: 1.125rem;
}

.search-help dl {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 0.5rem 1rem;
    margin: 0;
}

.search-help dt {
    font-weight: 600;
    color: #5468ff;
}

.search-help dd {
    margin: 0;
    color: #666;
}

.search-help kbd {
    background: white;
    border: 1px solid #ddd;
    border-radius: 3px;
    padding: 0.125rem 0.375rem;
    font-size: 0.875rem;
    font-family: monospace;
}

.search-help code {
    background: white;
    padding: 0.125rem 0.375rem;
    border-radius: 3px;
    font-size: 0.875rem;
}
```

## 4. Add Recent Searches (15 minutes)

Add to your InstantSearch initialization:

```javascript
// Store recent searches
const RECENT_SEARCHES_KEY = 'uno-docs-recent-searches';
const MAX_RECENT = 5;

function saveRecentSearch(query) {
    if (!query || query.length < 2) return;
    
    let recent = JSON.parse(localStorage.getItem(RECENT_SEARCHES_KEY) || '[]');
    
    // Remove if already exists
    recent = recent.filter(q => q !== query);
    
    // Add to front
    recent.unshift(query);
    
    // Keep only MAX_RECENT
    recent = recent.slice(0, MAX_RECENT);
    
    localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(recent));
}

function getRecentSearches() {
    return JSON.parse(localStorage.getItem(RECENT_SEARCHES_KEY) || '[]');
}

// Add recent searches plugin
const recentSearchesPlugin = {
    getSources({ query, setQuery, refresh }) {
        if (query) return [];
        
        const recent = getRecentSearches();
        if (recent.length === 0) return [];
        
        return [{
            sourceId: 'recentSearches',
            getItems() { return recent; },
            templates: {
                header() { return 'Recent Searches'; },
                item({ item }) {
                    return `
                        <div class="recent-search-item">
                            <svg class="recent-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <circle cx="12" cy="12" r="10"/>
                                <polyline points="12 6 12 12 16 14"/>
                            </svg>
                            <span>${item}</span>
                        </div>
                    `;
                },
            },
            onSelect({ item, setQuery, setIsOpen, refresh }) {
                setQuery(item);
                refresh();
            },
        }];
    },
};

// Track searches
search.use(() => ({
    onStateChange({ uiState }) {
        if (uiState.platform && uiState.platform.query) {
            saveRecentSearch(uiState.platform.query);
        }
    },
}));
```

Add CSS:

```css
.recent-search-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem;
    cursor: pointer;
}

.recent-search-item:hover {
    background: #f5f5f5;
}

.recent-icon {
    color: #999;
    flex-shrink: 0;
}
```

## 5. Add "No Results" Tracking (5 minutes)

Track queries that return no results to identify documentation gaps:

```javascript
// Add after search.start() in search.md
search.use(() => ({
    onStateChange({ uiState, results }) {
        if (results && results.platform) {
            const { query, nbHits } = results.platform;
            
            if (query && nbHits === 0) {
                // Track no results
                console.warn('No results for query:', query);
                
                if (window.gtag) {
                    gtag('event', 'search_no_results', {
                        search_term: query,
                    });
                }
                
                // Optional: Send to your own analytics
                fetch('/api/analytics/no-results', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ query, timestamp: Date.now() }),
                }).catch(() => {}); // Silent fail
            }
        }
    },
}));
```

## 6. Add Search Result Click Tracking (5 minutes)

Track which results users click to understand relevance:

```javascript
// Add to search.md
document.addEventListener('click', (e) => {
    const hitLink = e.target.closest('.hit-title a');
    if (!hitLink) return;
    
    const query = new URLSearchParams(window.location.search).get('q');
    const resultUrl = hitLink.href;
    const resultTitle = hitLink.textContent;
    
    // Track with Google Analytics
    if (window.gtag) {
        gtag('event', 'search_result_click', {
            search_term: query,
            link_url: resultUrl,
            link_text: resultTitle,
        });
    }
    
    // Track with Algolia Insights (if configured)
    if (window.aa) {
        aa('clickedObjectIDsAfterSearch', {
            index: 'platform',
            eventName: 'Search Result Clicked',
            queryID: resultUrl, // Use actual queryID from Algolia response
            objectIDs: [resultUrl],
            positions: [1], // Calculate actual position
        });
    }
});
```

## 7. Add Loading States (10 minutes)

Improve perceived performance with skeleton screens:

Add to search.md CSS:

```css
.search-loading {
    display: none;
}

.search-loading.active {
    display: block;
}

.skeleton-item {
    background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
    background-size: 200% 100%;
    animation: loading 1.5s ease-in-out infinite;
    border-radius: 4px;
    margin-bottom: 1rem;
}

.skeleton-title {
    height: 24px;
    width: 70%;
    margin-bottom: 0.5rem;
}

.skeleton-text {
    height: 16px;
    width: 100%;
    margin-bottom: 0.25rem;
}

.skeleton-text:last-child {
    width: 60%;
}

@keyframes loading {
    0% { background-position: 200% 0; }
    100% { background-position: -200% 0; }
}
```

Add loading HTML:

```html
<div id="search-loading" class="search-loading">
    <div class="skeleton-item">
        <div class="skeleton-title"></div>
        <div class="skeleton-text"></div>
        <div class="skeleton-text"></div>
    </div>
    <div class="skeleton-item">
        <div class="skeleton-title"></div>
        <div class="skeleton-text"></div>
        <div class="skeleton-text"></div>
    </div>
</div>
```

Add JavaScript:

```javascript
const loadingEl = document.getElementById('search-loading');

search.use(() => ({
    onStateChange({ uiState, results }) {
        if (uiState.platform && uiState.platform.query && !results) {
            loadingEl.classList.add('active');
        } else {
            loadingEl.classList.remove('active');
        }
    },
}));
```

## 8. Add Search Query Validation (5 minutes)

Prevent problematic searches:

```javascript
// Add to DocSearch initialization
const validateQuery = (query) => {
    // Minimum length
    if (query.length < 2) {
        return false;
    }
    
    // Maximum length
    if (query.length > 200) {
        return false;
    }
    
    // Block SQL injection attempts
    const sqlPattern = /(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC)\b)/i;
    if (sqlPattern.test(query)) {
        return false;
    }
    
    return true;
};

// Use in search
docsearch({
    // ... other config
    transformSearchClient(searchClient) {
        return {
            ...searchClient,
            search(requests) {
                const validatedRequests = requests.filter(request => 
                    validateQuery(request.params.query)
                );
                
                if (validatedRequests.length === 0) {
                    return Promise.resolve({
                        results: requests.map(() => ({
                            hits: [],
                            nbHits: 0,
                            processingTimeMS: 0,
                        })),
                    });
                }
                
                return searchClient.search(validatedRequests);
            },
        };
    },
});
```

## Priority Implementation Order

1. **Keyboard shortcuts** (5 min) - Immediate UX improvement
2. **Analytics tracking** (10 min) - Essential for data-driven decisions
3. **No results tracking** (5 min) - Identify documentation gaps
4. **Result click tracking** (5 min) - Measure relevance
5. **Search tips** (5 min) - Help users search better
6. **Recent searches** (15 min) - Nice quality of life feature
7. **Loading states** (10 min) - Better perceived performance
8. **Query validation** (5 min) - Security and quality

Total time: ~60 minutes for all high-priority improvements

## Testing Checklist

- [ ] Keyboard shortcuts work (Ctrl/Cmd+K and /)
- [ ] Analytics events fire in browser console
- [ ] No results queries are tracked
- [ ] Recent searches appear when search is empty
- [ ] Loading states show during searches
- [ ] Search tips are visible and helpful
- [ ] Mobile experience is smooth
- [ ] Dark mode works correctly
- [ ] All filters function properly
- [ ] Pagination works on all screen sizes
