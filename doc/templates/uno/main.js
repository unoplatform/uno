// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

document.addEventListener(
    "DOMContentLoaded",
    function () {

        initializeNavbar();
        
        // Move SDK badge into the .subnav container
        const sdkBadge = document.getElementById('sdk-version-info');
        const subnav = document.querySelector('.subnav');
        if (sdkBadge && subnav) {
            subnav.appendChild(sdkBadge);
        }
        
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
        fetch('https://api.nuget.org/v3-flatcontainer/uno.sdk/index.json')
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
                    headerTitle.textContent = 'Update Uno Platform';
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
                    function createVersionSection(icon, title, version, description, command, additionalNote) {
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
                        
                        // Add additional note if provided (before command block)
                        if (additionalNote) {
                            const note = document.createElement('p');
                            note.textContent = additionalNote;
                            section.appendChild(note);
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
                    
                    // Uno.Sdk section (for existing projects)
                    const sdkCommand = latestStableVersion
                        ? `{\n  "msbuild-sdks": {\n    "Uno.Sdk": "${latestStableVersion}"\n  }\n}`
                        : null;
                    const sdkSection = createVersionSection(
                        '📦',
                        'Update Uno.Sdk (For Existing Projects)',
                        latestStableVersion,
                        'Recommended for production',
                        sdkCommand,
                        'Edit the global.json file at your solution root:'
                    );
                    const sdkNote = document.createElement('p');
                    sdkNote.className = 'sdk-note';
                    sdkNote.textContent = '📍 ';
                    const sdkNoteStrong = document.createElement('strong');
                    sdkNoteStrong.textContent = 'NuGet Package:';
                    sdkNote.appendChild(sdkNoteStrong);
                    sdkNote.appendChild(document.createTextNode(' '));
                    const sdkNugetLink = document.createElement('a');
                    sdkNugetLink.href = 'https://www.nuget.org/packages/Uno.Sdk';
                    sdkNugetLink.target = '_blank';
                    sdkNugetLink.rel = 'noopener';
                    sdkNugetLink.textContent = 'Uno.Sdk on NuGet.org';
                    sdkNote.appendChild(sdkNugetLink);
                    sdkSection.appendChild(sdkNote);
                    body.appendChild(sdkSection);
                    
                    // Uno.Sdk Dev version section (for existing projects - preview)
                    const sdkDevCommand = latestDevVersion
                        ? `{\n  "msbuild-sdks": {\n    "Uno.Sdk": "${latestDevVersion}"\n  }\n}`
                        : null;
                    const sdkDevSection = createVersionSection(
                        '🚀',
                        'Update Uno.Sdk Dev (For Existing Projects - Preview)',
                        latestDevVersion,
                        'Preview features & fixes',
                        sdkDevCommand,
                        'Edit the global.json file at your solution root:'
                    );
                    const sdkDevNote = document.createElement('p');
                    sdkDevNote.className = 'sdk-note';
                    sdkDevNote.textContent = '📍 ';
                    const sdkDevNoteStrong = document.createElement('strong');
                    sdkDevNoteStrong.textContent = 'NuGet Package:';
                    sdkDevNote.appendChild(sdkDevNoteStrong);
                    sdkDevNote.appendChild(document.createTextNode(' '));
                    const sdkDevNugetLink = document.createElement('a');
                    sdkDevNugetLink.href = 'https://www.nuget.org/packages/Uno.Sdk';
                    sdkDevNugetLink.target = '_blank';
                    sdkDevNugetLink.rel = 'noopener';
                    sdkDevNugetLink.textContent = 'Uno.Sdk on NuGet.org';
                    sdkDevNote.appendChild(sdkDevNugetLink);
                    sdkDevSection.appendChild(sdkDevNote);
                    body.appendChild(sdkDevSection);
                    
                    // Uno.Templates section (for creating new projects)
                    const templatesCommand = latestStableVersion
                        ? 'dotnet new install Uno.Templates'
                        : null;
                    const templatesSection = createVersionSection(
                        '🚀',
                        'Update Uno.Templates (For Creating New Projects)',
                        null,
                        null,
                        templatesCommand,
                        'This installs project templates, not the SDK used by existing projects.'
                    );
                    const templatesNote = document.createElement('p');
                    templatesNote.className = 'sdk-note';
                    templatesNote.textContent = '📍 ';
                    const templatesNoteStrong = document.createElement('strong');
                    templatesNoteStrong.textContent = 'NuGet Package:';
                    templatesNote.appendChild(templatesNoteStrong);
                    templatesNote.appendChild(document.createTextNode(' '));
                    const templatesNugetLink = document.createElement('a');
                    templatesNugetLink.href = 'https://www.nuget.org/packages/Uno.Templates';
                    templatesNugetLink.target = '_blank';
                    templatesNugetLink.rel = 'noopener';
                    templatesNugetLink.textContent = 'Uno.Templates on NuGet.org';
                    templatesNote.appendChild(templatesNugetLink);
                    templatesSection.appendChild(templatesNote);
                    body.appendChild(templatesSection);
                    
                    // Add link to upgrade documentation
                    const upgradeNote = document.createElement('p');
                    upgradeNote.className = 'sdk-note';
                    upgradeNote.textContent = 'ℹ️ ';
                    const upgradeNoteStrong = document.createElement('strong');
                    upgradeNoteStrong.textContent = 'More Information:';
                    upgradeNote.appendChild(upgradeNoteStrong);
                    upgradeNote.appendChild(document.createTextNode(' '));
                    const upgradeLink = document.createElement('a');
                    upgradeLink.href = 'https://platform.uno/docs/articles/upgrading-nuget-packages.html';
                    upgradeLink.target = '_blank';
                    upgradeLink.rel = 'noopener';
                    upgradeLink.textContent = 'How to upgrade Uno Platform NuGet Packages';
                    upgradeNote.appendChild(upgradeLink);
                    body.appendChild(upgradeNote);
                    
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
    },
    false
);

