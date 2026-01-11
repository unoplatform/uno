---
_layout: chromeless
_disableNavbar: false
_disableBreadcrumb: true
_disableToc: true
_disableAffix: true
_disableContribution: true
_enableSearch: false
---

# Search Results

<div id="search-container">
  <div class="search-header">
    <div id="search-input-container"></div>
    <div id="search-stats"></div>
  </div>
  <div class="search-body">
    <aside id="search-refinements" class="search-sidebar">
      <div id="search-categories"></div>
      <div id="search-clear"></div>
    </aside>
    <main class="search-results">
      <div id="search-hits"></div>
      <div id="search-pagination"></div>
    </main>
  </div>
</div>

<!-- Algolia InstantSearch for Full Search Results Page -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/instantsearch.css@8/themes/satellite-min.css" />
<script src="https://cdn.jsdelivr.net/npm/algoliasearch@4/dist/algoliasearch-lite.umd.js"></script>
<script src="https://cdn.jsdelivr.net/npm/instantsearch.js@4"></script>

<style>
#search-container {
    max-width: 1400px;
    margin: 2rem auto;
    padding: 0 1rem;
}

.search-header {
    margin-bottom: 2rem;
}

#search-input-container {
    margin-bottom: 1rem;
}

#search-stats {
    margin-bottom: 1rem;
}

.search-body {
    display: grid;
    grid-template-columns: 250px 1fr;
    gap: 2rem;
    align-items: start;
}

.search-sidebar {
    position: sticky;
    top: 20px;
}

.search-results {
    min-width: 0;
}

@media (max-width: 768px) {
    .search-body {
        grid-template-columns: 1fr;
    }
    
    .search-sidebar {
        position: static;
        border-bottom: 1px solid #eee;
        padding-bottom: 1rem;
        margin-bottom: 1rem;
    }
}

.ais-SearchBox {
    margin-bottom: 1rem;
}

.ais-SearchBox-input {
    width: 100%;
    padding: 0.75rem 1rem;
    font-size: 1.125rem;
    border: 2px solid #ddd;
    border-radius: 8px;
    transition: all 0.2s ease;
}

.ais-SearchBox-input:focus {
    outline: none;
    border-color: #5468ff;
    box-shadow: 0 4px 12px rgba(84, 104, 255, 0.15);
}

.ais-SearchBox-submit,
.ais-SearchBox-reset {
    padding: 0.5rem;
}

.ais-SearchBox-resetIcon {
    width: 14px;
    height: 14px;
}

.ais-RefinementList-item {
    margin-bottom: 0.5rem;
}

.ais-RefinementList-label {
    cursor: pointer;
    display: flex;
    align-items: center;
    padding: 0.25rem 0;
}

.ais-RefinementList-checkbox {
    margin-right: 0.5rem;
}

.ais-RefinementList-count {
    margin-left: auto;
    background: #f0f0f0;
    padding: 0.125rem 0.5rem;
    border-radius: 12px;
    font-size: 0.75rem;
}

.ais-ClearRefinements {
    margin-top: 1rem;
}

.ais-ClearRefinements-button {
    width: 100%;
    padding: 0.5rem 1rem;
    border: 1px solid #ddd;
    border-radius: 4px;
    background: white;
    cursor: pointer;
    font-size: 0.875rem;
}

.ais-ClearRefinements-button:hover:not(:disabled) {
    background: #f5f5f5;
    border-color: #5468ff;
}

.ais-ClearRefinements-button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.ais-Hits-item {
    margin-bottom: 1rem;
    padding: 1rem;
    border: 1px solid #eee;
    border-radius: 4px;
    background: #fff;
}

.ais-Hits-item:hover {
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.hit-title {
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.hit-title a {
    color: #5468ff;
    text-decoration: none;
}

.hit-title a:hover {
    text-decoration: underline;
}

.hit-hierarchy {
    font-size: 0.875rem;
    color: #666;
    margin-bottom: 0.5rem;
}

.hit-content {
    color: #333;
    line-height: 1.5;
}

.ais-Pagination {
    margin-top: 2rem;
}

.ais-Stats {
    color: #666;
    font-size: 0.875rem;
}

.ais-Stats-text {
    display: block;
}

.search-tips {
    background: #f8f9fa;
    border-left: 4px solid #5468ff;
    padding: 1rem;
    margin: 1rem 0;
    border-radius: 4px;
}

.search-tips h3 {
    margin: 0 0 0.5rem 0;
    font-size: 1rem;
    color: #333;
}

.search-tips ul {
    margin: 0;
    padding-left: 1.5rem;
}

.search-tips li {
    margin-bottom: 0.25rem;
    font-size: 0.875rem;
    color: #666;
}

/* Dark mode support */
@media (prefers-color-scheme: dark) {
    .ais-SearchBox-input {
        background: #1e1e1e;
        border-color: #444;
        color: #ddd;
    }
    
    .ais-Hits-item {
        background: #1e1e1e;
        border-color: #333;
    }
    
    .hit-content {
        color: #ddd;
    }
    
    .hit-hierarchy {
        color: #999;
    }
    
    .search-tips {
        background: #2a2a2a;
        border-color: #5468ff;
    }
    
    .search-tips h3 {
        color: #ddd;
    }
    
    .ais-RefinementList-count {
        background: #333;
        color: #ddd;
    }
    
    .ais-ClearRefinements-button {
        background: #1e1e1e;
        border-color: #444;
        color: #ddd;
    }
    
    .ais-ClearRefinements-button:hover:not(:disabled) {
        background: #2a2a2a;
    }
}
</style>

<script>
(function() {
    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeInstantSearch);
    } else {
        initializeInstantSearch();
    }

    function initializeInstantSearch() {
        const searchClient = algoliasearch('PHB9D8WS99', '7877394996f96cde1a9b795dce3f7787');
        
        const search = instantsearch({
            indexName: 'platform',
            searchClient,
            routing: true,
            insights: true, // Enable search analytics
        });

        // Get initial query from URL
        const urlParams = new URLSearchParams(window.location.search);
        const initialQuery = urlParams.get('q') || '';

        search.addWidgets([
            instantsearch.widgets.searchBox({
                container: '#search-input-container',
                placeholder: 'Search documentation...',
                autofocus: true,
                showLoadingIndicator: true,
                showReset: true,
                showSubmit: true,
                queryHook(query, search) {
                    // Track searches for analytics
                    if (query && query.length >= 3 && window.gtag) {
                        gtag('event', 'search', {
                            search_term: query,
                        });
                    }
                    search(query);
                },
            }),

            instantsearch.widgets.stats({
                container: '#search-stats',
                templates: {
                    text(data, { html }) {
                        let text = '';
                        if (data.hasManyResults) {
                            text = `${data.nbHits.toLocaleString()} results found`;
                        } else if (data.hasOneResult) {
                            text = `1 result found`;
                        } else {
                            text = `No results`;
                        }
                        if (data.query) {
                            text += ` for "${data.query}"`;
                        }
                        text += ` in ${data.processingTimeMS}ms`;
                        return html`<span>${text}</span>`;
                    },
                },
            }),

            // Refinement list for filtering by category/section
            instantsearch.widgets.refinementList({
                container: '#search-categories',
                attribute: 'hierarchy.lvl0',
                limit: 10,
                showMore: true,
                showMoreLimit: 20,
                searchable: true,
                searchablePlaceholder: 'Search categories...',
                templates: {
                    header: 'Filter by Category',
                },
            }),

            // Clear refinements button
            instantsearch.widgets.clearRefinements({
                container: '#search-clear',
                templates: {
                    resetLabel: 'Clear all filters',
                },
            }),

            // Query suggestions for typos/alternatives
            instantsearch.widgets.queryRuleCustomData({
                container: '#search-stats',
                templates: {
                    default({ items }, { html }) {
                        if (!items || items.length === 0) return '';
                        
                        const suggestions = items[0];
                        if (suggestions && suggestions.redirect) {
                            window.location.href = suggestions.redirect;
                        }
                        
                        if (suggestions && suggestions.banner) {
                            return html`
                                <div class="search-banner" style="background: #e3f2fd; padding: 1rem; margin-bottom: 1rem; border-radius: 4px; border-left: 4px solid #2196f3;">
                                    ${suggestions.banner}
                                </div>
                            `;
                        }
                        
                        return '';
                    },
                },
            }),

            instantsearch.widgets.hits({
                container: '#search-hits',
                templates: {
                    item(hit, { html, components }) {
                        return html`
                            <article class="hit-item">
                                <div class="hit-title">
                                    <a href="${hit.url}">${components.Highlight({ hit, attribute: 'title' })}</a>
                                </div>
                                ${hit.hierarchy && hit.hierarchy.lvl0 ? html`
                                    <div class="hit-hierarchy">
                                        ${hit.hierarchy.lvl0}
                                        ${hit.hierarchy.lvl1 ? ` > ${hit.hierarchy.lvl1}` : ''}
                                        ${hit.hierarchy.lvl2 ? ` > ${hit.hierarchy.lvl2}` : ''}
                                    </div>
                                ` : ''}
                                ${hit.content ? html`
                                    <div class="hit-content">
                                        ${components.Snippet({ hit, attribute: 'content' })}
                                    </div>
                                ` : ''}
                            </article>
                        `;
                    },
                    empty(results, { html }) {
                        return html`
                            <div class="no-results">
                                <p>No results found for query <strong>${results.query}</strong>.</p>
                                <div class="search-tips">
                                    <h3>Search Tips:</h3>
                                    <ul>
                                        <li>Try different or more general keywords</li>
                                        <li>Check your spelling</li>
                                        <li>Remove filters to see more results</li>
                                        <li>Use fewer keywords for broader results</li>
                                        <li>Try searching for related concepts</li>
                                    </ul>
                                </div>
                                <p>Popular topics: <a href="/docs/articles/get-started.html">Get Started</a>, <a href="/docs/articles/controls.html">Controls</a>, <a href="/docs/articles/features.html">Features</a></p>
                            </div>
                        `;
                    },
                },
            }),

            instantsearch.widgets.pagination({
                container: '#search-pagination',
                padding: 2,
                showFirst: true,
                showLast: true,
                showPrevious: true,
                showNext: true,
            }),
        ]);

        search.start();

        // Set initial query if provided in URL
        if (initialQuery) {
            search.helper.setQuery(initialQuery).search();
        }

        // Track queries with no results to identify documentation gaps
        search.use(() => ({
            onStateChange({ uiState, results }) {
                if (results && results.platform) {
                    const { query, nbHits } = results.platform;
                    
                    if (query && nbHits === 0) {
                        // Log to console for debugging
                        console.warn('No results found for query:', query);
                        
                        // Track with Google Analytics
                        if (window.gtag) {
                            gtag('event', 'search_no_results', {
                                search_term: query,
                            });
                        }
                        
                        // Optional: Send to your analytics endpoint
                        // This helps identify what documentation is missing
                        try {
                            fetch('/api/analytics/no-results', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ 
                                    query, 
                                    timestamp: Date.now(),
                                    url: window.location.href,
                                }),
                            }).catch(() => {}); // Silent fail
                        } catch (e) {
                            // Silent fail - don't break search if analytics fails
                        }
                    }
                }
            },
        }));
    }
})();
</script>
