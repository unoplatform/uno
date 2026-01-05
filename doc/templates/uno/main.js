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
        
        // Fetch and update SDK version dynamically from NuGet
        fetch('https://api.nuget.org/v3-flatcontainer/uno.templates/index.json')
            .then(r => r.json())
            .then(data => {
                const versionElement = document.querySelector('.sdk-version');
                const badgeElement = document.querySelector('.sdk-version-badge');
                const sdkContainer = document.querySelector('#sdk-version-info');
                
                if (versionElement && data.versions) {
                    const versions = data.versions;
                    
                    // Find latest stable version (no -dev suffix)
                    const stableVersions = versions.filter(v => !v.includes('-dev'));
                    const latestStableVersion = stableVersions[stableVersions.length - 1];
                    
                    // Find latest dev version
                    const devVersions = versions.filter(v => v.includes('-dev'));
                    const latestDevVersion = devVersions[devVersions.length - 1];
                    
                    versionElement.textContent = latestStableVersion;
                    
                    if (badgeElement) {
                        badgeElement.title = `Latest stable: ${latestStableVersion}\nLatest dev: ${latestDevVersion}\nClick to see update instructions`;
                        badgeElement.style.cursor = 'pointer';
                        
                        // Add click handler to show update instructions
                        badgeElement.addEventListener('click', function(e) {
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
                                            <button class="sdk-modal-close">&times;</button>
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
                            
                            // Close modal handlers
                            modal.querySelector('.sdk-modal-close').addEventListener('click', () => modal.remove());
                            modal.querySelector('.sdk-modal-overlay').addEventListener('click', (e) => {
                                if (e.target.classList.contains('sdk-modal-overlay')) {
                                    modal.remove();
                                }
                            });
                            
                            // Copy button handlers
                            modal.querySelectorAll('.sdk-copy-btn').forEach(btn => {
                                btn.addEventListener('click', function() {
                                    const text = this.getAttribute('data-clipboard');
                                    navigator.clipboard.writeText(text).then(() => {
                                        const originalText = this.textContent;
                                        this.textContent = 'Copied!';
                                        this.classList.add('copied');
                                        setTimeout(() => {
                                            this.textContent = originalText;
                                            this.classList.remove('copied');
                                        }, 2000);
                                    });
                                });
                            });
                        });
                    }
                }
            })
            .catch(err => console.log('Could not fetch SDK version:', err));

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

