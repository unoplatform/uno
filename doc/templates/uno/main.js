// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

document.addEventListener(
    "DOMContentLoaded",
    function () {

        initializeNavbar();
        
        // Move SDK version badge into first h1
        const sdkBadge = document.querySelector('#sdk-version-info');
        const firstH1 = document.querySelector('article h1:first-child');
        if (sdkBadge && firstH1) {
            firstH1.appendChild(sdkBadge);
        }
        
        // Cache key and expiration (1 hour)
        const CACHE_KEY = 'uno_sdk_versions';
        const CACHE_DURATION = 60 * 60 * 1000; // 1 hour in milliseconds
        
        // Try to load cached versions immediately
        const cached = localStorage.getItem(CACHE_KEY);
        let cachedData = null;
        if (cached) {
            try {
                cachedData = JSON.parse(cached);
                if (Date.now() - cachedData.timestamp < CACHE_DURATION) {
                    updateVersionDisplay(cachedData.stable, cachedData.dev);
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
                    const latestStableVersion = stableVersions[stableVersions.length - 1];
                    
                    // Find latest dev version
                    const devVersions = versions.filter(v => v.includes('-dev'));
                    const latestDevVersion = devVersions[devVersions.length - 1];
                    
                    // Cache the versions
                    localStorage.setItem(CACHE_KEY, JSON.stringify({
                        stable: latestStableVersion,
                        dev: latestDevVersion,
                        timestamp: Date.now()
                    }));
                    
                    // Update display
                    updateVersionDisplay(latestStableVersion, latestDevVersion);
                }
            })
            .catch(err => {
                console.log('Could not fetch SDK version:', err);
                // If fetch fails and we have cached data, keep using it
            });
        
        function updateVersionDisplay(latestStableVersion, latestDevVersion) {
            const versionElement = document.querySelector('.sdk-version');
            const badgeElement = document.querySelector('.sdk-version-badge');
            
            if (!versionElement) return;
            
            versionElement.textContent = latestStableVersion;
            
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
                    
                    const modal = document.createElement('div');
                    modal.id = 'sdk-update-modal';
                    modal.innerHTML = `
                        <div class="sdk-modal-overlay">
                            <div class="sdk-modal-content">
                                <div class="sdk-modal-header">
                                    <h3>Update Uno SDK</h3>
                                    <button class="sdk-modal-close" type="button" aria-label="Close update dialog">&times;</button>
                                </div>
                                <div class="sdk-modal-body">
                                    <div class="sdk-version-section">
                                        <h4>📦 Latest Stable Version</h4>
                                        <p class="version-label"><strong>${latestStableVersion}</strong> - Recommended for production</p>
                                        <div class="sdk-code-block">
                                            <code>dotnet new install Uno.Templates::${latestStableVersion}</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new install Uno.Templates::${latestStableVersion}">Copy</button>
                                        </div>
                                    </div>
                                    
                                    <div class="sdk-version-section">
                                        <h4>🚀 Latest Dev Version</h4>
                                        <p class="version-label"><strong>${latestDevVersion}</strong> - Preview features & fixes</p>
                                        <div class="sdk-code-block">
                                            <code>dotnet new install Uno.Templates::${latestDevVersion}</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new install Uno.Templates::${latestDevVersion}">Copy</button>
                                        </div>
                                        <p class="sdk-note">
                                            📍 <strong>NuGet Package:</strong> <a href="https://www.nuget.org/packages/Uno.Templates" target="_blank" rel="noopener">Uno.Templates on NuGet.org</a>
                                        </p>
                                    </div>
                                    
                                    <div class="sdk-version-section">
                                        <h4>ℹ️ Check Installed Version</h4>
                                        <div class="sdk-code-block">
                                            <code>dotnet new details Uno.Templates</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new details Uno.Templates">Copy</button>
                                        </div>
                                    </div>
                                    
                                    <p class="sdk-note">
                                        💡 <strong>Tip:</strong> If you have an older version installed, uninstall it first using:<br>
                                        <code>dotnet new uninstall Uno.Templates</code>
                                    </p>
                                </div>
                            </div>
                        </div>
                    `;
                    document.body.appendChild(modal);
                    
                    // Close modal handlers (click and Escape key)
                    function closeModal() {
                        modal.remove();
                        document.removeEventListener('keydown', onKeyDown);
                    }

                    function onKeyDown(e) {
                        if (e.key === 'Escape' || e.key === 'Esc') {
                            e.preventDefault();
                            closeModal();
                        }
                    }

                    document.addEventListener('keydown', onKeyDown);

                    modal.querySelector('.sdk-modal-close').addEventListener('click', () => closeModal());
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
                                    const successful = document.execCommand && document.execCommand('copy');
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

