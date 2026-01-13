// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

document.addEventListener(
    "DOMContentLoaded",
    function () {


        initializeNavbar();
        
        // Cache key and expiration (1 hour)
        const CACHE_KEY = 'uno_sdk_versions';
        const CACHE_DURATION = 60 * 60 * 1000; // 1 hour in milliseconds
        
        // Flag to track if cached data was successfully displayed
        let cachedDataDisplayed = false;
        
        // Try to load cached versions immediately
        const cached = localStorage.getItem(CACHE_KEY);
        let cachedData = null;
        if (cached) {
            try {
                cachedData = JSON.parse(cached);
                if (Date.now() - cachedData.timestamp < CACHE_DURATION) {
                    updateVersionDisplay(cachedData.stable, cachedData.dev);
                    cachedDataDisplayed = true;
                } else {
                    cachedData = null; // Cache expired
                }
            } catch (e) {
                cachedData = null;
            }
        }
        
        // Fetch and update SDK version dynamically from NuGet
        fetch('https://api.nuget.org/v3-flatcontainer/uno.templates/index.json')
            .then(r => r.json())
            .then(data => {
                if (data.versions) {
                    const versions = data.versions;
                    
                    // Find latest stable version (no -dev suffix)
                    const stableVersions = versions.filter(v => !v.includes('-dev'));
                    const latestStableVersion = stableVersions.length > 0
                        ? stableVersions[stableVersions.length - 1]
                        : null;
                    
                    // Find latest dev version
                    const devVersions = versions.filter(v => v.includes('-dev'));
                    const latestDevVersion = devVersions.length > 0
                        ? devVersions[devVersions.length - 1]
                        : null;
                    
                    // Cache the versions and update display only if at least one version is available
                    if (latestStableVersion !== null || latestDevVersion !== null) {
                        localStorage.setItem(CACHE_KEY, JSON.stringify({
                            stable: latestStableVersion,
                            dev: latestDevVersion,
                            timestamp: Date.now()
                        }));
                        
                        // Update display
                        updateVersionDisplay(latestStableVersion, latestDevVersion);
                    }
                }
            })
            .catch(err => {
                console.log('Could not fetch SDK version:', err);
                // Show error state only if cached data wasn't successfully displayed earlier
                // (cachedDataDisplayed is true when cached data was successfully displayed)
                if (!cachedDataDisplayed) {
                    const versionElement = document.querySelector('.sdk-version');
                    const badgeElement = document.querySelector('.sdk-version-badge');

                    if (versionElement) {
                        versionElement.textContent = 'Version unavailable';
                    }

                    if (badgeElement) {
                        // Optional: add a class for styling the error state
                        badgeElement.classList.add('sdk-version-badge-error');
                    }
                }
            });
        
        function updateVersionDisplay(latestStableVersion, latestDevVersion) {
            const versionElement = document.querySelector('.sdk-version');
            const badgeElement = document.querySelector('.sdk-version-badge');
            
            if (!versionElement) return;
            
            // Handle null values with a fallback
            versionElement.textContent = latestStableVersion || 'Version unavailable';
            
            if (badgeElement) {
                // Remove existing click handler if any
                const newBadge = badgeElement.cloneNode(true);
                badgeElement.parentNode.replaceChild(newBadge, badgeElement);
                
                newBadge.title = `Latest stable: ${latestStableVersion}\nLatest dev: ${latestDevVersion}\nClick to see update instructions`;
                newBadge.style.cursor = 'pointer';
                
                // Add click handler to show update instructions
                newBadge.addEventListener('click', function(e) {
                    e.preventDefault();
                    const existingModal = document.getElementById('sdk-update-modal');
                    if (existingModal) {
                        existingModal.remove();
                        return;
                    }
                    
                    // Create modal structure using DOM manipulation to prevent XSS
                    const modal = document.createElement('div');
                    modal.id = 'sdk-update-modal';
                    
                    const overlay = document.createElement('div');
                    overlay.className = 'sdk-modal-overlay';
                    
                    const content = document.createElement('div');
                    content.className = 'sdk-modal-content';
                    
                    // Header
                    const header = document.createElement('div');
                    header.className = 'sdk-modal-header';
                    const headerTitle = document.createElement('h3');
                    headerTitle.textContent = 'Update Uno SDK';
                    const closeBtn = document.createElement('button');
                    closeBtn.className = 'sdk-modal-close';
                    closeBtn.type = 'button';
                    closeBtn.setAttribute('aria-label', 'Close update dialog');
                    closeBtn.textContent = '×';
                    header.appendChild(headerTitle);
                    header.appendChild(closeBtn);
                    
                    // Body
                    const body = document.createElement('div');
                    body.className = 'sdk-modal-body';
                    
                    // Helper function to create version section with safe text content
                    function createVersionSection(icon, title, version, description, command) {
                        const section = document.createElement('div');
                        section.className = 'sdk-version-section';
                        
                        const h4 = document.createElement('h4');
                        h4.textContent = icon + ' ' + title;
                        section.appendChild(h4);
                        
                        // Only add version label if version and description are provided
                        if (version || description) {
                            const versionLabel = document.createElement('p');
                            versionLabel.className = 'version-label';
                            if (version) {
                                const strong = document.createElement('strong');
                                strong.textContent = version; // Use textContent to prevent XSS
                                versionLabel.appendChild(strong);
                            }
                            if (description) {
                                versionLabel.appendChild(document.createTextNode(version ? ' - ' + description : description));
                            }
                            section.appendChild(versionLabel);
                        }
                        
                        // Only add command block if a valid command string is provided
                        if (command) {
                            const codeBlock = document.createElement('div');
                            codeBlock.className = 'sdk-code-block';
                            const code = document.createElement('code');
                            code.textContent = command; // Use textContent to prevent XSS
                            const copyBtn = document.createElement('button');
                            copyBtn.className = 'sdk-copy-btn';
                            copyBtn.setAttribute('data-clipboard', command);
                            copyBtn.textContent = 'Copy';
                            codeBlock.appendChild(code);
                            codeBlock.appendChild(copyBtn);
                            section.appendChild(codeBlock);
                        }
                        
                        return section;
                    }
                    
                    // Stable version section
                    const stableCommand = latestStableVersion
                        ? 'dotnet new install Uno.Templates::' + latestStableVersion
                        : null;
                    body.appendChild(createVersionSection(
                        '📦',
                        'Latest Stable Version',
                        latestStableVersion,
                        'Recommended for production',
                        stableCommand
                    ));
                    
                    // Dev version section
                    const devCommand = latestDevVersion
                        ? 'dotnet new install Uno.Templates::' + latestDevVersion
                        : null;
                    const devSection = createVersionSection(
                        '🚀',
                        'Latest Dev Version',
                        latestDevVersion,
                        'Preview features & fixes',
                        devCommand
                    );
                    const devNote = document.createElement('p');
                    devNote.className = 'sdk-note';
                    devNote.textContent = '📍 ';
                    const noteStrong = document.createElement('strong');
                    noteStrong.textContent = 'NuGet Package:';
                    devNote.appendChild(noteStrong);
                    devNote.appendChild(document.createTextNode(' '));
                    const nugetLink = document.createElement('a');
                    nugetLink.href = 'https://www.nuget.org/packages/Uno.Templates';
                    nugetLink.target = '_blank';
                    nugetLink.rel = 'noopener';
                    nugetLink.textContent = 'Uno.Templates on NuGet.org';
                    devNote.appendChild(nugetLink);
                    devSection.appendChild(devNote);
                    body.appendChild(devSection);
                    
                    // Check version section
                    body.appendChild(createVersionSection(
                        'ℹ️',
                        'Check Installed Version',
                        null,
                        null,
                        'dotnet new details Uno.Templates'
                    ));
                    
                    // Tip note
                    const tipNote = document.createElement('p');
                    tipNote.className = 'sdk-note';
                    tipNote.textContent = '💡 ';
                    const tipStrong = document.createElement('strong');
                    tipStrong.textContent = 'Tip:';
                    tipNote.appendChild(tipStrong);
                    tipNote.appendChild(document.createTextNode(' If you have an older version installed, uninstall it first using:'));
                    const br = document.createElement('br');
                    tipNote.appendChild(br);
                    const tipCode = document.createElement('code');
                    tipCode.textContent = 'dotnet new uninstall Uno.Templates';
                    tipNote.appendChild(tipCode);
                    body.appendChild(tipNote);
                    
                    // Assemble modal
                    content.appendChild(header);
                    content.appendChild(body);
                    overlay.appendChild(content);
                    modal.appendChild(overlay);

                    // Save the element that was focused before opening the modal
                    const previouslyFocusedElement = document.activeElement;

                    document.body.appendChild(modal);

                    // Move focus to the close button (or first focusable element) for accessibility
                    const closeButton = modal.querySelector('.sdk-modal-close');
                    if (closeButton && typeof closeButton.focus === 'function') {
                        closeButton.focus();
                    }

                    const focusableSelectors = 'a[href], button:not([disabled]), textarea, input[type="text"], input[type="radio"], input[type="checkbox"], select, [tabindex]:not([tabindex="-1"])';
                    function getFocusableElements() {
                        return Array.from(modal.querySelectorAll(focusableSelectors))
                            .filter(el => !el.hasAttribute('disabled') && el.getAttribute('aria-hidden') !== 'true');
                    }
                    
                    // Close modal handlers (click and Escape key)
                    function closeModal() {
                        modal.remove();
                        document.removeEventListener('keydown', onKeyDown);
                        if (previouslyFocusedElement && document.contains(previouslyFocusedElement) && typeof previouslyFocusedElement.focus === 'function') {
                            previouslyFocusedElement.focus();
                        }
                    }

                    function onKeyDown(e) {
                        if (e.key === 'Escape' || e.key === 'Esc') {
                            e.preventDefault();
                            closeModal();
                            return;
                        }

                        if (e.key === 'Tab') {
                            const focusableElements = getFocusableElements();
                            if (!focusableElements.length) {
                                return;
                            }

                            const firstElement = focusableElements[0];
                            const lastElement = focusableElements[focusableElements.length - 1];
                            const isShift = e.shiftKey;
                            const current = document.activeElement;

                            if (!isShift && current === lastElement) {
                                e.preventDefault();
                                firstElement.focus();
                            } else if (isShift && current === firstElement) {
                                e.preventDefault();
                                lastElement.focus();
                            }
                        }
                    }

                    document.addEventListener('keydown', onKeyDown);

                    if (closeButton) {
                        closeButton.addEventListener('click', () => closeModal());
                    }
                    modal.querySelector('.sdk-modal-overlay').addEventListener('click', (e) => {
                        if (e.target.classList.contains('sdk-modal-overlay')) {
                            closeModal();
                        }
                    });
                    
                    // Copy button handlers
                    modal.querySelectorAll('.sdk-copy-btn').forEach(btn => {
                        btn.addEventListener('click', function() {
                            const text = this.getAttribute('data-clipboard');
                            const button = this;
                            const showCopiedState = () => {
                                const originalText = button.textContent;
                                button.textContent = 'Copied!';
                                button.classList.add('copied');
                                setTimeout(() => {
                                    button.textContent = originalText;
                                    button.classList.remove('copied');
                                }, 2000);
                            };

                            if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
                                navigator.clipboard.writeText(text)
                                    .then(showCopiedState)
                                    .catch(err => {
                                        console.error('Failed to copy to clipboard using navigator.clipboard:', err);
                                    });
                            } else {
                                // Fallback for browsers without Clipboard API support
                                try {
                                    const textarea = document.createElement('textarea');
                                    textarea.value = text;
                                    textarea.setAttribute('readonly', '');
                                    textarea.style.position = 'absolute';
                                    textarea.style.left = '-9999px';
                                    document.body.appendChild(textarea);
                                    textarea.select();
                                    let successful = false;
                                    try {
                                        successful = document.execCommand && document.execCommand('copy');
                                    } catch (execErr) {
                                        console.warn('document.execCommand copy failed:', execErr);
                                    }
                                    document.body.removeChild(textarea);
                                    if (successful) {
                                        showCopiedState();
                                    } else {
                                        console.warn('Fallback copy to clipboard was not successful.');
                                    }
                                } catch (err) {
                                    console.error('Fallback copy to clipboard failed:', err);
                                }
                            }
                        });
                    });
                });
            }
        }

        document.addEventListener(
            "click",
            function (e) {
                const t = e.target;
                if (
                    window.innerWidth >= 1024 ||
                    !t.matches("#navbar .menu-item-has-children a")
                )
                    return;
                e.stopImmediatePropagation();
                t.parentElement.classList.toggle("open");
            },
            true
        );
        
        // Light-dismiss for Pagefind search flyout
        document.addEventListener('click', function(e) {
            const searchContainer = document.querySelector('#search');
            const pagefindDrawer = document.querySelector('.pagefind-ui__drawer');
            
            if (!searchContainer || !pagefindDrawer) return;
            
            // Check if the drawer is visible
            const drawerIsVisible = pagefindDrawer.offsetParent !== null;
            
            if (drawerIsVisible) {
                // Check if click is outside the search container
                if (!searchContainer.contains(e.target)) {
                    // Clear the search input to dismiss the drawer
                    const searchInput = searchContainer.querySelector('.pagefind-ui__search-input');
                    if (searchInput) {
                        searchInput.value = '';
                        // Trigger input event to update Pagefind UI
                        searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                }
            }
        }, true);
        
        // Store and display search history as pills
        const SEARCH_HISTORY_KEY = 'uno_search_history';
        const MAX_SEARCH_HISTORY = 10;
        
        // Wait for Pagefind to initialize
        const initializeSearchKeywords = setInterval(function() {
            const searchInput = document.querySelector('.pagefind-ui__search-input');
            const searchContainer = document.querySelector('#search');
            
            if (searchInput && searchContainer) {
                clearInterval(initializeSearchKeywords);
                
                // Get search history
                function getSearchHistory() {
                    const history = localStorage.getItem(SEARCH_HISTORY_KEY);
                    return history ? JSON.parse(history) : [];
                }
                
                // Save search to history
                function saveSearchToHistory(query) {
                    let history = getSearchHistory();
                    // Remove if already exists
                    history = history.filter(item => item !== query);
                    // Add to beginning
                    history.unshift(query);
                    // Keep only last MAX_SEARCH_HISTORY items
                    history = history.slice(0, MAX_SEARCH_HISTORY);
                    localStorage.setItem(SEARCH_HISTORY_KEY, JSON.stringify(history));
                }
                
                // Clear search history
                function clearSearchHistory() {
                    localStorage.removeItem(SEARCH_HISTORY_KEY);
                    const pillsContainer = document.querySelector('.search-history-pills');
                    if (pillsContainer) {
                        pillsContainer.remove();
                    }
                }
                
                // Create or update pills container
                function updatePillsContainer() {
                    const history = getSearchHistory();
                    
                    // Remove existing pills container
                    const existingPills = document.querySelector('.search-history-pills');
                    if (existingPills) {
                        existingPills.remove();
                    }
                    
                    // Remove any existing empty state from results area
                    const existingEmptyState = document.querySelector('.search-empty-state');
                    if (existingEmptyState) {
                        existingEmptyState.remove();
                    }
                    
                    // Only show if input is empty
                    const hasQuery = searchInput.value.trim().length > 0;
                    if (hasQuery) {
                        return;
                    }
                    
                    // Wait for drawer to exist
                    const drawer = document.querySelector('.pagefind-ui__drawer');
                    if (!drawer) {
                        return; // Drawer not available yet
                    }
                    
                    // Show pills if history exists
                    if (history.length > 0) {
                        const pillsContainer = document.createElement('div');
                        pillsContainer.className = 'search-history-pills';
                    
                    // Create header with title and clear button
                    const header = document.createElement('div');
                    header.className = 'search-history-header';
                    
                    const title = document.createElement('div');
                    title.className = 'search-history-title';
                    title.textContent = 'Recent searches';
                    header.appendChild(title);
                    
                    const clearButton = document.createElement('button');
                    clearButton.className = 'search-history-clear';
                    clearButton.textContent = 'Clear all';
                    clearButton.setAttribute('type', 'button');
                    clearButton.addEventListener('click', function(e) {
                        e.preventDefault();
                        e.stopPropagation();
                        clearSearchHistory();
                    });
                    header.appendChild(clearButton);
                    
                    pillsContainer.appendChild(header);
                    
                    const pillsWrapper = document.createElement('div');
                    pillsWrapper.className = 'search-history-pills-wrapper';
                    
                    history.forEach(function(term) {
                        const pill = document.createElement('button');
                        pill.className = 'search-history-pill';
                        pill.textContent = term;
                        pill.setAttribute('type', 'button');
                        pill.addEventListener('click', function(e) {
                            e.preventDefault();
                            e.stopPropagation();
                            searchInput.value = term;
                            searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                            searchInput.focus();
                        });
                        pillsWrapper.appendChild(pill);
                    });
                    
                    pillsContainer.appendChild(pillsWrapper);
                        
                        // Insert pills at the top of the drawer
                        drawer.insertBefore(pillsContainer, drawer.firstChild);
                    }
                    
                    // Show empty state in the results area (always shown, even with history)
                    // Poll for the results area with retries
                    let attempts = 0;
                    const maxAttempts = 20;
                    const checkInterval = setInterval(function() {
                        attempts++;
                        const resultsArea = drawer.querySelector('.pagefind-ui__results-area');
                        
                        if (resultsArea || attempts >= maxAttempts) {
                            clearInterval(checkInterval);
                            
                            if (resultsArea) {
                                // Hide/remove the Pagefind "No results" message
                                const pagefindMessage = resultsArea.querySelector('.pagefind-ui__message');
                                if (pagefindMessage) {
                                    pagefindMessage.style.display = 'none';
                                }
                                
                                // Create and inject empty state
                                const emptyState = document.createElement('div');
                                emptyState.className = 'search-empty-state';
                                emptyState.innerHTML = `
                                    <div class="search-empty-state__icon">🔍</div>
                                    <div class="search-empty-state__title">Start searching</div>
                                    <div class="search-empty-state__description">Type a keyword or phrase to search through the Uno Platform documentation</div>
                                `;
                                resultsArea.appendChild(emptyState);
                            }
                        }
                    }, 50); // Check every 50ms
                }
                
                // Show pills or empty state on focus
                searchInput.addEventListener('focus', function() {
                    // If input is empty, show drawer with pills or empty state
                    if (!searchInput.value.trim()) {
                        // Trigger empty search to open drawer without visible text
                        const originalValue = searchInput.value;
                        
                        // Use a zero-width space to trigger drawer without showing results
                        searchInput.value = '\u200B'; // Zero-width space
                        searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                        
                        // Wait a moment for drawer to potentially appear
                        setTimeout(function() {
                            // Restore original empty value
                            searchInput.value = originalValue;
                            
                            // Check for drawer and show pills
                            const checkDrawer = setInterval(function() {
                                const drawer = document.querySelector('.pagefind-ui__drawer');
                                if (drawer) {
                                    clearInterval(checkDrawer);
                                    updatePillsContainer();
                                }
                            }, 50);
                            
                            // Stop checking after 1 second
                            setTimeout(function() { clearInterval(checkDrawer); }, 1000);
                        }, 10);
                    }
                });
                
                // Hide pills and empty state when typing
                searchInput.addEventListener('input', function() {
                    const query = searchInput.value.trim();
                    const pillsContainer = document.querySelector('.search-history-pills');
                    const emptyState = document.querySelector('.search-empty-state');
                    
                    if (query) {
                        // Hide pills and empty state when there's a query
                        if (pillsContainer) {
                            pillsContainer.remove();
                        }
                        if (emptyState) {
                            emptyState.remove();
                        }
                    } else {
                        // Show appropriate state when query is cleared
                        updatePillsContainer();
                    }
                });
                
                // Save search on blur if there's a value
                searchInput.addEventListener('blur', function() {
                    const query = searchInput.value.trim();
                    if (query) {
                        saveSearchToHistory(query);
                    }
                });
            }
        }, 100);
        
        // Clear initialization check after 5 seconds to avoid memory leak
        setTimeout(function() {
            clearInterval(initializeSearchKeywords);
        }, 5000);
    },
    false
);

