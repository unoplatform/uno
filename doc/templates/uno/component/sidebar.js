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

    function registerTocEvents() {
        // Assign contextual icons to first-level nodes only
        $('.toc .nav > li').each(function() {
            const $li = $(this);
            const $stub = $li.children('.expand-stub');
            const $link = $li.children('a');
            const text = $link.text().toLowerCase();
            const isLevel1 = $li.parent().hasClass('level1');
            
            let icon = '📄'; // default document icon
            
            // Level 1 specific icons (exact matches for main navigation)
            if (isLevel1) {
                if (text.includes('get started')) icon = '🚀';
                else if (text.includes('samples') || text.includes('tutorials')) icon = '💡';
                else if (text === 'overview') icon = '📚';
                else if (text === 'studio') icon = '🎨';
                else if (text === 'reference') icon = '📘';
                else if (text === 'extensions') icon = '🧩';
                else if (text === 'themes') icon = '🌈';
                else if (text === 'toolkit') icon = '🔧';
                else if (text === 'tooling') icon = '🛠️';
            }
            // Sub-level icon assignment with priority order (most specific first)
            else if (text.includes('getting') && text.includes('start')) icon = '🚀';
            else if (text.includes('quick') && text.includes('start')) icon = '⚡';
            else if (text.includes('first') && text.includes('app')) icon = '🎯';
            
            // API & Technical Reference
            else if (text.includes('api') || text.includes('reference')) icon = '📘';
            else if (text.includes('method') || text.includes('property')) icon = '🔷';
            
            // Setup & Configuration
            else if (text.includes('install') || text.includes('setup')) icon = '⚙️';
            else if (text.includes('configur') || text.includes('setting')) icon = '🔧';
            
            // Guides & Tutorials
            else if (text.includes('guide') || text.includes('tutorial') || text.includes('walkthrough')) icon = '📖';
            else if (text.includes('how to') || text.includes('how-to')) icon = '📝';
            
            // Features & Capabilities
            else if (text.includes('feature') || text.includes('capability')) icon = '✨';
            else if (text.includes('what\'s new') || text.includes('new feature')) icon = '🆕';
            
            // Controls & Components (Uno Platform specific)
            else if (text.includes('control') || text.includes('widget')) icon = '🧩';
            else if (text.includes('component') || text.includes('element')) icon = '🔳';
            
            // Platform Targets (Uno Platform specific)
            else if (text.includes('ios') || text.includes('ipad') || text.includes('iphone')) icon = '🍎';
            else if (text.includes('android')) icon = '🤖';
            else if (text.includes('windows') || text.includes('uwp') || text.includes('winui')) icon = '🪟';
            else if (text.includes('wasm') || text.includes('webassembly')) icon = '🌐';
            else if (text.includes('macos') || text.includes('catalyst')) icon = '💻';
            else if (text.includes('linux') || text.includes('gtk') || text.includes('skia')) icon = '🐧';
            else if (text.includes('platform') || text.includes('target') || text.includes('mobile') || text.includes('desktop')) icon = '🎯';
            
            // Build & Compilation
            else if (text.includes('build') || text.includes('compile')) icon = '🏗️';
            else if (text.includes('package') || text.includes('packaging')) icon = '📦';
            
            // Deployment & Release
            else if (text.includes('deploy') || text.includes('publish') || text.includes('release')) icon = '🚀';
            
            // Debugging & Diagnostics
            else if (text.includes('debug') || text.includes('diagnos') || text.includes('trace')) icon = '🐛';
            else if (text.includes('log') || text.includes('logging')) icon = '📋';
            
            // Testing
            else if (text.includes('test') || text.includes('unit') || text.includes('integration')) icon = '🧪';
            
            // Performance
            else if (text.includes('performance') || text.includes('optimize') || text.includes('speed') || text.includes('memory')) icon = '⚡';
            
            // Security & Authentication
            else if (text.includes('security') || text.includes('secure')) icon = '🔒';
            else if (text.includes('auth') || text.includes('permission') || text.includes('credential')) icon = '🔑';
            
            // Data & Storage
            else if (text.includes('data') || text.includes('database')) icon = '💾';
            else if (text.includes('storage') || text.includes('cache')) icon = '🗄️';
            
            // Networking
            else if (text.includes('network') || text.includes('http') || text.includes('rest') || text.includes('request')) icon = '🌐';
            
            // UI & Layout (Uno Platform specific)
            else if (text.includes('ui') || text.includes('interface')) icon = '🖥️';
            else if (text.includes('layout') || text.includes('page')) icon = '📐';
            
            // XAML (Uno Platform core)
            else if (text.includes('xaml') || text.includes('markup') || text.includes('xml')) icon = '📝';
            
            // Views & Navigation
            else if (text.includes('view') || text.includes('screen') || text.includes('window')) icon = '📱';
            else if (text.includes('navigation') || text.includes('routing') || text.includes('navigate')) icon = '🧭';
            
            // Styling & Theming
            else if (text.includes('style') || text.includes('styling')) icon = '🎨';
            else if (text.includes('theme') || text.includes('theming')) icon = '🌈';
            else if (text.includes('design') || text.includes('appearance')) icon = '✨';
            
            // Animation
            else if (text.includes('animation') || text.includes('transition') || text.includes('motion')) icon = '🎬';
            
            // Data Binding (Uno Platform specific)
            else if (text.includes('binding') || text.includes('databinding')) icon = '🔗';
            else if (text.includes('mvvm') || text.includes('viewmodel')) icon = '🔀';
            
            // Samples & Examples
            else if (text.includes('sample') || text.includes('example') || text.includes('demo')) icon = '💡';
            
            // Migration & Updates
            else if (text.includes('migration') || text.includes('migrat')) icon = '🔄';
            else if (text.includes('upgrade') || text.includes('update') || text.includes('port')) icon = '⬆️';
            
            // Troubleshooting
            else if (text.includes('troubleshoot') || text.includes('issue') || text.includes('problem') || text.includes('error')) icon = '🔧';
            else if (text.includes('faq') || text.includes('question') || text.includes('help')) icon = '❓';
            
            // Contributing & Community
            else if (text.includes('contribute') || text.includes('contributor') || text.includes('contributing')) icon = '🤝';
            else if (text.includes('community') || text.includes('forum') || text.includes('discussion')) icon = '👥';
            
            // Development
            else if (text.includes('develop') || text.includes('development') || text.includes('coding')) icon = '💻';
            
            // Documentation Structure
            else if (text.includes('overview') || text.includes('about')) icon = '📚';
            else if (text.includes('introduction') || text.includes('intro')) icon = '👋';
            else if (text.includes('concept') || text.includes('architecture') || text.includes('principle')) icon = '💭';
            else if (text.includes('best practice') || text.includes('pattern') || text.includes('recommendation')) icon = '⭐';
            
            // Resources & Assets
            else if (text.includes('resource') || text.includes('asset')) icon = '📁';
            else if (text.includes('image') || text.includes('icon')) icon = '🖼️';
            else if (text.includes('video') || text.includes('media') || text.includes('audio')) icon = '🎥';
            
            // Localization & Accessibility
            else if (text.includes('localization') || text.includes('translation') || text.includes('language') || text.includes('globalization')) icon = '🌍';
            else if (text.includes('accessibility') || text.includes('a11y')) icon = '♿';
            
            // Tools & Extensions
            else if (text.includes('tool') || text.includes('utility') || text.includes('helper')) icon = '🔨';
            else if (text.includes('extension') || text.includes('plugin') || text.includes('addon')) icon = '🧩';
            
            // Packages & Dependencies (NuGet specific)
            else if (text.includes('nuget') || text.includes('library') || text.includes('dependency')) icon = '📦';
            
            // Versioning & Release
            else if (text.includes('version') || text.includes('changelog') || text.includes('release note')) icon = '📋';
            
            // Legal
            else if (text.includes('license') || text.includes('legal') || text.includes('terms')) icon = '⚖️';
            
            // Feedback
            else if (text.includes('feedback') || text.includes('report') || text.includes('contact')) icon = '💬';
            
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
