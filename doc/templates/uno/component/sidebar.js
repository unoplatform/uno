const sidefilterHeight = 94; //94px from sidefilter height

function setTocHeight() {
    if($(window).width() < 768) {
        let headerHeight = $("#header-container").outerHeight();
        let breadcrumbHeight = $("#breadcrumb").outerHeight();
        let tocToggleHeight = $(".btn.toc-toggle.collapse").outerHeight();
        let intViewportHeight = window.innerHeight;
        let sidenavPaddingTop = parseInt($(".sidenav").css('padding-top'));
        let maxHeightToc = intViewportHeight - (headerHeight + breadcrumbHeight + tocToggleHeight + sidefilterHeight + sidenavPaddingTop);
        $(".sidetoc").css("max-height", maxHeightToc);
    } else {
        $(".sidetoc").css("max-height", "none");
    }
}

function updateTocHeightOnResize() {
    $(window).on('resize', function () {
        setTocHeight();
    });
}

function setSidenavTop() {
    let headerHeight = $("#header-container").outerHeight();
    let breadcrumbHeight = $("#breadcrumb").outerHeight();
    let tocToggleHeight = $(".btn.toc-toggle.collapse").outerHeight();
    let sidenavTop = headerHeight + breadcrumbHeight;
    let sidefilterTop = headerHeight + breadcrumbHeight;
    let sidetocTop = sidefilterTop + sidefilterHeight;
    let articleMarginTopDesk = sidenavTop + tocToggleHeight + 30; //30px from .sidenav padding top and bottom
    let articleMarginTopMobile = sidenavTop;
    $(".sidenav").css("top", sidenavTop);
    $(".sidefilter").css("top", sidenavTop);
    $(".sidetoc").css("top", sidetocTop);

    if($(window).width() < 768) {
        $(".body-content .article").attr("style", "margin-top:" + (articleMarginTopDesk + 5) + "px !important");
    } else {
        $(".body-content .article").attr("style", "margin-top:" + (articleMarginTopMobile + 5) + "px !important");
    }
}

function updateSidenavTopOnResize() {
    $(window).on('resize', function () {
        setSidenavTop();
    });
}

function renderSidebar() {

    const sideToggleSideToc = $('#sidetoggle .sidetoc')[0];
    const footer = $('footer');
    const sidetoc = $('.sidetoc');

    if (typeof (sideToggleSideToc) === 'undefined') {
        loadToc();
    } else {
        registerTocEvents();
        if (footer.is(':visible')) {
            sidetoc.addClass('shiftup');
        }

        // Disable transitions during page navigation to prevent flickering
        sidetoc.addClass('no-transition');
        
        // Scroll to active item instantly
        let top = 0;
        $('#toc a.active').parents('li').each(function (i, e) {
            $(e).addClass(active).addClass(expanded);
            $(e).children('a').addClass(active);
            top += $(e).position().top;
        })

        // Instant scroll to active item
        sidetoc.scrollTop(top - 50);
        
        // Re-enable transitions after a brief delay
        setTimeout(function() {
            sidetoc.removeClass('no-transition');
        }, 50);

        if (footer.is(':visible')) {
            sidetoc.addClass('shiftup');
        }

        if (window.location.href.indexOf("articles/intro.html") > -1 && $(window).width() > 850) {
            $('.nav.level1 li:eq(1)').addClass(expanded);
        }

        renderBreadcrumb();
        setSidenavTop();
        setTocHeight();
        
        // Initialize TOC controls after initial setup
        initializeTocControls();
    }

    // Icon mapping configuration
    const ICON_MAP = {
        // Level 1 navigation (exact matches)
        level1: [
            { keywords: ['get started'], icon: '🚀', exact: true },
            { keywords: ['samples', 'tutorials'], icon: '💡' },
            { keywords: ['overview'], icon: '📚', exact: true },
            { keywords: ['studio'], icon: '🎨', exact: true },
            { keywords: ['reference'], icon: '📘', exact: true },
            { keywords: ['extensions'], icon: '🧩', exact: true },
            { keywords: ['themes'], icon: '🌈', exact: true },
            { keywords: ['toolkit'], icon: '🔧', exact: true },
            { keywords: ['tooling'], icon: '🛠️', exact: true }
        ],
        // Sub-level navigation (priority order - most specific first)
        subLevel: [
            // Getting Started
            { keywords: ['getting', 'start'], icon: '🚀', allRequired: true },
            { keywords: ['quick', 'start'], icon: '⚡', allRequired: true },
            { keywords: ['first', 'app'], icon: '🎯', allRequired: true },
            
            // API & Technical Reference
            { keywords: ['api', 'reference'], icon: '📘' },
            { keywords: ['method', 'property'], icon: '🔷' },
            
            // Setup & Configuration
            { keywords: ['install', 'setup'], icon: '⚙️' },
            { keywords: ['configur', 'setting'], icon: '🔧' },
            
            // Guides & Tutorials
            { keywords: ['guide', 'tutorial', 'walkthrough'], icon: '📖' },
            { keywords: ['how to', 'how-to'], icon: '📝' },
            
            // Features & Capabilities
            { keywords: ['feature', 'capability'], icon: '✨' },
            { keywords: ["what's new", 'new feature'], icon: '🆕' },
            
            // Controls & Components
            { keywords: ['control', 'widget'], icon: '🧩' },
            { keywords: ['component', 'element'], icon: '🔳' },
            
            // Platform Targets
            { keywords: ['ios', 'ipad', 'iphone'], icon: '🍎' },
            { keywords: ['android'], icon: '🤖' },
            { keywords: ['windows', 'uwp', 'winui'], icon: '🪟' },
            { keywords: ['wasm', 'webassembly'], icon: '🌐' },
            { keywords: ['macos', 'catalyst'], icon: '💻' },
            { keywords: ['linux', 'gtk', 'skia'], icon: '🐧' },
            { keywords: ['platform', 'target', 'mobile', 'desktop'], icon: '🎯' },
            
            // Build & Compilation
            { keywords: ['build', 'compile'], icon: '🏗️' },
            { keywords: ['package', 'packaging'], icon: '📦' },
            
            // Deployment
            { keywords: ['deploy', 'publish', 'release'], icon: '🚀' },
            
            // Debugging & Diagnostics
            { keywords: ['debug', 'diagnos', 'trace'], icon: '🐛' },
            { keywords: ['log', 'logging'], icon: '📋' },
            
            // Testing
            { keywords: ['test', 'unit', 'integration'], icon: '🧪' },
            
            // Performance
            { keywords: ['performance', 'optimize', 'speed', 'memory'], icon: '⚡' },
            
            // Security & Authentication
            { keywords: ['security', 'secure'], icon: '🔒' },
            { keywords: ['auth', 'permission', 'credential'], icon: '🔑' },
            
            // Data & Storage
            { keywords: ['data', 'database'], icon: '💾' },
            { keywords: ['storage', 'cache'], icon: '🗄️' },
            
            // Networking
            { keywords: ['network', 'http', 'rest', 'request'], icon: '🌐' },
            
            // UI & Layout
            { keywords: ['ui', 'interface'], icon: '🖥️' },
            { keywords: ['layout', 'page'], icon: '📐' },
            
            // XAML
            { keywords: ['xaml', 'markup', 'xml'], icon: '📝' },
            
            // Views & Navigation
            { keywords: ['view', 'screen', 'window'], icon: '📱' },
            { keywords: ['navigation', 'routing', 'navigate'], icon: '🧭' },
            
            // Styling & Theming
            { keywords: ['style', 'styling'], icon: '🎨' },
            { keywords: ['theme', 'theming'], icon: '🌈' },
            { keywords: ['design', 'appearance'], icon: '✨' },
            
            // Animation
            { keywords: ['animation', 'transition', 'motion'], icon: '🎬' },
            
            // Data Binding
            { keywords: ['binding', 'databinding'], icon: '🔗' },
            { keywords: ['mvvm', 'viewmodel'], icon: '🔀' },
            
            // Samples & Examples
            { keywords: ['sample', 'example', 'demo'], icon: '💡' },
            
            // Migration & Updates
            { keywords: ['migration', 'migrat'], icon: '🔄' },
            { keywords: ['upgrade', 'update', 'port'], icon: '⬆️' },
            
            // Troubleshooting
            { keywords: ['troubleshoot', 'issue', 'problem', 'error'], icon: '🔧' },
            { keywords: ['faq', 'question', 'help'], icon: '❓' },
            
            // Contributing & Community
            { keywords: ['contribute', 'contributor', 'contributing'], icon: '🤝' },
            { keywords: ['community', 'forum', 'discussion'], icon: '👥' },
            
            // Development
            { keywords: ['develop', 'development', 'coding'], icon: '💻' },
            
            // Documentation Structure
            { keywords: ['overview', 'about'], icon: '📚' },
            { keywords: ['introduction', 'intro'], icon: '👋' },
            { keywords: ['concept', 'architecture', 'principle'], icon: '💭' },
            { keywords: ['best practice', 'pattern', 'recommendation'], icon: '⭐' },
            
            // Resources & Assets
            { keywords: ['resource', 'asset'], icon: '📁' },
            { keywords: ['image', 'icon'], icon: '🖼️' },
            { keywords: ['video', 'media', 'audio'], icon: '🎥' },
            
            // Localization & Accessibility
            { keywords: ['localization', 'translation', 'language', 'globalization'], icon: '🌍' },
            { keywords: ['accessibility', 'a11y'], icon: '♿' },
            
            // Tools & Extensions
            { keywords: ['tool', 'utility', 'helper'], icon: '🔨' },
            { keywords: ['extension', 'plugin', 'addon'], icon: '🧩' },
            
            // Packages & Dependencies
            { keywords: ['nuget', 'library', 'dependency'], icon: '📦' },
            
            // Versioning & Release
            { keywords: ['version', 'changelog', 'release note'], icon: '📋' },
            
            // Legal
            { keywords: ['license', 'legal', 'terms'], icon: '⚖️' },
            
            // Feedback
            { keywords: ['feedback', 'report', 'contact'], icon: '💬' }
        ],
        default: '📄'
    };

    /**
     * Determines the appropriate icon for a given text and level
     * @param {string} text - The text to match against
     * @param {boolean} isLevel1 - Whether this is a level 1 item
     * @returns {string} The matched icon or default icon
     */
    function getIconForItem(text, isLevel1) {
        const lowerText = text.toLowerCase();
        
        // Check level 1 specific icons first
        if (isLevel1) {
            for (const rule of ICON_MAP.level1) {
                if (rule.exact && lowerText === rule.keywords[0]) {
                    return rule.icon;
                }
                if (!rule.exact && rule.keywords.some(kw => lowerText.includes(kw))) {
                    return rule.icon;
                }
            }
        }
        
        // Check sub-level patterns
        for (const rule of ICON_MAP.subLevel) {
            if (rule.allRequired) {
                // All keywords must be present
                if (rule.keywords.every(kw => lowerText.includes(kw))) {
                    return rule.icon;
                }
            } else {
                // Any keyword matches
                if (rule.keywords.some(kw => lowerText.includes(kw))) {
                    return rule.icon;
                }
            }
        }
        
        return ICON_MAP.default;
    }

    function registerTocEvents() {
        // Assign contextual icons to first-level nodes only
        $('.toc .nav > li').each(function() {
            const $li = $(this);
            const $stub = $li.children('.expand-stub');
            const $link = $li.children('a');
            const text = $link.text();
            const isLevel1 = $li.parent().hasClass('level1');
            
            // Get appropriate icon using centralized logic
            const icon = getIconForItem(text, isLevel1);
            
            // Assign icon to expand-stub if it exists
            if ($stub.length > 0) {
                $stub.attr('data-icon', icon);
            }
            
            // Also assign icon directly to the link for nodes without expand-stubs
            if ($link.length > 0) {
                $link.attr('data-icon', icon);
            }
        });
        
        // Debounce scroll calculations
        let scrollTimeout = null;
        
        const smoothScrollToItem = function($parent) {
            if (scrollTimeout) clearTimeout(scrollTimeout);
            
            scrollTimeout = setTimeout(function() {
                const $sidetoc = $('.sidetoc');
                const itemTop = $parent.position().top;
                const scrollTop = $sidetoc.scrollTop();
                const containerHeight = $sidetoc.height();
                const itemHeight = $parent.outerHeight();
                
                // Only scroll if item is out of view
                const isAboveViewport = itemTop < 80; // Account for top padding
                const isBelowViewport = itemTop + itemHeight > containerHeight - 50;
                
                if (isAboveViewport || isBelowViewport) {
                    // Use requestAnimationFrame for smoother animation
                    requestAnimationFrame(function() {
                        let newScrollTop;
                        
                        if (isAboveViewport) {
                            // Item is above viewport - scroll to show it at top
                            newScrollTop = scrollTop + itemTop - 100;
                        } else {
                            // Item is below viewport - scroll to show it at bottom
                            newScrollTop = scrollTop + (itemTop - containerHeight + itemHeight + 100);
                        }
                        
                        $sidetoc.animate({
                            scrollTop: newScrollTop
                        }, 250, 'swing');
                    });
                }
            }, 50);
        };
        
        $('.toc .nav > li > .expand-stub').on('click', function (e) {
            const $parent = $(e.target).parent();
            const wasExpanded = $parent.hasClass(expanded);
            $parent.toggleClass(expanded);
            
            // Smooth scroll to keep expanded item in view
            if (!wasExpanded) {
                smoothScrollToItem($parent);
            }
        });
        $('.toc .nav > li > .expand-stub + a:not([href])').on('click', function (e) {
            const $parent = $(e.target).parent();
            const wasExpanded = $parent.hasClass(expanded);
            $parent.toggleClass(expanded);
            
            // Smooth scroll to keep expanded item in view
            if (!wasExpanded) {
                smoothScrollToItem($parent);
            }
        });
        $('#toc_filter_input').on('input', function () {
            const val = this.value;
            if (val === '') {
                // Clear 'filtered' class
                $('#toc li').removeClass(filtered).removeClass(hide);
                return;
            }

            // Get leaf nodes
            const tocLineAnchor = $('#toc li>a');

            tocLineAnchor.filter(function (i, e) {
                return $(e).siblings().length === 0
            }).each(function (j, anchor) {
                let text = $(anchor).attr('title');
                const parent = $(anchor).parent();
                const parentNodes = parent.parents('ul>li');
                for (let k = 0; k < parentNodes.length; k++) {
                    let parentText = $(parentNodes[k]).children('a').attr('title');
                    if (parentText) text = parentText + '.' + text;
                }

                if (filterNavItem(text, val)) {
                    parent.addClass(show);
                    parent.removeClass(hide);
                } else {
                    parent.addClass(hide);
                    parent.removeClass(show);
                }
            });

            tocLineAnchor.filter(function (i, e) {
                return $(e).siblings().length > 0
            }).each(function (i, anchor) {
                const parent = $(anchor).parent();
                if (parent.find('li.show').length > 0) {
                    parent.addClass(show);
                    parent.addClass(filtered);
                    parent.removeClass(hide);
                } else {
                    parent.addClass(hide);
                    parent.removeClass(show);
                    parent.removeClass(filtered);
                }
            })

            function filterNavItem(name, text) {

                if (!text) return true;

                return name && name.toLowerCase().indexOf(text.toLowerCase()) > -1;

            }
        });
    }

    function loadToc() {
        let tocPath = $("meta[property='docfx\\:tocrel']").attr("content");

        if (!tocPath) {
            return;
        }
        tocPath = tocPath.replace(/\\/g, '/');
        $('#sidetoc').load(tocPath + " #sidetoggle > div", function () {
            const index = tocPath.lastIndexOf('/');
            let tocrel = '';

            if (index > -1) {
                tocrel = tocPath.substr(0, index + 1);
            }

            const currentHref = getAbsolutePath(window.location.pathname);

            $('#sidetoc').find('a[href]').each(function (i, e) {
                let href = $(e).attr("href");
                if (isRelativePath(href)) {
                    href = tocrel + href;
                    $(e).attr("href", href);
                }

                if (getAbsolutePath(e.href) === currentHref) {
                    $(e).addClass(active);
                }

                $(e).breakWord();
            });

            renderSidebar();
            const body = $('body');
            const searchResult = $('#search-results');

            if (searchResult.length !== 0) {
                $('#search').show();
                body.trigger("searchEvent");
            }

            // if the target of the click isn't the container nor a descendant of the container
            body.on('mouseup', function (e) {
                if (!searchResult.is(e.target) && searchResult.has(e.target).length === 0) {
                    searchResult.hide();
                }
            });
            
            // Initialize TOC resize and collapse functionality
            initializeTocControls();
        });
    }
    
    // TOC Collapse and Resize Functionality
    function initializeTocControls() {
        const sidetoggle = $('#sidetoggle');
        const sidetoc = $('.sidetoc');
        const collapseToggle = $('.toc-collapse-toggle');
        const expandButton = $('.toc-expand-button');
        const resizeHandle = $('.toc-resize-handle');
        const article = $('.article.grid-right');
        
        // Load saved state from localStorage
        const isCollapsed = localStorage.getItem('toc-collapsed') === 'true';
        const savedWidth = localStorage.getItem('toc-width');
        
        if (savedWidth && !isCollapsed) {
            const width = parseInt(savedWidth);
            sidetoggle.css('width', width + 'px');
            article.css('margin-left', (width + 20) + 'px');
        }
        
        if (isCollapsed) {
            sidetoggle.addClass('collapsed');
            expandButton.addClass('visible');
            article.addClass('toc-collapsed');
        }
        
        // Collapse functionality
        collapseToggle.on('click', function() {
            sidetoggle.addClass('collapsed');
            expandButton.addClass('visible');
            article.addClass('toc-collapsed');
            localStorage.setItem('toc-collapsed', 'true');
        });
        
        expandButton.on('click', function() {
            sidetoggle.removeClass('collapsed');
            expandButton.removeClass('visible');
            article.removeClass('toc-collapsed');
            const savedWidth = localStorage.getItem('toc-width') || '280';
            const width = parseInt(savedWidth);
            sidetoggle.css('width', width + 'px');
            article.css('margin-left', (width + 20) + 'px');
            localStorage.setItem('toc-collapsed', 'false');
        });
        
        // Resize functionality
        let isResizing = false;
        let startX = 0;
        let startWidth = 0;
        const minWidth = 200;
        const maxWidth = 600;
        
        resizeHandle.on('mousedown', function(e) {
            isResizing = true;
            startX = e.pageX;
            startWidth = sidetoggle.width();
            resizeHandle.addClass('resizing');
            $('body').css('cursor', 'ew-resize').css('user-select', 'none');
            e.preventDefault();
        });
        
        $(document).on('mousemove', function(e) {
            if (!isResizing) return;
            
            const diff = e.pageX - startX;
            let newWidth = startWidth + diff;
            
            // Constrain width
            newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
            
            sidetoggle.css('width', newWidth + 'px');
            article.css('margin-left', (newWidth + 20) + 'px');
            e.preventDefault();
        });
        
        $(document).on('mouseup', function() {
            if (isResizing) {
                isResizing = false;
                resizeHandle.removeClass('resizing');
                $('body').css('cursor', '').css('user-select', '');
                
                // Save width to localStorage
                const currentWidth = sidetoggle.width();
                localStorage.setItem('toc-width', currentWidth);
            }
        });
    }
}
