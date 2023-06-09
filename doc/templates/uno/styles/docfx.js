const active = 'active';
const expanded = 'in';
const filtered = 'filtered';
const show = 'show';
const hide = 'hide';
const collapsed = 'collapsed';

// workaround for gulp-uglify changing order of execution on $.fn func assignments
Object.assign($.fn, { breakWord });

workAroundFixedHeaderForAnchors();
highlight();
enableSearch();

renderTables();
renderAlerts();
updateAlertHeightOnResize();
renderLinks();
renderSidebar();
renderAffix();

renderNavbar();
renderLogo();
updateLogo()
updateLogoOnResize();
updateNavbarHeightOnResize();
updateTocHeightOnResize();
updateSidenavTopOnResize();
renderFooter();
breakText();
renderTabs();
updateLogo();

//Setup Affix
function renderAffix() {
    const hierarchy = getHierarchy();

    if (hierarchy && hierarchy.length > 0) {
        let html = '<h5 class="title">In This Article</h5>'
        html += formList(hierarchy, ['nav', 'bs-docs-sidenav']);

        $("#affix").empty().append(html);

        if ($('footer').is(':visible')) {
            $(".sideaffix").css("bottom", "70px");
        }

        $('#affix a').on('click', function (e) {
            const scrollspy = $('[data-spy="scroll"]').data()['bs.scrollspy'];
            const target = e.target.hash;
            if (scrollspy && target) {
                scrollspy.activate(target);
            }
        });

        const contribution = $('.contribution');
        const contributionDiv = contribution.get(0).outerHTML;
        contribution.remove();
        $('.sideaffix').append(contributionDiv);

    }

    function getHierarchy() {
        // supported headers are h1, h2, h3, and h4
        const $headers = $($.map(['h1', 'h2', 'h3', 'h4'], function (h) { return ".article article " + h; }).join(", "));

        // a stack of hierarchy items that are currently being built
        const stack = [];
        $headers.each(function (i, e) {
            if (!e.id) {
                return;
            }

            const item = {
                name: htmlEncode($(e).text()),
                href: "#" + e.id,
                items: []
            };

            if (!stack.length) {
                stack.push({ type: e.tagName, siblings: [item] });
                return;
            }

            const frame = stack[stack.length - 1];
            if (e.tagName === frame.type) {
                frame.siblings.push(item);
            } else if (e.tagName[1] > frame.type[1]) {
                // we are looking at a child of the last element of frame.siblings.
                // push a frame onto the stack. After we've finished building this item's children,
                // we'll attach it as a child of the last element
                stack.push({ type: e.tagName, siblings: [item] });
            } else {  // e.tagName[1] < frame.type[1]
                // we are looking at a sibling of an ancestor of the current item.
                // pop frames from the stack, building items as we go, until we reach the correct level at which to attach this item.
                while (e.tagName[1] < stack[stack.length - 1].type[1]) {
                    buildParent();
                }
                if (e.tagName === stack[stack.length - 1].type) {
                    stack[stack.length - 1].siblings.push(item);
                } else {
                    stack.push({ type: e.tagName, siblings: [item] });
                }
            }
        });
        while (stack.length > 1) {
            buildParent();
        }

        function buildParent() {
            const childrenToAttach = stack.pop();
            const parentFrame = stack[stack.length - 1];
            const parent = parentFrame.siblings[parentFrame.siblings.length - 1];
            $.each(childrenToAttach.siblings, function (i, child) {
                parent.items.push(child);
            });
        }
        if (stack.length > 0) {

            const topLevel = stack.pop().siblings;
            if (topLevel.length === 1) {  // if there's only one topmost header, dump it
                return topLevel[0].items;
            }
            return topLevel;
        }
        return undefined;
    }

    function htmlEncode(str) {
        if (!str) return str;
        return str
            .replace(/&/g, '&amp;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function htmlDecode(str) {
        if (!str) return str;
        return str
            .replace(/&quot;/g, '"')
            .replace(/&#39;/g, "'")
            .replace(/&lt;/g, '<')
            .replace(/&gt;/g, '>')
            .replace(/&amp;/g, '&');
    }

    function cssEscape(str) {
        // see: http://stackoverflow.com/questions/2786538/need-to-escape-a-special-character-in-a-jquery-selector-string#answer-2837646
        if (!str) return str;
        return str
            .replace(/[!"#$%&'()*+,.\/:;<=>?@[\\\]^`{|}~]/g, "\\$&");
    }
}

// Styling for alerts.
function setAlertHeight(){
    var maxHeight = Math.max.apply(null, $(".col-md-6 div.alert").map(function ()
    {
        return $(this).outerHeight();
    }).get());

    $('.alert').css('height', maxHeight);
    
}

function updateAlertHeightOnResize() {
    $(window).on('resize', function () {
        $('.alert').css('height', 'auto');
        setAlertHeight();
    });
}

function renderAlerts() {
    $('.NOTE, .TIP').addClass('alert alert-info');
    $('.WARNING').addClass('alert alert-warning');
    $('.IMPORTANT, .CAUTION').addClass('alert alert-danger');
    setAlertHeight();

}

function renderBreadcrumb() {
    const breadcrumb = [];

    $('#navbar a.active').each(function (i, e) {
        breadcrumb.push({
            href: e.href,
            name: e.innerHTML
        });
    })
    $('#toc a.active').each(function (i, e) {
        breadcrumb.push({
            href: e.href,
            name: e.innerHTML
        });
    })

    const html = formList(breadcrumb, 'breadcrumb');
    $('#breadcrumb').html(html);
}

// Show footer
function renderFooter() {
    initFooter();
    $(window).on("scroll", () => showFooterCore());

    function initFooter() {
        if (needFooter()) {
            shiftUpBottomCss();
            $("footer").show();
        } else {
            resetBottomCss();
            $("footer").hide();
        }
    }

    function showFooterCore() {
        if (needFooter()) {
            shiftUpBottomCss();
            $("footer").fadeIn();
        } else {
            resetBottomCss();
            $("footer").fadeOut();
        }
    }

    function needFooter() {
        const scrollHeight = $(document).height();
        const scrollPosition = $(window).height() + $(window).scrollTop();
        return (scrollHeight - scrollPosition) < 1;
    }

    function resetBottomCss() {
        $(".sidetoc").removeClass("shiftup");
        $(".sideaffix").removeClass("shiftup");
    }

    function shiftUpBottomCss() {
        $(".sidetoc").addClass("shiftup");
        $(".sideaffix").addClass("shiftup");
    }
}

// Open links to different host in a new window.
function renderLinks() {
    if ($("meta[property='docfx:newtab']").attr("content") === "true") {
        $(document.links).filter(function () {
            return this.hostname !== window.location.hostname;
        }).attr('target', '_blank');
    }
}

function setNavbarHeight() {
    let headerHeight = $("#header-container").outerHeight();
    let intViewportHeight = window.innerHeight;
    let maxHeightNavbar = intViewportHeight - headerHeight;
    $("#navbar").css("max-height", maxHeightNavbar);
}


/**
 * Load the navbar from the uno website
 */
function initializeNavbar() {

    const navbar = document.querySelector("header > .navbar");
    if (document.body.classList.contains("front-page")) {
        let last_known_scroll_position = 0;
        let ticking = false;

        function doSomething(scroll_pos) {
            if (scroll_pos >= 100) navbar.classList.add("scrolled");
            else navbar.classList.remove("scrolled");
        }

        window.addEventListener("scroll", function () {
            last_known_scroll_position = window.scrollY;

            if (!ticking) {
                window.requestAnimationFrame(function () {
                    doSomething(last_known_scroll_position);
                    ticking = false;
                });

                ticking = true;
            }
        });
    }

    const unoMenuReq = new XMLHttpRequest();
    const unoMenuEndpoint = "https://platform.uno/wp-json/wp/v2/menu";
    const $navbar = document.getElementById("navbar");
    let wordpressMenuHasLoaded = false;

    unoMenuReq.open("get", unoMenuEndpoint, true);

    if (typeof navbar !== "undefined") {
        unoMenuReq.onload = function () {
            if (unoMenuReq.status === 200 && unoMenuReq.responseText) {
                $navbar.innerHTML = JSON.parse(
                    unoMenuReq.responseText
                );
                wordpressMenuHasLoaded = true;
                $(document).trigger("wordpressMenuHasLoaded");
            }
        };
        unoMenuReq.onerror = function (e) {
        };
        unoMenuReq.send();
    }

    $(document).ajaxComplete(function (event, xhr, settings) {
        const docFxNavbarHasLoaded = settings.url === "toc.html";

        if (docFxNavbarHasLoaded && wordpressMenuHasLoaded) {
            const $docfxNavbar = $navbar.getElementsByClassName("navbar-nav");
            $docfxNavbar[0].className += " hidden";

        }
    });

    setNavbarHeight();

}

/**
 * Changes the logo on resize
*/

function updateLogo() {
    const curWidth = window.innerWidth;
    const headerLogo = document.getElementById('logo');
    if (curWidth < 980) {
        const mobileLogo = new URL('UnoLogoSmall.png', headerLogo.src).href;
        headerLogo.src = mobileLogo;
    } else {
        const deskLogo = new URL('uno-logo.svg', headerLogo.src).href;
        headerLogo.src = deskLogo;
    }
}

function updateLogoOnResize() {
    $(window).on('resize', function () {
        updateLogo();
    });
}


function updateNavbarHeightOnResize() {
    $(window).on('resize', function () {
        setNavbarHeight();
    });
}


// Update href in navbar
function renderNavbar() {
    const navbar = $('#navbar ul')[0];
    if (typeof (navbar) === 'undefined') {
        loadNavbar();
    } else {
        $('#navbar ul a.active').parents('li').addClass(active);
        renderBreadcrumb();
    }

    function loadNavbar() {
        let navbarPath = $("meta[property='docfx\\:navrel']").attr("content");
        if (!navbarPath) {
            return;
        }
        navbarPath = navbarPath.replace(/\\/g, '/');
        let tocPath = $("meta[property='docfx\\:tocrel']").attr("content") || '';
        if (tocPath) tocPath = tocPath.replace(/\\/g, '/');
        $.get(navbarPath, function (data) {
            $(data).find("#toc>ul").appendTo("#navbar");
            const index = navbarPath.lastIndexOf('/');
            let navrel = '';
            if (index > -1) {
                navrel = navbarPath.substr(0, index + 1);
            }
            $('#navbar>ul').addClass('navbar-nav');

            const currentAbsPath = getAbsolutePath(window.location.pathname);

            // set active item
            $('#navbar').find('a[href]').each(function (i, e) {
                let href = $(e).attr("href");
                if (isRelativePath(href)) {
                    href = navrel + href;
                    $(e).attr("href", href);

                    // TODO: currently only support one level navbar
                    let isActive = false;
                    let originalHref = e.name;
                    if (originalHref) {
                        originalHref = navrel + originalHref;
                        if (getDirectory(getAbsolutePath(originalHref)) === getDirectory(getAbsolutePath(tocPath))) {
                            isActive = true;
                        }
                    } else {
                        if (getAbsolutePath(href) === currentAbsPath) {

                            const dropdown = $(e).attr('data-toggle') === "dropdown";

                            if (!dropdown) {
                                isActive = true;
                            }
                        }
                    }
                    if (isActive) {
                        $(e).addClass(active);
                    }
                }
            });
            renderNavbar();
        });
    }
}

function renderLogo() {
    // For LOGO SVG
    // Replace SVG with inline SVG
    // http://stackoverflow.com/questions/11978995/how-to-change-color-of-svg-image-using-css-jquery-svg-image-replacement
    $('img.svg').each(function () {
        const $img = jQuery(this);
        const imgID = $img.attr('id');
        const imgClass = $img.attr('class');
        const imgURL = $img.attr('src');

        jQuery.get(imgURL, function (data) {
            // Get the SVG tag, ignore the rest
            let $svg = $(data).find('svg');

            // Add replaced image's ID to the new SVG
            if (typeof imgID !== 'undefined') {
                $svg = $svg.attr('id', imgID);
            }
            // Add replaced image's classes to the new SVG
            if (typeof imgClass !== 'undefined') {
                $svg = $svg.attr('class', imgClass + ' replaced-svg');
            }

            // Remove any invalid XML tags as per http://validator.w3.org
            $svg = $svg.removeAttr('xmlns:a');

            // Replace image with new SVG
            $img.replaceWith($svg);

        }, 'xml');
    });
}

function setTocHeight() {
    if($(window).width() < 767) {
        let headerHeight = $("#header-container").outerHeight();
        let breadcrumbHeight = $("#breadcrumb").outerHeight();
        let tocToggleHeight = $(".btn.toc-toggle.collapse").outerHeight();
        let sidefilterHeight = 65; //65px from sidefilter height
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
    let sidefilterHeight = $(".sidefilter").outerHeight();
    let sidenavTop = headerHeight + breadcrumbHeight;
    let sidefilterTop = headerHeight + breadcrumbHeight;
    let sidetocTop = sidefilterTop + sidefilterHeight;
    let articleMarginTopDesk = sidenavTop + tocToggleHeight + 30; //30px from .sidenav padding top and bottom
    let articleMarginTopMobile = sidenavTop;
    $(".sidenav").css("top", sidenavTop);
    $(".sidefilter").css("top", sidenavTop);
    $(".sidetoc").css("top", sidetocTop);
    if($(window).width() < 767) {
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

        // Scroll to active item
        let top = 0;
        $('#toc a.active').parents('li').each(function (i, e) {
            $(e).addClass(active).addClass(expanded);
            $(e).children('a').addClass(active);
            top += $(e).position().top;
        })

        sidetoc.scrollTop(top - 50);

        if (footer.is(':visible')) {
            sidetoc.addClass('shiftup');
        }

        if (window.location.href.indexOf("articles/intro.html") > -1 && $(window).width() > 850) {
            $('.nav.level1 li:eq(1)').addClass(expanded);
        }

        renderBreadcrumb();
        setSidenavTop();
        setTocHeight();
    }

    function registerTocEvents() {
        $('.toc .nav > li > .expand-stub').on('click', function (e) {
            $(e.target).parent().toggleClass(expanded);
        });
        $('.toc .nav > li > .expand-stub + a:not([href])').on('click', function (e) {
            $(e.target).parent().toggleClass(expanded);
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
        });
    }
}

function renderTabs() {

    const contentAttrs = {
        id: 'data-bi-id',
        name: 'data-bi-name',
        type: 'data-bi-type'
    };

    const Tab = (function () {
        function Tab(li, a, section) {
            this.li = li;
            this.a = a;
            this.section = section;
        }

        Object.defineProperty(Tab.prototype, "tabIds", {
            get: function () {
                return this.a.getAttribute('data-tab').split(' ');
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "condition", {
            get: function () {
                return this.a.getAttribute('data-condition');
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "visible", {
            get: function () {
                return !this.li.hasAttribute('hidden');
            },
            set: function (value) {
                if (value) {
                    this.li.removeAttribute('hidden');
                    this.li.removeAttribute('aria-hidden');
                } else {
                    this.li.setAttribute('hidden', 'hidden');
                    this.li.setAttribute('aria-hidden', 'true');
                }
            },
            enumerable: true,
            configurable: true
        });

        Object.defineProperty(Tab.prototype, "selected", {
            get: function () {
                return !this.section.hasAttribute('hidden');
            },
            set: function (value) {
                if (value) {
                    this.a.setAttribute('aria-selected', 'true');
                    this.a.tabIndex = 0;
                    this.section.removeAttribute('hidden');
                    this.section.removeAttribute('aria-hidden');
                } else {
                    this.a.setAttribute('aria-selected', 'false');
                    this.a.tabIndex = -1;
                    this.section.setAttribute('hidden', 'hidden');
                    this.section.setAttribute('aria-hidden', 'true');
                }
            },
            enumerable: true,
            configurable: true
        });

        Tab.prototype.focus = function () {
            this.a.focus();
        };

        return Tab;

    }());

    initTabs(document.body);

    function initTabs(container) {
        const queryStringTabs = readTabsQueryStringParam();
        const elements = container.querySelectorAll('.tabGroup');
        const state = {groups: [], selectedTabs: []};
        for (let i = 0; i < elements.length; i++) {
            const group = initTabGroup(elements.item(i));
            if (!group.independent) {
                updateVisibilityAndSelection(group, state);
                state.groups.push(group);
            }
        }
        container.addEventListener('click', function (event) {
            return handleClick(event, state);
        });
        if (state.groups.length === 0) {
            return state;
        }
        selectTabs(queryStringTabs, container);
        updateTabsQueryStringParam(state);
        notifyContentUpdated();
        return state;
    }

    function initTabGroup(element) {

        const group = {
            independent: element.hasAttribute('data-tab-group-independent'),
            tabs: []
        };

        let li = element.firstElementChild.firstElementChild;
        while (li) {
            const a = li.firstElementChild;
            a.setAttribute(contentAttrs.name, 'tab');

            const dataTab = a.getAttribute('data-tab').replace(/\+/g, ' ');
            a.setAttribute('data-tab', dataTab);

            const section = element.querySelector("[id=\"" + a.getAttribute('aria-controls') + "\"]");
            const tab = new Tab(li, a, section);
            group.tabs.push(tab);

            li = li.nextElementSibling;
        }

        element.setAttribute(contentAttrs.name, 'tab-group');
        element.tabGroup = group;

        return group;
    }

    function updateVisibilityAndSelection(group, state) {
        let anySelected = false;
        let firstVisibleTab;

        for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
            let tab = _a[_i];
            tab.visible = tab.condition === null || state.selectedTabs.indexOf(tab.condition) !== -1;
            if (tab.visible) {
                if (!firstVisibleTab) {
                    firstVisibleTab = tab;
                }
            }
            tab.selected = tab.visible && arraysIntersect(state.selectedTabs, tab.tabIds);
            anySelected = anySelected || tab.selected;
        }

        if (!anySelected) {
            for (let _b = 0, _c = group.tabs; _b < _c.length; _b++) {
                const tabIds = _c[_b].tabIds;
                for (let _d = 0, tabIds_1 = tabIds; _d < tabIds_1.length; _d++) {
                    const tabId = tabIds_1[_d];
                    const index = state.selectedTabs.indexOf(tabId);
                    if (index === -1) {
                        continue;
                    }
                    state.selectedTabs.splice(index, 1);
                }
            }
            const tab = firstVisibleTab;
            tab.selected = true;
            state.selectedTabs.push(tab.tabIds[0]);
        }
    }

    function getTabInfoFromEvent(event) {

        if (!(event.target instanceof HTMLElement)) {
            return null;
        }

        const anchor = event.target.closest('a[data-tab]');

        if (anchor === null) {
            return null;
        }

        const tabIds = anchor.getAttribute('data-tab').split(' ');
        const group = anchor.parentElement.parentElement.parentElement.tabGroup;

        if (group === undefined) {
            return null;
        }

        return {tabIds: tabIds, group: group, anchor: anchor};
    }

    function handleClick(event, state) {
        const info = getTabInfoFromEvent(event);

        if (info === null) {
            return;
        }

        event.preventDefault();
        info.anchor.href = 'javascript:';

        setTimeout(function () {
            return info.anchor.href = '#' + info.anchor.getAttribute('aria-controls');
        });

        const tabIds = info.tabIds, group = info.group;
        const originalTop = info.anchor.getBoundingClientRect().top;

        if (group.independent) {
            for (let _i = 0, _a = group.tabs; _i < _a.length; _i++) {
                const tab = _a[_i];
                tab.selected = arraysIntersect(tab.tabIds, tabIds);
            }
        } else {
            if (arraysIntersect(state.selectedTabs, tabIds)) {
                return;
            }
            const previousTabId = group.tabs.filter(function (t) {
                return t.selected;
            })[0].tabIds[0];
            state.selectedTabs.splice(state.selectedTabs.indexOf(previousTabId), 1, tabIds[0]);
            for (let _b = 0, _c = state.groups; _b < _c.length; _b++) {
                const group_1 = _c[_b];
                updateVisibilityAndSelection(group_1, state);
            }
            updateTabsQueryStringParam(state);
        }
        notifyContentUpdated();
        const top = info.anchor.getBoundingClientRect().top;
        if (top !== originalTop && event instanceof MouseEvent) {
            window.scrollTo(0, window.pageYOffset + top - originalTop);
        }
    }

    function selectTabs(tabIds) {
        for (let _i = 0, tabIds_1 = tabIds; _i < tabIds_1.length; _i++) {
            const tabId = tabIds_1[_i];
            const a = document.querySelector(".tabGroup > ul > li > a[data-tab=\"" + tabId + "\"]:not([hidden])");

            if (a === null) {
                return;
            }

            a.dispatchEvent(new CustomEvent('click', {bubbles: true}));
        }
    }

    function readTabsQueryStringParam() {
        const qs = parseQueryString();
        const t = qs.tabs;

        if (t === undefined || t === '') {
            return [];
        }

        return t.split(',');
    }

    function updateTabsQueryStringParam(state) {
        const qs = parseQueryString();
        qs.tabs = state.selectedTabs.join();

        const url = location.protocol + "//" + location.host + location.pathname + "?" + toQueryString(qs) + location.hash;

        if (location.href === url) {
            return;
        }

        history.replaceState({}, document.title, url);
    }

    function toQueryString(args) {
        const parts = [];

        for (let name_1 in args) {
            if (args.hasOwnProperty(name_1) && args[name_1] !== '' && args[name_1] !== null && args[name_1] !== undefined) {
                parts.push(encodeURIComponent(name_1) + '=' + encodeURIComponent(args[name_1]));
            }
        }

        return parts.join('&');
    }

    function parseQueryString(queryString) {
        let match;
        const pl = /\+/g;
        const search = /([^&=]+)=?([^&]*)/g;

        const decode = function (s) {
            return decodeURIComponent(s.replace(pl, ' '));
        };

        if (queryString === undefined) {
            queryString = '';
        }

        queryString = queryString.substring(1);
        const urlParams = {};

        while (match = search.exec(queryString)) {
            urlParams[decode(match[1])] = decode(match[2]);
        }

        return urlParams;
    }

    function arraysIntersect(a, b) {
        for (let _i = 0, a_1 = a; _i < a_1.length; _i++) {
            const itemA = a_1[_i];

            for (let _a = 0, b_1 = b; _a < b_1.length; _a++) {
                const itemB = b_1[_a];
                if (itemA === itemB) {
                    return true;
                }
            }
        }

        return false;
    }

    function notifyContentUpdated() {
        // Dispatch this event when needed
        // window.dispatchEvent(new CustomEvent('content-update'));
    }
}

/**
 * Styling for tables in conceptual documents using Bootstrap.
 * See http://getbootstrap.com/css/#tables
 */
function renderTables() {
    $('table').addClass('table table-bordered table-striped table-condensed').wrap('<div class=\"table-responsive\"></div>');
}

window.refresh = function (article) {

    // Update markup result
    if (typeof article == 'undefined' || typeof article.content == 'undefined') {
        console.error("Null Argument");
    }

    $("article.content").html(article.content);

    highlight();
    renderTables();
    renderAlerts();
    renderAffix();
    renderTabs();
}

$(document).on('wordpressMenuHasLoaded', function () {
    const path = window.location.pathname;
    const docsUrl = '/docs/articles/';
    const wpNavBar = document.getElementById('menu-menu-principal');
    const items = wpNavBar.getElementsByTagName('a');

    for (let i = 0; i < items.length; i++) {

        if (items[i].href.includes(docsUrl) && path.includes(docsUrl) && !items[i].href.includes('#')) {
            $(items[i]).addClass('activepath');
        }
    }

    const queryString = window.location.search;

    if (queryString) {
        const queryStringComponents = queryString.split('=');
        const searchParam = queryStringComponents.slice(-1)[0];
        $('#search-query').val(decodeURI(searchParam));
    }

});


// Enable anchors for headings.
(function () {
    anchors.options = {
        placement: 'right',
        visible: 'hover',
        icon: '#'
    };
    anchors.add('article h2:not(.no-anchor), article h3:not(.no-anchor), article h4:not(.no-anchor)');
})();

// Enable highlight.js
function highlight() {

    $('pre code').each(function (i, block) {
        hljs.highlightBlock(block);
    });

    $('pre code[highlight-lines]').each(function (i, block) {
        if (block.innerHTML === "") return;
        const lines = block.innerHTML.split('\n');

        const queryString = block.getAttribute('highlight-lines');
        if (!queryString) return;

        let rangesString = queryString.split(',');
        let ranges = rangesString.map(Number);

        for (let range of ranges) {
            const found = range.match(/^(\d+)\-(\d+)?$/);
            let start = 0;
            let end = 0;
            if (found) {
                // consider region as `{startlinenumber}-{endlinenumber}`, in which {endlinenumber} is optional
                start = +found[1];
                end = +found[2];
                if (isNaN(end) || end > lines.length) {
                    end = lines.length;
                }
            } else {
                // consider region as a sigine line number
                if (isNaN(range)) continue;
                start = +range;
                end = start;
            }
            if (start <= 0 || end <= 0 || start > end || start > lines.length) {
                // skip current region if invalid
                continue;
            }
            lines[start - 1] = '<span class="line-highlight">' + lines[start - 1];
            lines[end - 1] = lines[end - 1] + '</span>';
        }

        block.innerHTML = lines.join('\n');
    });
}

// Support full-text-search
function enableSearch() {
    let query;
    const relHref = $("meta[property='docfx\\:rel']").attr("content");

    if (typeof relHref === 'undefined') {
        return;
    }

    try {
        const worker = new Worker(relHref + 'styles/search-worker.js');
        if (!worker && !window.worker) {
            localSearch();
        } else {
            webWorkerSearch(worker);
        }
        renderSearchBox();
        highlightKeywords();
        addSearchEvent();
    } catch (e) {
        console.error(e);
    }

    //Adjust the position of search box in navbar
    function renderSearchBox() {
        autoCollapse();

        $(window).on('resize', () => autoCollapse());

        $(document).on('click', '.navbar-collapse.in', function (e) {
            if ($(e.target).is('a')) {
                $(this).collapse(hide);
            }
        });

        function autoCollapse() {
            const navbar = $('#autocollapse');
            if (navbar.height() === null) {
                setTimeout(autoCollapse, 300);
            }
            navbar.removeClass(collapsed);
            if (navbar.height() > 60) {
                navbar.addClass(collapsed);
            }
        }
    }

    // Search factory
    function localSearch() {
        const lunrIndex = lunr(function () {
            this.ref('href');
            this.field('title', {boost: 50});
            this.field('keywords', {boost: 20});
        });
        lunr.tokenizer.seperator = /[\s\-\.]+/;
        let searchData = {};
        const searchDataRequest = new XMLHttpRequest();

        const indexPath = relHref + "index.json";
        if (indexPath) {
            searchDataRequest.open('GET', indexPath);
            searchDataRequest.onload = function () {
                if (this.status !== 200) {
                    return;
                }
                searchData = JSON.parse(this.responseText);
                for (let prop in searchData) {
                    if (searchData.hasOwnProperty(prop)) {
                        lunrIndex.add(searchData[prop]);
                    }
                }
            }
            searchDataRequest.send();
        }

        $("body").on("queryReady", function () {
            const hits = lunrIndex.search(query);
            const results = [];
            hits.forEach(function (hit) {
                const item = searchData[hit.ref];
                results.push({'href': item.href, 'title': item.title, 'keywords': item.keywords});
            });
            handleSearchResults(results);
        });
    }

    function webWorkerSearch(worker) {
        const indexReady = $.Deferred();
        worker.onmessage = function (oEvent) {
            switch (oEvent.data.e) {
                case 'index-ready':
                    indexReady.resolve();
                    break;
                case 'query-ready':
                    const hits = oEvent.data.d;
                    handleSearchResults(hits);
                    break;
            }
        }

        indexReady.promise().done(function () {

            $("body").on("query-ready", function () {
                postSearchQuery(worker, query);
            });

            postSearchQuery(worker, query);

        });
    }

    /**
     * This function posts the message to the worker if the string has at least
     * three characters.
     *
     * @param worker The search worker used by DocFx (lunr)
     * @param searchQuery The string to post to the worker.
     */
    function postSearchQuery(worker, searchQuery) {
        if (searchQuery && (searchQuery.length >= 3)) {
            worker.postMessage({q: `${searchQuery}*`});
        } else {
            worker.postMessage({q: ''});
        }
    }

    /**
     *   Highlight the searching keywords
     */
    function highlightKeywords() {
        const q = url('?q');
        if (q != null) {
            const keywords = q.split("%20");
            keywords.forEach(function (keyword) {
                if (keyword !== "") {
                    $('.data-searchable *').mark(keyword);
                    $('article *').mark(keyword);
                }
            });
        }
    }

    function addSearchEvent() {
        $('body').on("searchEvent", function () {
            $('#search-results>.sr-items').html('<p>No results found</p>');

            const searchQuery = $('#search-query');

            searchQuery.on('input', function (e) {
                return e.key !== 'Enter';
            });

            searchQuery.on("keyup", function (e) {
                $('#search-results').show();
                query = `${e.target.value}`;
                $("body").trigger("query-ready");
                $('#search-results>.search-list').text('Search Results for "' + query + '"');
            }).off("keydown");
        });
    }

    function relativeUrlToAbsoluteUrl(currentUrl, relativeUrl) {
        const currentItems = currentUrl.split(/\/+/);
        const relativeItems = relativeUrl.split(/\/+/);
        let depth = currentItems.length - 1;
        const items = [];
        for (let i = 0; i < relativeItems.length; i++) {
            if (relativeItems[i] === '..') {
                depth--;
            } else if (relativeItems[i] !== '.') {
                items.push(relativeItems[i]);
            }
        }
        return currentItems.slice(0, depth).concat(items).join('/');
    }

    function extractContentBrief(content) {
        const briefOffset = 50;
        const words = query.split(/\s+/g);
        const queryIndex = content.indexOf(words[0]);

        if (queryIndex > briefOffset) {
            return "..." + content.slice(queryIndex - briefOffset, queryIndex + briefOffset) + "...";
        } else if (queryIndex <= briefOffset) {
            return content.slice(0, queryIndex + briefOffset) + "...";
        }
    }

    function handleSearchResults(hits) {
        if (hits.length === 0) {
            $('#search-results>.sr-items').html('<p>No results found</p>');
        } else {
            $('#search-results>.sr-items').empty().append(
                hits.slice(0, 20).map(function (hit) {
                    const currentUrl = window.location.href;

                    const itemRawHref = relativeUrlToAbsoluteUrl(currentUrl, relHref + hit.href);
                    const itemHref = relHref + hit.href + "?q=" + query;
                    const itemTitle = hit.title;
                    const itemBrief = extractContentBrief(hit.keywords);

                    const itemNode = $('<a>').attr('class', 'sr-item').attr('href', itemHref);
                    const itemTitleNode = $('<div>').attr('class', 'item-title').text(itemTitle);
                    const itemBriefNode = $('<div>').attr('class', 'item-brief').text(itemBrief);
                    itemNode.append(itemTitleNode).append(itemBriefNode);

                    return itemNode;
                })
            );
            query.split(/\s+/).forEach(function (word) {
                if (word !== '') {
                    word = word.replace(/\*/g, '');
                    $('#search-results>.sr-items *').mark(word);
                }
            });
        }
    }
}

function getAbsolutePath(href) {
    // Use anchor to normalize href
    const anchor = $('<a href="' + href + '"></a>')[0];
    // Ignore protocal, remove search and query
    return anchor.host + anchor.pathname;
}

function isRelativePath(href) {
    if (href === undefined || href === '' || href[0] === '/') {
        return false;
    }
    return !isAbsolutePath(href);
}

function isAbsolutePath(href) {
    return (/^(?:[a-z]+:)?\/\//i).test(href);
}

function getDirectory(href) {
    if (!href) {
        return '';
    }

    const index = href.lastIndexOf('/');

    if (index === -1) {
        return '';
    }

    if (index > -1) {
        return href.substr(0, index);
    }
}

function formList(item, classes) {
    let level = 1;
    const model = {
        items: item
    };

    const cls = [].concat(classes).join(" ");
    return getList(model, cls);

    function getList(model, cls) {

        if (!model || !model.items) {
            return null;
        }

        const l = model.items.length;

        if (l === 0) {
            return null;
        }

        let html = '<ul class="level' + level + ' ' + (cls || '') + '">';
        level++;

        for (let i = 0; i < l; i++) {
            const item = model.items[i];
            const href = item.href;
            const name = item.name;

            if (!name) {
                continue;
            }

            html += href ? '<li><a href="' + href + '">' + name + '</a>' : '<li>' + name;
            html += getList(item, cls) || '';
            html += '</li>';
        }

        html += '</ul>';
        return html;
    }
}


/**
 * Add <wbr> into long word.
 * @param {String} text - The word to break. It should be in plain text without HTML tags.
 */
function breakPlainText(text) {
    if (!text) return text;
    return text.replace(/([a-z])([A-Z])|(\.)(\w)/g, '$1$3<wbr>$2$4')
}

/**
 * Add <wbr> into long word. The jQuery element should contain no html tags.
 * If the jQuery element contains tags, this function will not change the element.
 */
function breakWord() {
    if (this.html() === this.text()) {
        this.html(function (index, text) {
            return breakPlainText(text);
        })
    }

    return this;
}

/**
 * adjusted from https://stackoverflow.com/a/13067009/1523776
 */
function workAroundFixedHeaderForAnchors() {
    const HISTORY_SUPPORT = !!(history && history.pushState);
    const ANCHOR_REGEX = /^#[^ ]+$/;

    function getFixedOffset() {
        return $('header').first().height();
    }

    /**
     * If the provided href is an anchor which resolves to an element on the
     * page, scroll to it.
     * @param  {String} href destination
     * @param  {Boolean} pushToHistory push to history
     * @return {Boolean} - Was the href an anchor.
     */
    function scrollIfAnchor(href, pushToHistory) {
        let match, rect, anchorOffset;

        if (!ANCHOR_REGEX.test(href)) {
            return false;
        }

        match = document.getElementById(href.slice(1));

        if (match) {
            rect = match.getBoundingClientRect();
            anchorOffset = window.pageYOffset + rect.top - getFixedOffset();
            window.scrollTo(window.pageXOffset, anchorOffset);

            // Add the state to history as-per normal anchor links
            if (HISTORY_SUPPORT && pushToHistory) {
                history.pushState({}, document.title, location.pathname + href);
            }
        }

        return !!match;
    }

    /**
     * Attempt to scroll to the current location's hash.
     */
    function scrollToCurrent() {
        scrollIfAnchor(window.location.hash, false);
    }

    $(window).on('hashchange', () => scrollToCurrent());
    // Exclude tabbed content case
    scrollToCurrent();

    $(document).on('ready', function () {
        $('body').scrollspy({offset: 150});
    });
}

function breakText() {
    $(".xref").addClass("text-break");
    const texts = $(".text-break");
    texts.each(function () {
        $(this).breakWord();
    });
}

//# sourceMappingURL=data:application/json;charset=utf8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbImNvbnN0YW50LmpzIiwicmVuZGVyLmpzIiwiY29tcG9uZW50L2FmZml4LmpzIiwiY29tcG9uZW50L2FsZXJ0cy5qcyIsImNvbXBvbmVudC9icmVhZGNydW1iLmpzIiwiY29tcG9uZW50L2Zvb3Rlci5qcyIsImNvbXBvbmVudC9saW5rcy5qcyIsImNvbXBvbmVudC9uYXZiYXIuanMiLCJjb21wb25lbnQvc2lkZWJhci5qcyIsImNvbXBvbmVudC90YWIuanMiLCJjb21wb25lbnQvdGFibGVzLmpzIiwic2VydmljZS9nbG9iYWxldmVudHMuanMiLCJzZXJ2aWNlL3NlYXJjaC5qcyIsInNlcnZpY2UvdXRpbGl0eS5qcyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiQUFBQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQ05BO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUN6QkE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDMUhBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUN6QkE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQ25CQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUN6Q0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDUkE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQ3ZNQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDcE1BO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDalVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUNQQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDakRBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FDeFFBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBIiwiZmlsZSI6ImRvY2Z4LmpzIiwic291cmNlc0NvbnRlbnQiOlsiY29uc3QgYWN0aXZlID0gJ2FjdGl2ZSc7XHJcbmNvbnN0IGV4cGFuZGVkID0gJ2luJztcclxuY29uc3QgZmlsdGVyZWQgPSAnZmlsdGVyZWQnO1xyXG5jb25zdCBzaG93ID0gJ3Nob3cnO1xyXG5jb25zdCBoaWRlID0gJ2hpZGUnO1xyXG5jb25zdCBjb2xsYXBzZWQgPSAnY29sbGFwc2VkJztcclxuIiwiLy8gd29ya2Fyb3VuZCBmb3IgZ3VscC11Z2xpZnkgY2hhbmdpbmcgb3JkZXIgb2YgZXhlY3V0aW9uIG9uICQuZm4gZnVuYyBhc3NpZ25tZW50c1xyXG5PYmplY3QuYXNzaWduKCQuZm4sIHsgYnJlYWtXb3JkIH0pO1xyXG5cclxud29ya0Fyb3VuZEZpeGVkSGVhZGVyRm9yQW5jaG9ycygpO1xyXG5oaWdobGlnaHQoKTtcclxuZW5hYmxlU2VhcmNoKCk7XHJcblxyXG5yZW5kZXJUYWJsZXMoKTtcclxucmVuZGVyQWxlcnRzKCk7XHJcbnVwZGF0ZUFsZXJ0SGVpZ2h0T25SZXNpemUoKTtcclxucmVuZGVyTGlua3MoKTtcclxucmVuZGVyU2lkZWJhcigpO1xyXG5yZW5kZXJBZmZpeCgpO1xyXG5cclxucmVuZGVyTmF2YmFyKCk7XHJcbnJlbmRlckxvZ28oKTtcclxudXBkYXRlTG9nbygpXHJcbnVwZGF0ZUxvZ29PblJlc2l6ZSgpO1xyXG51cGRhdGVOYXZiYXJIZWlnaHRPblJlc2l6ZSgpO1xyXG51cGRhdGVUb2NIZWlnaHRPblJlc2l6ZSgpO1xyXG51cGRhdGVTaWRlbmF2VG9wT25SZXNpemUoKTtcclxucmVuZGVyRm9vdGVyKCk7XHJcbmJyZWFrVGV4dCgpO1xyXG5yZW5kZXJUYWJzKCk7XHJcbnVwZGF0ZUxvZ28oKTtcclxuIiwiLy9TZXR1cCBBZmZpeFxyXG5mdW5jdGlvbiByZW5kZXJBZmZpeCgpIHtcclxuICAgIGNvbnN0IGhpZXJhcmNoeSA9IGdldEhpZXJhcmNoeSgpO1xyXG5cclxuICAgIGlmIChoaWVyYXJjaHkgJiYgaGllcmFyY2h5Lmxlbmd0aCA+IDApIHtcclxuICAgICAgICBsZXQgaHRtbCA9ICc8aDUgY2xhc3M9XCJ0aXRsZVwiPkluIFRoaXMgQXJ0aWNsZTwvaDU+J1xyXG4gICAgICAgIGh0bWwgKz0gZm9ybUxpc3QoaGllcmFyY2h5LCBbJ25hdicsICdicy1kb2NzLXNpZGVuYXYnXSk7XHJcblxyXG4gICAgICAgICQoXCIjYWZmaXhcIikuZW1wdHkoKS5hcHBlbmQoaHRtbCk7XHJcblxyXG4gICAgICAgIGlmICgkKCdmb290ZXInKS5pcygnOnZpc2libGUnKSkge1xyXG4gICAgICAgICAgICAkKFwiLnNpZGVhZmZpeFwiKS5jc3MoXCJib3R0b21cIiwgXCI3MHB4XCIpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgJCgnI2FmZml4IGEnKS5vbignY2xpY2snLCBmdW5jdGlvbiAoZSkge1xyXG4gICAgICAgICAgICBjb25zdCBzY3JvbGxzcHkgPSAkKCdbZGF0YS1zcHk9XCJzY3JvbGxcIl0nKS5kYXRhKClbJ2JzLnNjcm9sbHNweSddO1xyXG4gICAgICAgICAgICBjb25zdCB0YXJnZXQgPSBlLnRhcmdldC5oYXNoO1xyXG4gICAgICAgICAgICBpZiAoc2Nyb2xsc3B5ICYmIHRhcmdldCkge1xyXG4gICAgICAgICAgICAgICAgc2Nyb2xsc3B5LmFjdGl2YXRlKHRhcmdldCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgY29uc3QgY29udHJpYnV0aW9uID0gJCgnLmNvbnRyaWJ1dGlvbicpO1xyXG4gICAgICAgIGNvbnN0IGNvbnRyaWJ1dGlvbkRpdiA9IGNvbnRyaWJ1dGlvbi5nZXQoMCkub3V0ZXJIVE1MO1xyXG4gICAgICAgIGNvbnRyaWJ1dGlvbi5yZW1vdmUoKTtcclxuICAgICAgICAkKCcuc2lkZWFmZml4JykuYXBwZW5kKGNvbnRyaWJ1dGlvbkRpdik7XHJcblxyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIGdldEhpZXJhcmNoeSgpIHtcclxuICAgICAgICAvLyBzdXBwb3J0ZWQgaGVhZGVycyBhcmUgaDEsIGgyLCBoMywgYW5kIGg0XHJcbiAgICAgICAgY29uc3QgJGhlYWRlcnMgPSAkKCQubWFwKFsnaDEnLCAnaDInLCAnaDMnLCAnaDQnXSwgZnVuY3Rpb24gKGgpIHsgcmV0dXJuIFwiLmFydGljbGUgYXJ0aWNsZSBcIiArIGg7IH0pLmpvaW4oXCIsIFwiKSk7XHJcblxyXG4gICAgICAgIC8vIGEgc3RhY2sgb2YgaGllcmFyY2h5IGl0ZW1zIHRoYXQgYXJlIGN1cnJlbnRseSBiZWluZyBidWlsdFxyXG4gICAgICAgIGNvbnN0IHN0YWNrID0gW107XHJcbiAgICAgICAgJGhlYWRlcnMuZWFjaChmdW5jdGlvbiAoaSwgZSkge1xyXG4gICAgICAgICAgICBpZiAoIWUuaWQpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgY29uc3QgaXRlbSA9IHtcclxuICAgICAgICAgICAgICAgIG5hbWU6IGh0bWxFbmNvZGUoJChlKS50ZXh0KCkpLFxyXG4gICAgICAgICAgICAgICAgaHJlZjogXCIjXCIgKyBlLmlkLFxyXG4gICAgICAgICAgICAgICAgaXRlbXM6IFtdXHJcbiAgICAgICAgICAgIH07XHJcblxyXG4gICAgICAgICAgICBpZiAoIXN0YWNrLmxlbmd0aCkge1xyXG4gICAgICAgICAgICAgICAgc3RhY2sucHVzaCh7IHR5cGU6IGUudGFnTmFtZSwgc2libGluZ3M6IFtpdGVtXSB9KTtcclxuICAgICAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgY29uc3QgZnJhbWUgPSBzdGFja1tzdGFjay5sZW5ndGggLSAxXTtcclxuICAgICAgICAgICAgaWYgKGUudGFnTmFtZSA9PT0gZnJhbWUudHlwZSkge1xyXG4gICAgICAgICAgICAgICAgZnJhbWUuc2libGluZ3MucHVzaChpdGVtKTtcclxuICAgICAgICAgICAgfSBlbHNlIGlmIChlLnRhZ05hbWVbMV0gPiBmcmFtZS50eXBlWzFdKSB7XHJcbiAgICAgICAgICAgICAgICAvLyB3ZSBhcmUgbG9va2luZyBhdCBhIGNoaWxkIG9mIHRoZSBsYXN0IGVsZW1lbnQgb2YgZnJhbWUuc2libGluZ3MuXHJcbiAgICAgICAgICAgICAgICAvLyBwdXNoIGEgZnJhbWUgb250byB0aGUgc3RhY2suIEFmdGVyIHdlJ3ZlIGZpbmlzaGVkIGJ1aWxkaW5nIHRoaXMgaXRlbSdzIGNoaWxkcmVuLFxyXG4gICAgICAgICAgICAgICAgLy8gd2UnbGwgYXR0YWNoIGl0IGFzIGEgY2hpbGQgb2YgdGhlIGxhc3QgZWxlbWVudFxyXG4gICAgICAgICAgICAgICAgc3RhY2sucHVzaCh7IHR5cGU6IGUudGFnTmFtZSwgc2libGluZ3M6IFtpdGVtXSB9KTtcclxuICAgICAgICAgICAgfSBlbHNlIHsgIC8vIGUudGFnTmFtZVsxXSA8IGZyYW1lLnR5cGVbMV1cclxuICAgICAgICAgICAgICAgIC8vIHdlIGFyZSBsb29raW5nIGF0IGEgc2libGluZyBvZiBhbiBhbmNlc3RvciBvZiB0aGUgY3VycmVudCBpdGVtLlxyXG4gICAgICAgICAgICAgICAgLy8gcG9wIGZyYW1lcyBmcm9tIHRoZSBzdGFjaywgYnVpbGRpbmcgaXRlbXMgYXMgd2UgZ28sIHVudGlsIHdlIHJlYWNoIHRoZSBjb3JyZWN0IGxldmVsIGF0IHdoaWNoIHRvIGF0dGFjaCB0aGlzIGl0ZW0uXHJcbiAgICAgICAgICAgICAgICB3aGlsZSAoZS50YWdOYW1lWzFdIDwgc3RhY2tbc3RhY2subGVuZ3RoIC0gMV0udHlwZVsxXSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGJ1aWxkUGFyZW50KCk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICBpZiAoZS50YWdOYW1lID09PSBzdGFja1tzdGFjay5sZW5ndGggLSAxXS50eXBlKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgc3RhY2tbc3RhY2subGVuZ3RoIC0gMV0uc2libGluZ3MucHVzaChpdGVtKTtcclxuICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgc3RhY2sucHVzaCh7IHR5cGU6IGUudGFnTmFtZSwgc2libGluZ3M6IFtpdGVtXSB9KTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG4gICAgICAgIHdoaWxlIChzdGFjay5sZW5ndGggPiAxKSB7XHJcbiAgICAgICAgICAgIGJ1aWxkUGFyZW50KCk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBmdW5jdGlvbiBidWlsZFBhcmVudCgpIHtcclxuICAgICAgICAgICAgY29uc3QgY2hpbGRyZW5Ub0F0dGFjaCA9IHN0YWNrLnBvcCgpO1xyXG4gICAgICAgICAgICBjb25zdCBwYXJlbnRGcmFtZSA9IHN0YWNrW3N0YWNrLmxlbmd0aCAtIDFdO1xyXG4gICAgICAgICAgICBjb25zdCBwYXJlbnQgPSBwYXJlbnRGcmFtZS5zaWJsaW5nc1twYXJlbnRGcmFtZS5zaWJsaW5ncy5sZW5ndGggLSAxXTtcclxuICAgICAgICAgICAgJC5lYWNoKGNoaWxkcmVuVG9BdHRhY2guc2libGluZ3MsIGZ1bmN0aW9uIChpLCBjaGlsZCkge1xyXG4gICAgICAgICAgICAgICAgcGFyZW50Lml0ZW1zLnB1c2goY2hpbGQpO1xyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcbiAgICAgICAgaWYgKHN0YWNrLmxlbmd0aCA+IDApIHtcclxuXHJcbiAgICAgICAgICAgIGNvbnN0IHRvcExldmVsID0gc3RhY2sucG9wKCkuc2libGluZ3M7XHJcbiAgICAgICAgICAgIGlmICh0b3BMZXZlbC5sZW5ndGggPT09IDEpIHsgIC8vIGlmIHRoZXJlJ3Mgb25seSBvbmUgdG9wbW9zdCBoZWFkZXIsIGR1bXAgaXRcclxuICAgICAgICAgICAgICAgIHJldHVybiB0b3BMZXZlbFswXS5pdGVtcztcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICByZXR1cm4gdG9wTGV2ZWw7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJldHVybiB1bmRlZmluZWQ7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gaHRtbEVuY29kZShzdHIpIHtcclxuICAgICAgICBpZiAoIXN0cikgcmV0dXJuIHN0cjtcclxuICAgICAgICByZXR1cm4gc3RyXHJcbiAgICAgICAgICAgIC5yZXBsYWNlKC8mL2csICcmYW1wOycpXHJcbiAgICAgICAgICAgIC5yZXBsYWNlKC9cIi9nLCAnJnF1b3Q7JylcclxuICAgICAgICAgICAgLnJlcGxhY2UoLycvZywgJyYjMzk7JylcclxuICAgICAgICAgICAgLnJlcGxhY2UoLzwvZywgJyZsdDsnKVxyXG4gICAgICAgICAgICAucmVwbGFjZSgvPi9nLCAnJmd0OycpO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIGh0bWxEZWNvZGUoc3RyKSB7XHJcbiAgICAgICAgaWYgKCFzdHIpIHJldHVybiBzdHI7XHJcbiAgICAgICAgcmV0dXJuIHN0clxyXG4gICAgICAgICAgICAucmVwbGFjZSgvJnF1b3Q7L2csICdcIicpXHJcbiAgICAgICAgICAgIC5yZXBsYWNlKC8mIzM5Oy9nLCBcIidcIilcclxuICAgICAgICAgICAgLnJlcGxhY2UoLyZsdDsvZywgJzwnKVxyXG4gICAgICAgICAgICAucmVwbGFjZSgvJmd0Oy9nLCAnPicpXHJcbiAgICAgICAgICAgIC5yZXBsYWNlKC8mYW1wOy9nLCAnJicpO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIGNzc0VzY2FwZShzdHIpIHtcclxuICAgICAgICAvLyBzZWU6IGh0dHA6Ly9zdGFja292ZXJmbG93LmNvbS9xdWVzdGlvbnMvMjc4NjUzOC9uZWVkLXRvLWVzY2FwZS1hLXNwZWNpYWwtY2hhcmFjdGVyLWluLWEtanF1ZXJ5LXNlbGVjdG9yLXN0cmluZyNhbnN3ZXItMjgzNzY0NlxyXG4gICAgICAgIGlmICghc3RyKSByZXR1cm4gc3RyO1xyXG4gICAgICAgIHJldHVybiBzdHJcclxuICAgICAgICAgICAgLnJlcGxhY2UoL1shXCIjJCUmJygpKissLlxcLzo7PD0+P0BbXFxcXFxcXV5ge3x9fl0vZywgXCJcXFxcJCZcIik7XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gU3R5bGluZyBmb3IgYWxlcnRzLlxyXG5mdW5jdGlvbiBzZXRBbGVydEhlaWdodCgpe1xyXG4gICAgdmFyIG1heEhlaWdodCA9IE1hdGgubWF4LmFwcGx5KG51bGwsICQoXCIuY29sLW1kLTYgZGl2LmFsZXJ0XCIpLm1hcChmdW5jdGlvbiAoKVxyXG4gICAge1xyXG4gICAgICAgIHJldHVybiAkKHRoaXMpLm91dGVySGVpZ2h0KCk7XHJcbiAgICB9KS5nZXQoKSk7XHJcblxyXG4gICAgJCgnLmFsZXJ0JykuY3NzKCdoZWlnaHQnLCBtYXhIZWlnaHQpO1xyXG4gICAgXHJcbn1cclxuXHJcbmZ1bmN0aW9uIHVwZGF0ZUFsZXJ0SGVpZ2h0T25SZXNpemUoKSB7XHJcbiAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZScsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAkKCcuYWxlcnQnKS5jc3MoJ2hlaWdodCcsICdhdXRvJyk7XHJcbiAgICAgICAgc2V0QWxlcnRIZWlnaHQoKTtcclxuICAgIH0pO1xyXG59XHJcblxyXG5mdW5jdGlvbiByZW5kZXJBbGVydHMoKSB7XHJcbiAgICAkKCcuTk9URSwgLlRJUCcpLmFkZENsYXNzKCdhbGVydCBhbGVydC1pbmZvJyk7XHJcbiAgICAkKCcuV0FSTklORycpLmFkZENsYXNzKCdhbGVydCBhbGVydC13YXJuaW5nJyk7XHJcbiAgICAkKCcuSU1QT1JUQU5ULCAuQ0FVVElPTicpLmFkZENsYXNzKCdhbGVydCBhbGVydC1kYW5nZXInKTtcclxuICAgIHNldEFsZXJ0SGVpZ2h0KCk7XHJcblxyXG59XHJcbiIsImZ1bmN0aW9uIHJlbmRlckJyZWFkY3J1bWIoKSB7XHJcbiAgICBjb25zdCBicmVhZGNydW1iID0gW107XHJcblxyXG4gICAgJCgnI25hdmJhciBhLmFjdGl2ZScpLmVhY2goZnVuY3Rpb24gKGksIGUpIHtcclxuICAgICAgICBicmVhZGNydW1iLnB1c2goe1xyXG4gICAgICAgICAgICBocmVmOiBlLmhyZWYsXHJcbiAgICAgICAgICAgIG5hbWU6IGUuaW5uZXJIVE1MXHJcbiAgICAgICAgfSk7XHJcbiAgICB9KVxyXG4gICAgJCgnI3RvYyBhLmFjdGl2ZScpLmVhY2goZnVuY3Rpb24gKGksIGUpIHtcclxuICAgICAgICBicmVhZGNydW1iLnB1c2goe1xyXG4gICAgICAgICAgICBocmVmOiBlLmhyZWYsXHJcbiAgICAgICAgICAgIG5hbWU6IGUuaW5uZXJIVE1MXHJcbiAgICAgICAgfSk7XHJcbiAgICB9KVxyXG5cclxuICAgIGNvbnN0IGh0bWwgPSBmb3JtTGlzdChicmVhZGNydW1iLCAnYnJlYWRjcnVtYicpO1xyXG4gICAgJCgnI2JyZWFkY3J1bWInKS5odG1sKGh0bWwpO1xyXG59XHJcbiIsIi8vIFNob3cgZm9vdGVyXHJcbmZ1bmN0aW9uIHJlbmRlckZvb3RlcigpIHtcclxuICAgIGluaXRGb290ZXIoKTtcclxuICAgICQod2luZG93KS5vbihcInNjcm9sbFwiLCAoKSA9PiBzaG93Rm9vdGVyQ29yZSgpKTtcclxuXHJcbiAgICBmdW5jdGlvbiBpbml0Rm9vdGVyKCkge1xyXG4gICAgICAgIGlmIChuZWVkRm9vdGVyKCkpIHtcclxuICAgICAgICAgICAgc2hpZnRVcEJvdHRvbUNzcygpO1xyXG4gICAgICAgICAgICAkKFwiZm9vdGVyXCIpLnNob3coKTtcclxuICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICByZXNldEJvdHRvbUNzcygpO1xyXG4gICAgICAgICAgICAkKFwiZm9vdGVyXCIpLmhpZGUoKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gc2hvd0Zvb3RlckNvcmUoKSB7XHJcbiAgICAgICAgaWYgKG5lZWRGb290ZXIoKSkge1xyXG4gICAgICAgICAgICBzaGlmdFVwQm90dG9tQ3NzKCk7XHJcbiAgICAgICAgICAgICQoXCJmb290ZXJcIikuZmFkZUluKCk7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgcmVzZXRCb3R0b21Dc3MoKTtcclxuICAgICAgICAgICAgJChcImZvb3RlclwiKS5mYWRlT3V0KCk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIG5lZWRGb290ZXIoKSB7XHJcbiAgICAgICAgY29uc3Qgc2Nyb2xsSGVpZ2h0ID0gJChkb2N1bWVudCkuaGVpZ2h0KCk7XHJcbiAgICAgICAgY29uc3Qgc2Nyb2xsUG9zaXRpb24gPSAkKHdpbmRvdykuaGVpZ2h0KCkgKyAkKHdpbmRvdykuc2Nyb2xsVG9wKCk7XHJcbiAgICAgICAgcmV0dXJuIChzY3JvbGxIZWlnaHQgLSBzY3JvbGxQb3NpdGlvbikgPCAxO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIHJlc2V0Qm90dG9tQ3NzKCkge1xyXG4gICAgICAgICQoXCIuc2lkZXRvY1wiKS5yZW1vdmVDbGFzcyhcInNoaWZ0dXBcIik7XHJcbiAgICAgICAgJChcIi5zaWRlYWZmaXhcIikucmVtb3ZlQ2xhc3MoXCJzaGlmdHVwXCIpO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIHNoaWZ0VXBCb3R0b21Dc3MoKSB7XHJcbiAgICAgICAgJChcIi5zaWRldG9jXCIpLmFkZENsYXNzKFwic2hpZnR1cFwiKTtcclxuICAgICAgICAkKFwiLnNpZGVhZmZpeFwiKS5hZGRDbGFzcyhcInNoaWZ0dXBcIik7XHJcbiAgICB9XHJcbn1cclxuIiwiLy8gT3BlbiBsaW5rcyB0byBkaWZmZXJlbnQgaG9zdCBpbiBhIG5ldyB3aW5kb3cuXHJcbmZ1bmN0aW9uIHJlbmRlckxpbmtzKCkge1xyXG4gICAgaWYgKCQoXCJtZXRhW3Byb3BlcnR5PSdkb2NmeDpuZXd0YWInXVwiKS5hdHRyKFwiY29udGVudFwiKSA9PT0gXCJ0cnVlXCIpIHtcclxuICAgICAgICAkKGRvY3VtZW50LmxpbmtzKS5maWx0ZXIoZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICAgICByZXR1cm4gdGhpcy5ob3N0bmFtZSAhPT0gd2luZG93LmxvY2F0aW9uLmhvc3RuYW1lO1xyXG4gICAgICAgIH0pLmF0dHIoJ3RhcmdldCcsICdfYmxhbmsnKTtcclxuICAgIH1cclxufVxyXG4iLCJmdW5jdGlvbiBzZXROYXZiYXJIZWlnaHQoKSB7XHJcbiAgICBsZXQgaGVhZGVySGVpZ2h0ID0gJChcIiNoZWFkZXItY29udGFpbmVyXCIpLm91dGVySGVpZ2h0KCk7XHJcbiAgICBsZXQgaW50Vmlld3BvcnRIZWlnaHQgPSB3aW5kb3cuaW5uZXJIZWlnaHQ7XHJcbiAgICBsZXQgbWF4SGVpZ2h0TmF2YmFyID0gaW50Vmlld3BvcnRIZWlnaHQgLSBoZWFkZXJIZWlnaHQ7XHJcbiAgICAkKFwiI25hdmJhclwiKS5jc3MoXCJtYXgtaGVpZ2h0XCIsIG1heEhlaWdodE5hdmJhcik7XHJcbn1cclxuXHJcblxyXG4vKipcclxuICogTG9hZCB0aGUgbmF2YmFyIGZyb20gdGhlIHVubyB3ZWJzaXRlXHJcbiAqL1xyXG5mdW5jdGlvbiBpbml0aWFsaXplTmF2YmFyKCkge1xyXG5cclxuICAgIGNvbnN0IG5hdmJhciA9IGRvY3VtZW50LnF1ZXJ5U2VsZWN0b3IoXCJoZWFkZXIgPiAubmF2YmFyXCIpO1xyXG4gICAgaWYgKGRvY3VtZW50LmJvZHkuY2xhc3NMaXN0LmNvbnRhaW5zKFwiZnJvbnQtcGFnZVwiKSkge1xyXG4gICAgICAgIGxldCBsYXN0X2tub3duX3Njcm9sbF9wb3NpdGlvbiA9IDA7XHJcbiAgICAgICAgbGV0IHRpY2tpbmcgPSBmYWxzZTtcclxuXHJcbiAgICAgICAgZnVuY3Rpb24gZG9Tb21ldGhpbmcoc2Nyb2xsX3Bvcykge1xyXG4gICAgICAgICAgICBpZiAoc2Nyb2xsX3BvcyA+PSAxMDApIG5hdmJhci5jbGFzc0xpc3QuYWRkKFwic2Nyb2xsZWRcIik7XHJcbiAgICAgICAgICAgIGVsc2UgbmF2YmFyLmNsYXNzTGlzdC5yZW1vdmUoXCJzY3JvbGxlZFwiKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHdpbmRvdy5hZGRFdmVudExpc3RlbmVyKFwic2Nyb2xsXCIsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgbGFzdF9rbm93bl9zY3JvbGxfcG9zaXRpb24gPSB3aW5kb3cuc2Nyb2xsWTtcclxuXHJcbiAgICAgICAgICAgIGlmICghdGlja2luZykge1xyXG4gICAgICAgICAgICAgICAgd2luZG93LnJlcXVlc3RBbmltYXRpb25GcmFtZShmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgZG9Tb21ldGhpbmcobGFzdF9rbm93bl9zY3JvbGxfcG9zaXRpb24pO1xyXG4gICAgICAgICAgICAgICAgICAgIHRpY2tpbmcgPSBmYWxzZTtcclxuICAgICAgICAgICAgICAgIH0pO1xyXG5cclxuICAgICAgICAgICAgICAgIHRpY2tpbmcgPSB0cnVlO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3QgdW5vTWVudVJlcSA9IG5ldyBYTUxIdHRwUmVxdWVzdCgpO1xyXG4gICAgY29uc3QgdW5vTWVudUVuZHBvaW50ID0gXCJodHRwczovL3BsYXRmb3JtLnVuby93cC1qc29uL3dwL3YyL21lbnVcIjtcclxuICAgIGNvbnN0ICRuYXZiYXIgPSBkb2N1bWVudC5nZXRFbGVtZW50QnlJZChcIm5hdmJhclwiKTtcclxuICAgIGxldCB3b3JkcHJlc3NNZW51SGFzTG9hZGVkID0gZmFsc2U7XHJcblxyXG4gICAgdW5vTWVudVJlcS5vcGVuKFwiZ2V0XCIsIHVub01lbnVFbmRwb2ludCwgdHJ1ZSk7XHJcblxyXG4gICAgaWYgKHR5cGVvZiBuYXZiYXIgIT09IFwidW5kZWZpbmVkXCIpIHtcclxuICAgICAgICB1bm9NZW51UmVxLm9ubG9hZCA9IGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgaWYgKHVub01lbnVSZXEuc3RhdHVzID09PSAyMDAgJiYgdW5vTWVudVJlcS5yZXNwb25zZVRleHQpIHtcclxuICAgICAgICAgICAgICAgICRuYXZiYXIuaW5uZXJIVE1MID0gSlNPTi5wYXJzZShcclxuICAgICAgICAgICAgICAgICAgICB1bm9NZW51UmVxLnJlc3BvbnNlVGV4dFxyXG4gICAgICAgICAgICAgICAgKTtcclxuICAgICAgICAgICAgICAgIHdvcmRwcmVzc01lbnVIYXNMb2FkZWQgPSB0cnVlO1xyXG4gICAgICAgICAgICAgICAgJChkb2N1bWVudCkudHJpZ2dlcihcIndvcmRwcmVzc01lbnVIYXNMb2FkZWRcIik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9O1xyXG4gICAgICAgIHVub01lbnVSZXEub25lcnJvciA9IGZ1bmN0aW9uIChlKSB7XHJcbiAgICAgICAgfTtcclxuICAgICAgICB1bm9NZW51UmVxLnNlbmQoKTtcclxuICAgIH1cclxuXHJcbiAgICAkKGRvY3VtZW50KS5hamF4Q29tcGxldGUoZnVuY3Rpb24gKGV2ZW50LCB4aHIsIHNldHRpbmdzKSB7XHJcbiAgICAgICAgY29uc3QgZG9jRnhOYXZiYXJIYXNMb2FkZWQgPSBzZXR0aW5ncy51cmwgPT09IFwidG9jLmh0bWxcIjtcclxuXHJcbiAgICAgICAgaWYgKGRvY0Z4TmF2YmFySGFzTG9hZGVkICYmIHdvcmRwcmVzc01lbnVIYXNMb2FkZWQpIHtcclxuICAgICAgICAgICAgY29uc3QgJGRvY2Z4TmF2YmFyID0gJG5hdmJhci5nZXRFbGVtZW50c0J5Q2xhc3NOYW1lKFwibmF2YmFyLW5hdlwiKTtcclxuICAgICAgICAgICAgJGRvY2Z4TmF2YmFyWzBdLmNsYXNzTmFtZSArPSBcIiBoaWRkZW5cIjtcclxuXHJcbiAgICAgICAgfVxyXG4gICAgfSk7XHJcblxyXG4gICAgc2V0TmF2YmFySGVpZ2h0KCk7XHJcblxyXG59XHJcblxyXG4vKipcclxuICogQ2hhbmdlcyB0aGUgbG9nbyBvbiByZXNpemVcclxuKi9cclxuXHJcbmZ1bmN0aW9uIHVwZGF0ZUxvZ28oKSB7XHJcbiAgICBjb25zdCBjdXJXaWR0aCA9IHdpbmRvdy5pbm5lcldpZHRoO1xyXG4gICAgY29uc3QgaGVhZGVyTG9nbyA9IGRvY3VtZW50LmdldEVsZW1lbnRCeUlkKCdsb2dvJyk7XHJcbiAgICBpZiAoY3VyV2lkdGggPCA5ODApIHtcclxuICAgICAgICBjb25zdCBtb2JpbGVMb2dvID0gbmV3IFVSTCgnVW5vTG9nb1NtYWxsLnBuZycsIGhlYWRlckxvZ28uc3JjKS5ocmVmO1xyXG4gICAgICAgIGhlYWRlckxvZ28uc3JjID0gbW9iaWxlTG9nbztcclxuICAgIH0gZWxzZSB7XHJcbiAgICAgICAgY29uc3QgZGVza0xvZ28gPSBuZXcgVVJMKCd1bm8tbG9nby5zdmcnLCBoZWFkZXJMb2dvLnNyYykuaHJlZjtcclxuICAgICAgICBoZWFkZXJMb2dvLnNyYyA9IGRlc2tMb2dvO1xyXG4gICAgfVxyXG59XHJcblxyXG5mdW5jdGlvbiB1cGRhdGVMb2dvT25SZXNpemUoKSB7XHJcbiAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZScsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICB1cGRhdGVMb2dvKCk7XHJcbiAgICB9KTtcclxufVxyXG5cclxuXHJcbmZ1bmN0aW9uIHVwZGF0ZU5hdmJhckhlaWdodE9uUmVzaXplKCkge1xyXG4gICAgJCh3aW5kb3cpLm9uKCdyZXNpemUnLCBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgc2V0TmF2YmFySGVpZ2h0KCk7XHJcbiAgICB9KTtcclxufVxyXG5cclxuXHJcbi8vIFVwZGF0ZSBocmVmIGluIG5hdmJhclxyXG5mdW5jdGlvbiByZW5kZXJOYXZiYXIoKSB7XHJcbiAgICBjb25zdCBuYXZiYXIgPSAkKCcjbmF2YmFyIHVsJylbMF07XHJcbiAgICBpZiAodHlwZW9mIChuYXZiYXIpID09PSAndW5kZWZpbmVkJykge1xyXG4gICAgICAgIGxvYWROYXZiYXIoKTtcclxuICAgIH0gZWxzZSB7XHJcbiAgICAgICAgJCgnI25hdmJhciB1bCBhLmFjdGl2ZScpLnBhcmVudHMoJ2xpJykuYWRkQ2xhc3MoYWN0aXZlKTtcclxuICAgICAgICByZW5kZXJCcmVhZGNydW1iKCk7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gbG9hZE5hdmJhcigpIHtcclxuICAgICAgICBsZXQgbmF2YmFyUGF0aCA9ICQoXCJtZXRhW3Byb3BlcnR5PSdkb2NmeFxcXFw6bmF2cmVsJ11cIikuYXR0cihcImNvbnRlbnRcIik7XHJcbiAgICAgICAgaWYgKCFuYXZiYXJQYXRoKSB7XHJcbiAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICB9XHJcbiAgICAgICAgbmF2YmFyUGF0aCA9IG5hdmJhclBhdGgucmVwbGFjZSgvXFxcXC9nLCAnLycpO1xyXG4gICAgICAgIGxldCB0b2NQYXRoID0gJChcIm1ldGFbcHJvcGVydHk9J2RvY2Z4XFxcXDp0b2NyZWwnXVwiKS5hdHRyKFwiY29udGVudFwiKSB8fCAnJztcclxuICAgICAgICBpZiAodG9jUGF0aCkgdG9jUGF0aCA9IHRvY1BhdGgucmVwbGFjZSgvXFxcXC9nLCAnLycpO1xyXG4gICAgICAgICQuZ2V0KG5hdmJhclBhdGgsIGZ1bmN0aW9uIChkYXRhKSB7XHJcbiAgICAgICAgICAgICQoZGF0YSkuZmluZChcIiN0b2M+dWxcIikuYXBwZW5kVG8oXCIjbmF2YmFyXCIpO1xyXG4gICAgICAgICAgICBjb25zdCBpbmRleCA9IG5hdmJhclBhdGgubGFzdEluZGV4T2YoJy8nKTtcclxuICAgICAgICAgICAgbGV0IG5hdnJlbCA9ICcnO1xyXG4gICAgICAgICAgICBpZiAoaW5kZXggPiAtMSkge1xyXG4gICAgICAgICAgICAgICAgbmF2cmVsID0gbmF2YmFyUGF0aC5zdWJzdHIoMCwgaW5kZXggKyAxKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAkKCcjbmF2YmFyPnVsJykuYWRkQ2xhc3MoJ25hdmJhci1uYXYnKTtcclxuXHJcbiAgICAgICAgICAgIGNvbnN0IGN1cnJlbnRBYnNQYXRoID0gZ2V0QWJzb2x1dGVQYXRoKHdpbmRvdy5sb2NhdGlvbi5wYXRobmFtZSk7XHJcblxyXG4gICAgICAgICAgICAvLyBzZXQgYWN0aXZlIGl0ZW1cclxuICAgICAgICAgICAgJCgnI25hdmJhcicpLmZpbmQoJ2FbaHJlZl0nKS5lYWNoKGZ1bmN0aW9uIChpLCBlKSB7XHJcbiAgICAgICAgICAgICAgICBsZXQgaHJlZiA9ICQoZSkuYXR0cihcImhyZWZcIik7XHJcbiAgICAgICAgICAgICAgICBpZiAoaXNSZWxhdGl2ZVBhdGgoaHJlZikpIHtcclxuICAgICAgICAgICAgICAgICAgICBocmVmID0gbmF2cmVsICsgaHJlZjtcclxuICAgICAgICAgICAgICAgICAgICAkKGUpLmF0dHIoXCJocmVmXCIsIGhyZWYpO1xyXG5cclxuICAgICAgICAgICAgICAgICAgICAvLyBUT0RPOiBjdXJyZW50bHkgb25seSBzdXBwb3J0IG9uZSBsZXZlbCBuYXZiYXJcclxuICAgICAgICAgICAgICAgICAgICBsZXQgaXNBY3RpdmUgPSBmYWxzZTtcclxuICAgICAgICAgICAgICAgICAgICBsZXQgb3JpZ2luYWxIcmVmID0gZS5uYW1lO1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChvcmlnaW5hbEhyZWYpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgb3JpZ2luYWxIcmVmID0gbmF2cmVsICsgb3JpZ2luYWxIcmVmO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAoZ2V0RGlyZWN0b3J5KGdldEFic29sdXRlUGF0aChvcmlnaW5hbEhyZWYpKSA9PT0gZ2V0RGlyZWN0b3J5KGdldEFic29sdXRlUGF0aCh0b2NQYXRoKSkpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGlzQWN0aXZlID0gdHJ1ZTtcclxuICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGlmIChnZXRBYnNvbHV0ZVBhdGgoaHJlZikgPT09IGN1cnJlbnRBYnNQYXRoKSB7XHJcblxyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY29uc3QgZHJvcGRvd24gPSAkKGUpLmF0dHIoJ2RhdGEtdG9nZ2xlJykgPT09IFwiZHJvcGRvd25cIjtcclxuXHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBpZiAoIWRyb3Bkb3duKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgaXNBY3RpdmUgPSB0cnVlO1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIGlmIChpc0FjdGl2ZSkge1xyXG4gICAgICAgICAgICAgICAgICAgICAgICAkKGUpLmFkZENsYXNzKGFjdGl2ZSk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICAgICAgcmVuZGVyTmF2YmFyKCk7XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcbn1cclxuXHJcbmZ1bmN0aW9uIHJlbmRlckxvZ28oKSB7XHJcbiAgICAvLyBGb3IgTE9HTyBTVkdcclxuICAgIC8vIFJlcGxhY2UgU1ZHIHdpdGggaW5saW5lIFNWR1xyXG4gICAgLy8gaHR0cDovL3N0YWNrb3ZlcmZsb3cuY29tL3F1ZXN0aW9ucy8xMTk3ODk5NS9ob3ctdG8tY2hhbmdlLWNvbG9yLW9mLXN2Zy1pbWFnZS11c2luZy1jc3MtanF1ZXJ5LXN2Zy1pbWFnZS1yZXBsYWNlbWVudFxyXG4gICAgJCgnaW1nLnN2ZycpLmVhY2goZnVuY3Rpb24gKCkge1xyXG4gICAgICAgIGNvbnN0ICRpbWcgPSBqUXVlcnkodGhpcyk7XHJcbiAgICAgICAgY29uc3QgaW1nSUQgPSAkaW1nLmF0dHIoJ2lkJyk7XHJcbiAgICAgICAgY29uc3QgaW1nQ2xhc3MgPSAkaW1nLmF0dHIoJ2NsYXNzJyk7XHJcbiAgICAgICAgY29uc3QgaW1nVVJMID0gJGltZy5hdHRyKCdzcmMnKTtcclxuXHJcbiAgICAgICAgalF1ZXJ5LmdldChpbWdVUkwsIGZ1bmN0aW9uIChkYXRhKSB7XHJcbiAgICAgICAgICAgIC8vIEdldCB0aGUgU1ZHIHRhZywgaWdub3JlIHRoZSByZXN0XHJcbiAgICAgICAgICAgIGxldCAkc3ZnID0gJChkYXRhKS5maW5kKCdzdmcnKTtcclxuXHJcbiAgICAgICAgICAgIC8vIEFkZCByZXBsYWNlZCBpbWFnZSdzIElEIHRvIHRoZSBuZXcgU1ZHXHJcbiAgICAgICAgICAgIGlmICh0eXBlb2YgaW1nSUQgIT09ICd1bmRlZmluZWQnKSB7XHJcbiAgICAgICAgICAgICAgICAkc3ZnID0gJHN2Zy5hdHRyKCdpZCcsIGltZ0lEKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAvLyBBZGQgcmVwbGFjZWQgaW1hZ2UncyBjbGFzc2VzIHRvIHRoZSBuZXcgU1ZHXHJcbiAgICAgICAgICAgIGlmICh0eXBlb2YgaW1nQ2xhc3MgIT09ICd1bmRlZmluZWQnKSB7XHJcbiAgICAgICAgICAgICAgICAkc3ZnID0gJHN2Zy5hdHRyKCdjbGFzcycsIGltZ0NsYXNzICsgJyByZXBsYWNlZC1zdmcnKTtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgLy8gUmVtb3ZlIGFueSBpbnZhbGlkIFhNTCB0YWdzIGFzIHBlciBodHRwOi8vdmFsaWRhdG9yLnczLm9yZ1xyXG4gICAgICAgICAgICAkc3ZnID0gJHN2Zy5yZW1vdmVBdHRyKCd4bWxuczphJyk7XHJcblxyXG4gICAgICAgICAgICAvLyBSZXBsYWNlIGltYWdlIHdpdGggbmV3IFNWR1xyXG4gICAgICAgICAgICAkaW1nLnJlcGxhY2VXaXRoKCRzdmcpO1xyXG5cclxuICAgICAgICB9LCAneG1sJyk7XHJcbiAgICB9KTtcclxufVxyXG4iLCJmdW5jdGlvbiBzZXRUb2NIZWlnaHQoKSB7XHJcbiAgICBpZigkKHdpbmRvdykud2lkdGgoKSA8IDc2Nykge1xyXG4gICAgICAgIGxldCBoZWFkZXJIZWlnaHQgPSAkKFwiI2hlYWRlci1jb250YWluZXJcIikub3V0ZXJIZWlnaHQoKTtcclxuICAgICAgICBsZXQgYnJlYWRjcnVtYkhlaWdodCA9ICQoXCIjYnJlYWRjcnVtYlwiKS5vdXRlckhlaWdodCgpO1xyXG4gICAgICAgIGxldCB0b2NUb2dnbGVIZWlnaHQgPSAkKFwiLmJ0bi50b2MtdG9nZ2xlLmNvbGxhcHNlXCIpLm91dGVySGVpZ2h0KCk7XHJcbiAgICAgICAgbGV0IHNpZGVmaWx0ZXJIZWlnaHQgPSA2NTsgLy82NXB4IGZyb20gc2lkZWZpbHRlciBoZWlnaHRcclxuICAgICAgICBsZXQgaW50Vmlld3BvcnRIZWlnaHQgPSB3aW5kb3cuaW5uZXJIZWlnaHQ7XHJcbiAgICAgICAgbGV0IHNpZGVuYXZQYWRkaW5nVG9wID0gcGFyc2VJbnQoJChcIi5zaWRlbmF2XCIpLmNzcygncGFkZGluZy10b3AnKSk7XHJcbiAgICAgICAgbGV0IG1heEhlaWdodFRvYyA9IGludFZpZXdwb3J0SGVpZ2h0IC0gKGhlYWRlckhlaWdodCArIGJyZWFkY3J1bWJIZWlnaHQgKyB0b2NUb2dnbGVIZWlnaHQgKyBzaWRlZmlsdGVySGVpZ2h0ICsgc2lkZW5hdlBhZGRpbmdUb3ApO1xyXG4gICAgICAgICQoXCIuc2lkZXRvY1wiKS5jc3MoXCJtYXgtaGVpZ2h0XCIsIG1heEhlaWdodFRvYyk7XHJcbiAgICB9IGVsc2Uge1xyXG4gICAgICAgICQoXCIuc2lkZXRvY1wiKS5jc3MoXCJtYXgtaGVpZ2h0XCIsIFwibm9uZVwiKTtcclxuICAgIH1cclxufVxyXG5cclxuZnVuY3Rpb24gdXBkYXRlVG9jSGVpZ2h0T25SZXNpemUoKSB7XHJcbiAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZScsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICBzZXRUb2NIZWlnaHQoKTtcclxuICAgIH0pO1xyXG59XHJcblxyXG5mdW5jdGlvbiBzZXRTaWRlbmF2VG9wKCkge1xyXG4gICAgbGV0IGhlYWRlckhlaWdodCA9ICQoXCIjaGVhZGVyLWNvbnRhaW5lclwiKS5vdXRlckhlaWdodCgpO1xyXG4gICAgbGV0IGJyZWFkY3J1bWJIZWlnaHQgPSAkKFwiI2JyZWFkY3J1bWJcIikub3V0ZXJIZWlnaHQoKTtcclxuICAgIGxldCB0b2NUb2dnbGVIZWlnaHQgPSAkKFwiLmJ0bi50b2MtdG9nZ2xlLmNvbGxhcHNlXCIpLm91dGVySGVpZ2h0KCk7XHJcbiAgICBsZXQgc2lkZWZpbHRlckhlaWdodCA9ICQoXCIuc2lkZWZpbHRlclwiKS5vdXRlckhlaWdodCgpO1xyXG4gICAgbGV0IHNpZGVuYXZUb3AgPSBoZWFkZXJIZWlnaHQgKyBicmVhZGNydW1iSGVpZ2h0O1xyXG4gICAgbGV0IHNpZGVmaWx0ZXJUb3AgPSBoZWFkZXJIZWlnaHQgKyBicmVhZGNydW1iSGVpZ2h0O1xyXG4gICAgbGV0IHNpZGV0b2NUb3AgPSBzaWRlZmlsdGVyVG9wICsgc2lkZWZpbHRlckhlaWdodDtcclxuICAgIGxldCBhcnRpY2xlTWFyZ2luVG9wRGVzayA9IHNpZGVuYXZUb3AgKyB0b2NUb2dnbGVIZWlnaHQgKyAzMDsgLy8zMHB4IGZyb20gLnNpZGVuYXYgcGFkZGluZyB0b3AgYW5kIGJvdHRvbVxyXG4gICAgbGV0IGFydGljbGVNYXJnaW5Ub3BNb2JpbGUgPSBzaWRlbmF2VG9wO1xyXG4gICAgJChcIi5zaWRlbmF2XCIpLmNzcyhcInRvcFwiLCBzaWRlbmF2VG9wKTtcclxuICAgICQoXCIuc2lkZWZpbHRlclwiKS5jc3MoXCJ0b3BcIiwgc2lkZW5hdlRvcCk7XHJcbiAgICAkKFwiLnNpZGV0b2NcIikuY3NzKFwidG9wXCIsIHNpZGV0b2NUb3ApO1xyXG4gICAgaWYoJCh3aW5kb3cpLndpZHRoKCkgPCA3NjcpIHtcclxuICAgICAgICAkKFwiLmJvZHktY29udGVudCAuYXJ0aWNsZVwiKS5hdHRyKFwic3R5bGVcIiwgXCJtYXJnaW4tdG9wOlwiICsgKGFydGljbGVNYXJnaW5Ub3BEZXNrICsgNSkgKyBcInB4ICFpbXBvcnRhbnRcIik7XHJcbiAgICB9IGVsc2Uge1xyXG4gICAgICAgICQoXCIuYm9keS1jb250ZW50IC5hcnRpY2xlXCIpLmF0dHIoXCJzdHlsZVwiLCBcIm1hcmdpbi10b3A6XCIgKyAoYXJ0aWNsZU1hcmdpblRvcE1vYmlsZSArIDUpICsgXCJweCAhaW1wb3J0YW50XCIpO1xyXG4gICAgfVxyXG59XHJcblxyXG5mdW5jdGlvbiB1cGRhdGVTaWRlbmF2VG9wT25SZXNpemUoKSB7XHJcbiAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZScsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICBzZXRTaWRlbmF2VG9wKCk7XHJcbiAgICB9KTtcclxufVxyXG5cclxuZnVuY3Rpb24gcmVuZGVyU2lkZWJhcigpIHtcclxuXHJcbiAgICBjb25zdCBzaWRlVG9nZ2xlU2lkZVRvYyA9ICQoJyNzaWRldG9nZ2xlIC5zaWRldG9jJylbMF07XHJcbiAgICBjb25zdCBmb290ZXIgPSAkKCdmb290ZXInKTtcclxuICAgIGNvbnN0IHNpZGV0b2MgPSAkKCcuc2lkZXRvYycpO1xyXG5cclxuICAgIGlmICh0eXBlb2YgKHNpZGVUb2dnbGVTaWRlVG9jKSA9PT0gJ3VuZGVmaW5lZCcpIHtcclxuICAgICAgICBsb2FkVG9jKCk7XHJcbiAgICB9IGVsc2Uge1xyXG4gICAgICAgIHJlZ2lzdGVyVG9jRXZlbnRzKCk7XHJcbiAgICAgICAgaWYgKGZvb3Rlci5pcygnOnZpc2libGUnKSkge1xyXG4gICAgICAgICAgICBzaWRldG9jLmFkZENsYXNzKCdzaGlmdHVwJyk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICAvLyBTY3JvbGwgdG8gYWN0aXZlIGl0ZW1cclxuICAgICAgICBsZXQgdG9wID0gMDtcclxuICAgICAgICAkKCcjdG9jIGEuYWN0aXZlJykucGFyZW50cygnbGknKS5lYWNoKGZ1bmN0aW9uIChpLCBlKSB7XHJcbiAgICAgICAgICAgICQoZSkuYWRkQ2xhc3MoYWN0aXZlKS5hZGRDbGFzcyhleHBhbmRlZCk7XHJcbiAgICAgICAgICAgICQoZSkuY2hpbGRyZW4oJ2EnKS5hZGRDbGFzcyhhY3RpdmUpO1xyXG4gICAgICAgICAgICB0b3AgKz0gJChlKS5wb3NpdGlvbigpLnRvcDtcclxuICAgICAgICB9KVxyXG5cclxuICAgICAgICBzaWRldG9jLnNjcm9sbFRvcCh0b3AgLSA1MCk7XHJcblxyXG4gICAgICAgIGlmIChmb290ZXIuaXMoJzp2aXNpYmxlJykpIHtcclxuICAgICAgICAgICAgc2lkZXRvYy5hZGRDbGFzcygnc2hpZnR1cCcpO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKHdpbmRvdy5sb2NhdGlvbi5ocmVmLmluZGV4T2YoXCJhcnRpY2xlcy9pbnRyby5odG1sXCIpID4gLTEgJiYgJCh3aW5kb3cpLndpZHRoKCkgPiA4NTApIHtcclxuICAgICAgICAgICAgJCgnLm5hdi5sZXZlbDEgbGk6ZXEoMSknKS5hZGRDbGFzcyhleHBhbmRlZCk7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZW5kZXJCcmVhZGNydW1iKCk7XHJcbiAgICAgICAgc2V0U2lkZW5hdlRvcCgpO1xyXG4gICAgICAgIHNldFRvY0hlaWdodCgpO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIHJlZ2lzdGVyVG9jRXZlbnRzKCkge1xyXG4gICAgICAgICQoJy50b2MgLm5hdiA+IGxpID4gLmV4cGFuZC1zdHViJykub24oJ2NsaWNrJywgZnVuY3Rpb24gKGUpIHtcclxuICAgICAgICAgICAgJChlLnRhcmdldCkucGFyZW50KCkudG9nZ2xlQ2xhc3MoZXhwYW5kZWQpO1xyXG4gICAgICAgIH0pO1xyXG4gICAgICAgICQoJy50b2MgLm5hdiA+IGxpID4gLmV4cGFuZC1zdHViICsgYTpub3QoW2hyZWZdKScpLm9uKCdjbGljaycsIGZ1bmN0aW9uIChlKSB7XHJcbiAgICAgICAgICAgICQoZS50YXJnZXQpLnBhcmVudCgpLnRvZ2dsZUNsYXNzKGV4cGFuZGVkKTtcclxuICAgICAgICB9KTtcclxuICAgICAgICAkKCcjdG9jX2ZpbHRlcl9pbnB1dCcpLm9uKCdpbnB1dCcsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgY29uc3QgdmFsID0gdGhpcy52YWx1ZTtcclxuICAgICAgICAgICAgaWYgKHZhbCA9PT0gJycpIHtcclxuICAgICAgICAgICAgICAgIC8vIENsZWFyICdmaWx0ZXJlZCcgY2xhc3NcclxuICAgICAgICAgICAgICAgICQoJyN0b2MgbGknKS5yZW1vdmVDbGFzcyhmaWx0ZXJlZCkucmVtb3ZlQ2xhc3MoaGlkZSk7XHJcbiAgICAgICAgICAgICAgICByZXR1cm47XHJcbiAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgIC8vIEdldCBsZWFmIG5vZGVzXHJcbiAgICAgICAgICAgIGNvbnN0IHRvY0xpbmVBbmNob3IgPSAkKCcjdG9jIGxpPmEnKTtcclxuXHJcbiAgICAgICAgICAgIHRvY0xpbmVBbmNob3IuZmlsdGVyKGZ1bmN0aW9uIChpLCBlKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gJChlKS5zaWJsaW5ncygpLmxlbmd0aCA9PT0gMFxyXG4gICAgICAgICAgICB9KS5lYWNoKGZ1bmN0aW9uIChqLCBhbmNob3IpIHtcclxuICAgICAgICAgICAgICAgIGxldCB0ZXh0ID0gJChhbmNob3IpLmF0dHIoJ3RpdGxlJyk7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBwYXJlbnQgPSAkKGFuY2hvcikucGFyZW50KCk7XHJcbiAgICAgICAgICAgICAgICBjb25zdCBwYXJlbnROb2RlcyA9IHBhcmVudC5wYXJlbnRzKCd1bD5saScpO1xyXG4gICAgICAgICAgICAgICAgZm9yIChsZXQgayA9IDA7IGsgPCBwYXJlbnROb2Rlcy5sZW5ndGg7IGsrKykge1xyXG4gICAgICAgICAgICAgICAgICAgIGxldCBwYXJlbnRUZXh0ID0gJChwYXJlbnROb2Rlc1trXSkuY2hpbGRyZW4oJ2EnKS5hdHRyKCd0aXRsZScpO1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChwYXJlbnRUZXh0KSB0ZXh0ID0gcGFyZW50VGV4dCArICcuJyArIHRleHQ7XHJcbiAgICAgICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICAgICAgaWYgKGZpbHRlck5hdkl0ZW0odGV4dCwgdmFsKSkge1xyXG4gICAgICAgICAgICAgICAgICAgIHBhcmVudC5hZGRDbGFzcyhzaG93KTtcclxuICAgICAgICAgICAgICAgICAgICBwYXJlbnQucmVtb3ZlQ2xhc3MoaGlkZSk7XHJcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgICAgIHBhcmVudC5hZGRDbGFzcyhoaWRlKTtcclxuICAgICAgICAgICAgICAgICAgICBwYXJlbnQucmVtb3ZlQ2xhc3Moc2hvdyk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH0pO1xyXG5cclxuICAgICAgICAgICAgdG9jTGluZUFuY2hvci5maWx0ZXIoZnVuY3Rpb24gKGksIGUpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiAkKGUpLnNpYmxpbmdzKCkubGVuZ3RoID4gMFxyXG4gICAgICAgICAgICB9KS5lYWNoKGZ1bmN0aW9uIChpLCBhbmNob3IpIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IHBhcmVudCA9ICQoYW5jaG9yKS5wYXJlbnQoKTtcclxuICAgICAgICAgICAgICAgIGlmIChwYXJlbnQuZmluZCgnbGkuc2hvdycpLmxlbmd0aCA+IDApIHtcclxuICAgICAgICAgICAgICAgICAgICBwYXJlbnQuYWRkQ2xhc3Moc2hvdyk7XHJcbiAgICAgICAgICAgICAgICAgICAgcGFyZW50LmFkZENsYXNzKGZpbHRlcmVkKTtcclxuICAgICAgICAgICAgICAgICAgICBwYXJlbnQucmVtb3ZlQ2xhc3MoaGlkZSk7XHJcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgICAgIHBhcmVudC5hZGRDbGFzcyhoaWRlKTtcclxuICAgICAgICAgICAgICAgICAgICBwYXJlbnQucmVtb3ZlQ2xhc3Moc2hvdyk7XHJcbiAgICAgICAgICAgICAgICAgICAgcGFyZW50LnJlbW92ZUNsYXNzKGZpbHRlcmVkKTtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgfSlcclxuXHJcbiAgICAgICAgICAgIGZ1bmN0aW9uIGZpbHRlck5hdkl0ZW0obmFtZSwgdGV4dCkge1xyXG5cclxuICAgICAgICAgICAgICAgIGlmICghdGV4dCkgcmV0dXJuIHRydWU7XHJcblxyXG4gICAgICAgICAgICAgICAgcmV0dXJuIG5hbWUgJiYgbmFtZS50b0xvd2VyQ2FzZSgpLmluZGV4T2YodGV4dC50b0xvd2VyQ2FzZSgpKSA+IC0xO1xyXG5cclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIGxvYWRUb2MoKSB7XHJcbiAgICAgICAgbGV0IHRvY1BhdGggPSAkKFwibWV0YVtwcm9wZXJ0eT0nZG9jZnhcXFxcOnRvY3JlbCddXCIpLmF0dHIoXCJjb250ZW50XCIpO1xyXG5cclxuICAgICAgICBpZiAoIXRvY1BhdGgpIHtcclxuICAgICAgICAgICAgcmV0dXJuO1xyXG4gICAgICAgIH1cclxuICAgICAgICB0b2NQYXRoID0gdG9jUGF0aC5yZXBsYWNlKC9cXFxcL2csICcvJyk7XHJcbiAgICAgICAgJCgnI3NpZGV0b2MnKS5sb2FkKHRvY1BhdGggKyBcIiAjc2lkZXRvZ2dsZSA+IGRpdlwiLCBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgIGNvbnN0IGluZGV4ID0gdG9jUGF0aC5sYXN0SW5kZXhPZignLycpO1xyXG4gICAgICAgICAgICBsZXQgdG9jcmVsID0gJyc7XHJcblxyXG4gICAgICAgICAgICBpZiAoaW5kZXggPiAtMSkge1xyXG4gICAgICAgICAgICAgICAgdG9jcmVsID0gdG9jUGF0aC5zdWJzdHIoMCwgaW5kZXggKyAxKTtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgY29uc3QgY3VycmVudEhyZWYgPSBnZXRBYnNvbHV0ZVBhdGgod2luZG93LmxvY2F0aW9uLnBhdGhuYW1lKTtcclxuXHJcbiAgICAgICAgICAgICQoJyNzaWRldG9jJykuZmluZCgnYVtocmVmXScpLmVhY2goZnVuY3Rpb24gKGksIGUpIHtcclxuICAgICAgICAgICAgICAgIGxldCBocmVmID0gJChlKS5hdHRyKFwiaHJlZlwiKTtcclxuICAgICAgICAgICAgICAgIGlmIChpc1JlbGF0aXZlUGF0aChocmVmKSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGhyZWYgPSB0b2NyZWwgKyBocmVmO1xyXG4gICAgICAgICAgICAgICAgICAgICQoZSkuYXR0cihcImhyZWZcIiwgaHJlZik7XHJcbiAgICAgICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICAgICAgaWYgKGdldEFic29sdXRlUGF0aChlLmhyZWYpID09PSBjdXJyZW50SHJlZikge1xyXG4gICAgICAgICAgICAgICAgICAgICQoZSkuYWRkQ2xhc3MoYWN0aXZlKTtcclxuICAgICAgICAgICAgICAgIH1cclxuXHJcbiAgICAgICAgICAgICAgICAkKGUpLmJyZWFrV29yZCgpO1xyXG4gICAgICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgICAgIHJlbmRlclNpZGViYXIoKTtcclxuICAgICAgICAgICAgY29uc3QgYm9keSA9ICQoJ2JvZHknKTtcclxuICAgICAgICAgICAgY29uc3Qgc2VhcmNoUmVzdWx0ID0gJCgnI3NlYXJjaC1yZXN1bHRzJyk7XHJcblxyXG4gICAgICAgICAgICBpZiAoc2VhcmNoUmVzdWx0Lmxlbmd0aCAhPT0gMCkge1xyXG4gICAgICAgICAgICAgICAgJCgnI3NlYXJjaCcpLnNob3coKTtcclxuICAgICAgICAgICAgICAgIGJvZHkudHJpZ2dlcihcInNlYXJjaEV2ZW50XCIpO1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICAvLyBpZiB0aGUgdGFyZ2V0IG9mIHRoZSBjbGljayBpc24ndCB0aGUgY29udGFpbmVyIG5vciBhIGRlc2NlbmRhbnQgb2YgdGhlIGNvbnRhaW5lclxyXG4gICAgICAgICAgICBib2R5Lm9uKCdtb3VzZXVwJywgZnVuY3Rpb24gKGUpIHtcclxuICAgICAgICAgICAgICAgIGlmICghc2VhcmNoUmVzdWx0LmlzKGUudGFyZ2V0KSAmJiBzZWFyY2hSZXN1bHQuaGFzKGUudGFyZ2V0KS5sZW5ndGggPT09IDApIHtcclxuICAgICAgICAgICAgICAgICAgICBzZWFyY2hSZXN1bHQuaGlkZSgpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICB9KTtcclxuICAgIH1cclxufVxyXG4iLCJmdW5jdGlvbiByZW5kZXJUYWJzKCkge1xyXG5cclxuICAgIGNvbnN0IGNvbnRlbnRBdHRycyA9IHtcclxuICAgICAgICBpZDogJ2RhdGEtYmktaWQnLFxyXG4gICAgICAgIG5hbWU6ICdkYXRhLWJpLW5hbWUnLFxyXG4gICAgICAgIHR5cGU6ICdkYXRhLWJpLXR5cGUnXHJcbiAgICB9O1xyXG5cclxuICAgIGNvbnN0IFRhYiA9IChmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgZnVuY3Rpb24gVGFiKGxpLCBhLCBzZWN0aW9uKSB7XHJcbiAgICAgICAgICAgIHRoaXMubGkgPSBsaTtcclxuICAgICAgICAgICAgdGhpcy5hID0gYTtcclxuICAgICAgICAgICAgdGhpcy5zZWN0aW9uID0gc2VjdGlvbjtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIE9iamVjdC5kZWZpbmVQcm9wZXJ0eShUYWIucHJvdG90eXBlLCBcInRhYklkc1wiLCB7XHJcbiAgICAgICAgICAgIGdldDogZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXMuYS5nZXRBdHRyaWJ1dGUoJ2RhdGEtdGFiJykuc3BsaXQoJyAnKTtcclxuICAgICAgICAgICAgfSxcclxuICAgICAgICAgICAgZW51bWVyYWJsZTogdHJ1ZSxcclxuICAgICAgICAgICAgY29uZmlndXJhYmxlOiB0cnVlXHJcbiAgICAgICAgfSk7XHJcblxyXG4gICAgICAgIE9iamVjdC5kZWZpbmVQcm9wZXJ0eShUYWIucHJvdG90eXBlLCBcImNvbmRpdGlvblwiLCB7XHJcbiAgICAgICAgICAgIGdldDogZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXMuYS5nZXRBdHRyaWJ1dGUoJ2RhdGEtY29uZGl0aW9uJyk7XHJcbiAgICAgICAgICAgIH0sXHJcbiAgICAgICAgICAgIGVudW1lcmFibGU6IHRydWUsXHJcbiAgICAgICAgICAgIGNvbmZpZ3VyYWJsZTogdHJ1ZVxyXG4gICAgICAgIH0pO1xyXG5cclxuICAgICAgICBPYmplY3QuZGVmaW5lUHJvcGVydHkoVGFiLnByb3RvdHlwZSwgXCJ2aXNpYmxlXCIsIHtcclxuICAgICAgICAgICAgZ2V0OiBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgICAgICByZXR1cm4gIXRoaXMubGkuaGFzQXR0cmlidXRlKCdoaWRkZW4nKTtcclxuICAgICAgICAgICAgfSxcclxuICAgICAgICAgICAgc2V0OiBmdW5jdGlvbiAodmFsdWUpIHtcclxuICAgICAgICAgICAgICAgIGlmICh2YWx1ZSkge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMubGkucmVtb3ZlQXR0cmlidXRlKCdoaWRkZW4nKTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmxpLnJlbW92ZUF0dHJpYnV0ZSgnYXJpYS1oaWRkZW4nKTtcclxuICAgICAgICAgICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5saS5zZXRBdHRyaWJ1dGUoJ2hpZGRlbicsICdoaWRkZW4nKTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmxpLnNldEF0dHJpYnV0ZSgnYXJpYS1oaWRkZW4nLCAndHJ1ZScpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9LFxyXG4gICAgICAgICAgICBlbnVtZXJhYmxlOiB0cnVlLFxyXG4gICAgICAgICAgICBjb25maWd1cmFibGU6IHRydWVcclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgT2JqZWN0LmRlZmluZVByb3BlcnR5KFRhYi5wcm90b3R5cGUsIFwic2VsZWN0ZWRcIiwge1xyXG4gICAgICAgICAgICBnZXQ6IGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiAhdGhpcy5zZWN0aW9uLmhhc0F0dHJpYnV0ZSgnaGlkZGVuJyk7XHJcbiAgICAgICAgICAgIH0sXHJcbiAgICAgICAgICAgIHNldDogZnVuY3Rpb24gKHZhbHVlKSB7XHJcbiAgICAgICAgICAgICAgICBpZiAodmFsdWUpIHtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmEuc2V0QXR0cmlidXRlKCdhcmlhLXNlbGVjdGVkJywgJ3RydWUnKTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmEudGFiSW5kZXggPSAwO1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuc2VjdGlvbi5yZW1vdmVBdHRyaWJ1dGUoJ2hpZGRlbicpO1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuc2VjdGlvbi5yZW1vdmVBdHRyaWJ1dGUoJ2FyaWEtaGlkZGVuJyk7XHJcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgICAgIHRoaXMuYS5zZXRBdHRyaWJ1dGUoJ2FyaWEtc2VsZWN0ZWQnLCAnZmFsc2UnKTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLmEudGFiSW5kZXggPSAtMTtcclxuICAgICAgICAgICAgICAgICAgICB0aGlzLnNlY3Rpb24uc2V0QXR0cmlidXRlKCdoaWRkZW4nLCAnaGlkZGVuJyk7XHJcbiAgICAgICAgICAgICAgICAgICAgdGhpcy5zZWN0aW9uLnNldEF0dHJpYnV0ZSgnYXJpYS1oaWRkZW4nLCAndHJ1ZScpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9LFxyXG4gICAgICAgICAgICBlbnVtZXJhYmxlOiB0cnVlLFxyXG4gICAgICAgICAgICBjb25maWd1cmFibGU6IHRydWVcclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgVGFiLnByb3RvdHlwZS5mb2N1cyA9IGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgdGhpcy5hLmZvY3VzKCk7XHJcbiAgICAgICAgfTtcclxuXHJcbiAgICAgICAgcmV0dXJuIFRhYjtcclxuXHJcbiAgICB9KCkpO1xyXG5cclxuICAgIGluaXRUYWJzKGRvY3VtZW50LmJvZHkpO1xyXG5cclxuICAgIGZ1bmN0aW9uIGluaXRUYWJzKGNvbnRhaW5lcikge1xyXG4gICAgICAgIGNvbnN0IHF1ZXJ5U3RyaW5nVGFicyA9IHJlYWRUYWJzUXVlcnlTdHJpbmdQYXJhbSgpO1xyXG4gICAgICAgIGNvbnN0IGVsZW1lbnRzID0gY29udGFpbmVyLnF1ZXJ5U2VsZWN0b3JBbGwoJy50YWJHcm91cCcpO1xyXG4gICAgICAgIGNvbnN0IHN0YXRlID0ge2dyb3VwczogW10sIHNlbGVjdGVkVGFiczogW119O1xyXG4gICAgICAgIGZvciAobGV0IGkgPSAwOyBpIDwgZWxlbWVudHMubGVuZ3RoOyBpKyspIHtcclxuICAgICAgICAgICAgY29uc3QgZ3JvdXAgPSBpbml0VGFiR3JvdXAoZWxlbWVudHMuaXRlbShpKSk7XHJcbiAgICAgICAgICAgIGlmICghZ3JvdXAuaW5kZXBlbmRlbnQpIHtcclxuICAgICAgICAgICAgICAgIHVwZGF0ZVZpc2liaWxpdHlBbmRTZWxlY3Rpb24oZ3JvdXAsIHN0YXRlKTtcclxuICAgICAgICAgICAgICAgIHN0YXRlLmdyb3Vwcy5wdXNoKGdyb3VwKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgICAgICBjb250YWluZXIuYWRkRXZlbnRMaXN0ZW5lcignY2xpY2snLCBmdW5jdGlvbiAoZXZlbnQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIGhhbmRsZUNsaWNrKGV2ZW50LCBzdGF0ZSk7XHJcbiAgICAgICAgfSk7XHJcbiAgICAgICAgaWYgKHN0YXRlLmdyb3Vwcy5sZW5ndGggPT09IDApIHtcclxuICAgICAgICAgICAgcmV0dXJuIHN0YXRlO1xyXG4gICAgICAgIH1cclxuICAgICAgICBzZWxlY3RUYWJzKHF1ZXJ5U3RyaW5nVGFicywgY29udGFpbmVyKTtcclxuICAgICAgICB1cGRhdGVUYWJzUXVlcnlTdHJpbmdQYXJhbShzdGF0ZSk7XHJcbiAgICAgICAgbm90aWZ5Q29udGVudFVwZGF0ZWQoKTtcclxuICAgICAgICByZXR1cm4gc3RhdGU7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gaW5pdFRhYkdyb3VwKGVsZW1lbnQpIHtcclxuXHJcbiAgICAgICAgY29uc3QgZ3JvdXAgPSB7XHJcbiAgICAgICAgICAgIGluZGVwZW5kZW50OiBlbGVtZW50Lmhhc0F0dHJpYnV0ZSgnZGF0YS10YWItZ3JvdXAtaW5kZXBlbmRlbnQnKSxcclxuICAgICAgICAgICAgdGFiczogW11cclxuICAgICAgICB9O1xyXG5cclxuICAgICAgICBsZXQgbGkgPSBlbGVtZW50LmZpcnN0RWxlbWVudENoaWxkLmZpcnN0RWxlbWVudENoaWxkO1xyXG4gICAgICAgIHdoaWxlIChsaSkge1xyXG4gICAgICAgICAgICBjb25zdCBhID0gbGkuZmlyc3RFbGVtZW50Q2hpbGQ7XHJcbiAgICAgICAgICAgIGEuc2V0QXR0cmlidXRlKGNvbnRlbnRBdHRycy5uYW1lLCAndGFiJyk7XHJcblxyXG4gICAgICAgICAgICBjb25zdCBkYXRhVGFiID0gYS5nZXRBdHRyaWJ1dGUoJ2RhdGEtdGFiJykucmVwbGFjZSgvXFwrL2csICcgJyk7XHJcbiAgICAgICAgICAgIGEuc2V0QXR0cmlidXRlKCdkYXRhLXRhYicsIGRhdGFUYWIpO1xyXG5cclxuICAgICAgICAgICAgY29uc3Qgc2VjdGlvbiA9IGVsZW1lbnQucXVlcnlTZWxlY3RvcihcIltpZD1cXFwiXCIgKyBhLmdldEF0dHJpYnV0ZSgnYXJpYS1jb250cm9scycpICsgXCJcXFwiXVwiKTtcclxuICAgICAgICAgICAgY29uc3QgdGFiID0gbmV3IFRhYihsaSwgYSwgc2VjdGlvbik7XHJcbiAgICAgICAgICAgIGdyb3VwLnRhYnMucHVzaCh0YWIpO1xyXG5cclxuICAgICAgICAgICAgbGkgPSBsaS5uZXh0RWxlbWVudFNpYmxpbmc7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBlbGVtZW50LnNldEF0dHJpYnV0ZShjb250ZW50QXR0cnMubmFtZSwgJ3RhYi1ncm91cCcpO1xyXG4gICAgICAgIGVsZW1lbnQudGFiR3JvdXAgPSBncm91cDtcclxuXHJcbiAgICAgICAgcmV0dXJuIGdyb3VwO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIHVwZGF0ZVZpc2liaWxpdHlBbmRTZWxlY3Rpb24oZ3JvdXAsIHN0YXRlKSB7XHJcbiAgICAgICAgbGV0IGFueVNlbGVjdGVkID0gZmFsc2U7XHJcbiAgICAgICAgbGV0IGZpcnN0VmlzaWJsZVRhYjtcclxuXHJcbiAgICAgICAgZm9yIChsZXQgX2kgPSAwLCBfYSA9IGdyb3VwLnRhYnM7IF9pIDwgX2EubGVuZ3RoOyBfaSsrKSB7XHJcbiAgICAgICAgICAgIGxldCB0YWIgPSBfYVtfaV07XHJcbiAgICAgICAgICAgIHRhYi52aXNpYmxlID0gdGFiLmNvbmRpdGlvbiA9PT0gbnVsbCB8fCBzdGF0ZS5zZWxlY3RlZFRhYnMuaW5kZXhPZih0YWIuY29uZGl0aW9uKSAhPT0gLTE7XHJcbiAgICAgICAgICAgIGlmICh0YWIudmlzaWJsZSkge1xyXG4gICAgICAgICAgICAgICAgaWYgKCFmaXJzdFZpc2libGVUYWIpIHtcclxuICAgICAgICAgICAgICAgICAgICBmaXJzdFZpc2libGVUYWIgPSB0YWI7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgdGFiLnNlbGVjdGVkID0gdGFiLnZpc2libGUgJiYgYXJyYXlzSW50ZXJzZWN0KHN0YXRlLnNlbGVjdGVkVGFicywgdGFiLnRhYklkcyk7XHJcbiAgICAgICAgICAgIGFueVNlbGVjdGVkID0gYW55U2VsZWN0ZWQgfHwgdGFiLnNlbGVjdGVkO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgaWYgKCFhbnlTZWxlY3RlZCkge1xyXG4gICAgICAgICAgICBmb3IgKGxldCBfYiA9IDAsIF9jID0gZ3JvdXAudGFiczsgX2IgPCBfYy5sZW5ndGg7IF9iKyspIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IHRhYklkcyA9IF9jW19iXS50YWJJZHM7XHJcbiAgICAgICAgICAgICAgICBmb3IgKGxldCBfZCA9IDAsIHRhYklkc18xID0gdGFiSWRzOyBfZCA8IHRhYklkc18xLmxlbmd0aDsgX2QrKykge1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IHRhYklkID0gdGFiSWRzXzFbX2RdO1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGluZGV4ID0gc3RhdGUuc2VsZWN0ZWRUYWJzLmluZGV4T2YodGFiSWQpO1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChpbmRleCA9PT0gLTEpIHtcclxuICAgICAgICAgICAgICAgICAgICAgICAgY29udGludWU7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgICAgIHN0YXRlLnNlbGVjdGVkVGFicy5zcGxpY2UoaW5kZXgsIDEpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIGNvbnN0IHRhYiA9IGZpcnN0VmlzaWJsZVRhYjtcclxuICAgICAgICAgICAgdGFiLnNlbGVjdGVkID0gdHJ1ZTtcclxuICAgICAgICAgICAgc3RhdGUuc2VsZWN0ZWRUYWJzLnB1c2godGFiLnRhYklkc1swXSk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIGdldFRhYkluZm9Gcm9tRXZlbnQoZXZlbnQpIHtcclxuXHJcbiAgICAgICAgaWYgKCEoZXZlbnQudGFyZ2V0IGluc3RhbmNlb2YgSFRNTEVsZW1lbnQpKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBudWxsO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgY29uc3QgYW5jaG9yID0gZXZlbnQudGFyZ2V0LmNsb3Nlc3QoJ2FbZGF0YS10YWJdJyk7XHJcblxyXG4gICAgICAgIGlmIChhbmNob3IgPT09IG51bGwpIHtcclxuICAgICAgICAgICAgcmV0dXJuIG51bGw7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBjb25zdCB0YWJJZHMgPSBhbmNob3IuZ2V0QXR0cmlidXRlKCdkYXRhLXRhYicpLnNwbGl0KCcgJyk7XHJcbiAgICAgICAgY29uc3QgZ3JvdXAgPSBhbmNob3IucGFyZW50RWxlbWVudC5wYXJlbnRFbGVtZW50LnBhcmVudEVsZW1lbnQudGFiR3JvdXA7XHJcblxyXG4gICAgICAgIGlmIChncm91cCA9PT0gdW5kZWZpbmVkKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBudWxsO1xyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIHt0YWJJZHM6IHRhYklkcywgZ3JvdXA6IGdyb3VwLCBhbmNob3I6IGFuY2hvcn07XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gaGFuZGxlQ2xpY2soZXZlbnQsIHN0YXRlKSB7XHJcbiAgICAgICAgY29uc3QgaW5mbyA9IGdldFRhYkluZm9Gcm9tRXZlbnQoZXZlbnQpO1xyXG5cclxuICAgICAgICBpZiAoaW5mbyA9PT0gbnVsbCkge1xyXG4gICAgICAgICAgICByZXR1cm47XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBldmVudC5wcmV2ZW50RGVmYXVsdCgpO1xyXG4gICAgICAgIGluZm8uYW5jaG9yLmhyZWYgPSAnamF2YXNjcmlwdDonO1xyXG5cclxuICAgICAgICBzZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgcmV0dXJuIGluZm8uYW5jaG9yLmhyZWYgPSAnIycgKyBpbmZvLmFuY2hvci5nZXRBdHRyaWJ1dGUoJ2FyaWEtY29udHJvbHMnKTtcclxuICAgICAgICB9KTtcclxuXHJcbiAgICAgICAgY29uc3QgdGFiSWRzID0gaW5mby50YWJJZHMsIGdyb3VwID0gaW5mby5ncm91cDtcclxuICAgICAgICBjb25zdCBvcmlnaW5hbFRvcCA9IGluZm8uYW5jaG9yLmdldEJvdW5kaW5nQ2xpZW50UmVjdCgpLnRvcDtcclxuXHJcbiAgICAgICAgaWYgKGdyb3VwLmluZGVwZW5kZW50KSB7XHJcbiAgICAgICAgICAgIGZvciAobGV0IF9pID0gMCwgX2EgPSBncm91cC50YWJzOyBfaSA8IF9hLmxlbmd0aDsgX2krKykge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgdGFiID0gX2FbX2ldO1xyXG4gICAgICAgICAgICAgICAgdGFiLnNlbGVjdGVkID0gYXJyYXlzSW50ZXJzZWN0KHRhYi50YWJJZHMsIHRhYklkcyk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICBpZiAoYXJyYXlzSW50ZXJzZWN0KHN0YXRlLnNlbGVjdGVkVGFicywgdGFiSWRzKSkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIGNvbnN0IHByZXZpb3VzVGFiSWQgPSBncm91cC50YWJzLmZpbHRlcihmdW5jdGlvbiAodCkge1xyXG4gICAgICAgICAgICAgICAgcmV0dXJuIHQuc2VsZWN0ZWQ7XHJcbiAgICAgICAgICAgIH0pWzBdLnRhYklkc1swXTtcclxuICAgICAgICAgICAgc3RhdGUuc2VsZWN0ZWRUYWJzLnNwbGljZShzdGF0ZS5zZWxlY3RlZFRhYnMuaW5kZXhPZihwcmV2aW91c1RhYklkKSwgMSwgdGFiSWRzWzBdKTtcclxuICAgICAgICAgICAgZm9yIChsZXQgX2IgPSAwLCBfYyA9IHN0YXRlLmdyb3VwczsgX2IgPCBfYy5sZW5ndGg7IF9iKyspIHtcclxuICAgICAgICAgICAgICAgIGNvbnN0IGdyb3VwXzEgPSBfY1tfYl07XHJcbiAgICAgICAgICAgICAgICB1cGRhdGVWaXNpYmlsaXR5QW5kU2VsZWN0aW9uKGdyb3VwXzEsIHN0YXRlKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB1cGRhdGVUYWJzUXVlcnlTdHJpbmdQYXJhbShzdGF0ZSk7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIG5vdGlmeUNvbnRlbnRVcGRhdGVkKCk7XHJcbiAgICAgICAgY29uc3QgdG9wID0gaW5mby5hbmNob3IuZ2V0Qm91bmRpbmdDbGllbnRSZWN0KCkudG9wO1xyXG4gICAgICAgIGlmICh0b3AgIT09IG9yaWdpbmFsVG9wICYmIGV2ZW50IGluc3RhbmNlb2YgTW91c2VFdmVudCkge1xyXG4gICAgICAgICAgICB3aW5kb3cuc2Nyb2xsVG8oMCwgd2luZG93LnBhZ2VZT2Zmc2V0ICsgdG9wIC0gb3JpZ2luYWxUb3ApO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBmdW5jdGlvbiBzZWxlY3RUYWJzKHRhYklkcykge1xyXG4gICAgICAgIGZvciAobGV0IF9pID0gMCwgdGFiSWRzXzEgPSB0YWJJZHM7IF9pIDwgdGFiSWRzXzEubGVuZ3RoOyBfaSsrKSB7XHJcbiAgICAgICAgICAgIGNvbnN0IHRhYklkID0gdGFiSWRzXzFbX2ldO1xyXG4gICAgICAgICAgICBjb25zdCBhID0gZG9jdW1lbnQucXVlcnlTZWxlY3RvcihcIi50YWJHcm91cCA+IHVsID4gbGkgPiBhW2RhdGEtdGFiPVxcXCJcIiArIHRhYklkICsgXCJcXFwiXTpub3QoW2hpZGRlbl0pXCIpO1xyXG5cclxuICAgICAgICAgICAgaWYgKGEgPT09IG51bGwpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICAgICAgfVxyXG5cclxuICAgICAgICAgICAgYS5kaXNwYXRjaEV2ZW50KG5ldyBDdXN0b21FdmVudCgnY2xpY2snLCB7YnViYmxlczogdHJ1ZX0pKTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gcmVhZFRhYnNRdWVyeVN0cmluZ1BhcmFtKCkge1xyXG4gICAgICAgIGNvbnN0IHFzID0gcGFyc2VRdWVyeVN0cmluZygpO1xyXG4gICAgICAgIGNvbnN0IHQgPSBxcy50YWJzO1xyXG5cclxuICAgICAgICBpZiAodCA9PT0gdW5kZWZpbmVkIHx8IHQgPT09ICcnKSB7XHJcbiAgICAgICAgICAgIHJldHVybiBbXTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiB0LnNwbGl0KCcsJyk7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gdXBkYXRlVGFic1F1ZXJ5U3RyaW5nUGFyYW0oc3RhdGUpIHtcclxuICAgICAgICBjb25zdCBxcyA9IHBhcnNlUXVlcnlTdHJpbmcoKTtcclxuICAgICAgICBxcy50YWJzID0gc3RhdGUuc2VsZWN0ZWRUYWJzLmpvaW4oKTtcclxuXHJcbiAgICAgICAgY29uc3QgdXJsID0gbG9jYXRpb24ucHJvdG9jb2wgKyBcIi8vXCIgKyBsb2NhdGlvbi5ob3N0ICsgbG9jYXRpb24ucGF0aG5hbWUgKyBcIj9cIiArIHRvUXVlcnlTdHJpbmcocXMpICsgbG9jYXRpb24uaGFzaDtcclxuXHJcbiAgICAgICAgaWYgKGxvY2F0aW9uLmhyZWYgPT09IHVybCkge1xyXG4gICAgICAgICAgICByZXR1cm47XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBoaXN0b3J5LnJlcGxhY2VTdGF0ZSh7fSwgZG9jdW1lbnQudGl0bGUsIHVybCk7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gdG9RdWVyeVN0cmluZyhhcmdzKSB7XHJcbiAgICAgICAgY29uc3QgcGFydHMgPSBbXTtcclxuXHJcbiAgICAgICAgZm9yIChsZXQgbmFtZV8xIGluIGFyZ3MpIHtcclxuICAgICAgICAgICAgaWYgKGFyZ3MuaGFzT3duUHJvcGVydHkobmFtZV8xKSAmJiBhcmdzW25hbWVfMV0gIT09ICcnICYmIGFyZ3NbbmFtZV8xXSAhPT0gbnVsbCAmJiBhcmdzW25hbWVfMV0gIT09IHVuZGVmaW5lZCkge1xyXG4gICAgICAgICAgICAgICAgcGFydHMucHVzaChlbmNvZGVVUklDb21wb25lbnQobmFtZV8xKSArICc9JyArIGVuY29kZVVSSUNvbXBvbmVudChhcmdzW25hbWVfMV0pKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgcmV0dXJuIHBhcnRzLmpvaW4oJyYnKTtcclxuICAgIH1cclxuXHJcbiAgICBmdW5jdGlvbiBwYXJzZVF1ZXJ5U3RyaW5nKHF1ZXJ5U3RyaW5nKSB7XHJcbiAgICAgICAgbGV0IG1hdGNoO1xyXG4gICAgICAgIGNvbnN0IHBsID0gL1xcKy9nO1xyXG4gICAgICAgIGNvbnN0IHNlYXJjaCA9IC8oW14mPV0rKT0/KFteJl0qKS9nO1xyXG5cclxuICAgICAgICBjb25zdCBkZWNvZGUgPSBmdW5jdGlvbiAocykge1xyXG4gICAgICAgICAgICByZXR1cm4gZGVjb2RlVVJJQ29tcG9uZW50KHMucmVwbGFjZShwbCwgJyAnKSk7XHJcbiAgICAgICAgfTtcclxuXHJcbiAgICAgICAgaWYgKHF1ZXJ5U3RyaW5nID09PSB1bmRlZmluZWQpIHtcclxuICAgICAgICAgICAgcXVlcnlTdHJpbmcgPSAnJztcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHF1ZXJ5U3RyaW5nID0gcXVlcnlTdHJpbmcuc3Vic3RyaW5nKDEpO1xyXG4gICAgICAgIGNvbnN0IHVybFBhcmFtcyA9IHt9O1xyXG5cclxuICAgICAgICB3aGlsZSAobWF0Y2ggPSBzZWFyY2guZXhlYyhxdWVyeVN0cmluZykpIHtcclxuICAgICAgICAgICAgdXJsUGFyYW1zW2RlY29kZShtYXRjaFsxXSldID0gZGVjb2RlKG1hdGNoWzJdKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiB1cmxQYXJhbXM7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gYXJyYXlzSW50ZXJzZWN0KGEsIGIpIHtcclxuICAgICAgICBmb3IgKGxldCBfaSA9IDAsIGFfMSA9IGE7IF9pIDwgYV8xLmxlbmd0aDsgX2krKykge1xyXG4gICAgICAgICAgICBjb25zdCBpdGVtQSA9IGFfMVtfaV07XHJcblxyXG4gICAgICAgICAgICBmb3IgKGxldCBfYSA9IDAsIGJfMSA9IGI7IF9hIDwgYl8xLmxlbmd0aDsgX2ErKykge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgaXRlbUIgPSBiXzFbX2FdO1xyXG4gICAgICAgICAgICAgICAgaWYgKGl0ZW1BID09PSBpdGVtQikge1xyXG4gICAgICAgICAgICAgICAgICAgIHJldHVybiB0cnVlO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICByZXR1cm4gZmFsc2U7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gbm90aWZ5Q29udGVudFVwZGF0ZWQoKSB7XHJcbiAgICAgICAgLy8gRGlzcGF0Y2ggdGhpcyBldmVudCB3aGVuIG5lZWRlZFxyXG4gICAgICAgIC8vIHdpbmRvdy5kaXNwYXRjaEV2ZW50KG5ldyBDdXN0b21FdmVudCgnY29udGVudC11cGRhdGUnKSk7XHJcbiAgICB9XHJcbn1cclxuIiwiLyoqXHJcbiAqIFN0eWxpbmcgZm9yIHRhYmxlcyBpbiBjb25jZXB0dWFsIGRvY3VtZW50cyB1c2luZyBCb290c3RyYXAuXHJcbiAqIFNlZSBodHRwOi8vZ2V0Ym9vdHN0cmFwLmNvbS9jc3MvI3RhYmxlc1xyXG4gKi9cclxuZnVuY3Rpb24gcmVuZGVyVGFibGVzKCkge1xyXG4gICAgJCgndGFibGUnKS5hZGRDbGFzcygndGFibGUgdGFibGUtYm9yZGVyZWQgdGFibGUtc3RyaXBlZCB0YWJsZS1jb25kZW5zZWQnKS53cmFwKCc8ZGl2IGNsYXNzPVxcXCJ0YWJsZS1yZXNwb25zaXZlXFxcIj48L2Rpdj4nKTtcclxufVxyXG4iLCJ3aW5kb3cucmVmcmVzaCA9IGZ1bmN0aW9uIChhcnRpY2xlKSB7XHJcblxyXG4gICAgLy8gVXBkYXRlIG1hcmt1cCByZXN1bHRcclxuICAgIGlmICh0eXBlb2YgYXJ0aWNsZSA9PSAndW5kZWZpbmVkJyB8fCB0eXBlb2YgYXJ0aWNsZS5jb250ZW50ID09ICd1bmRlZmluZWQnKSB7XHJcbiAgICAgICAgY29uc29sZS5lcnJvcihcIk51bGwgQXJndW1lbnRcIik7XHJcbiAgICB9XHJcblxyXG4gICAgJChcImFydGljbGUuY29udGVudFwiKS5odG1sKGFydGljbGUuY29udGVudCk7XHJcblxyXG4gICAgaGlnaGxpZ2h0KCk7XHJcbiAgICByZW5kZXJUYWJsZXMoKTtcclxuICAgIHJlbmRlckFsZXJ0cygpO1xyXG4gICAgcmVuZGVyQWZmaXgoKTtcclxuICAgIHJlbmRlclRhYnMoKTtcclxufVxyXG5cclxuJChkb2N1bWVudCkub24oJ3dvcmRwcmVzc01lbnVIYXNMb2FkZWQnLCBmdW5jdGlvbiAoKSB7XHJcbiAgICBjb25zdCBwYXRoID0gd2luZG93LmxvY2F0aW9uLnBhdGhuYW1lO1xyXG4gICAgY29uc3QgZG9jc1VybCA9ICcvZG9jcy9hcnRpY2xlcy8nO1xyXG4gICAgY29uc3Qgd3BOYXZCYXIgPSBkb2N1bWVudC5nZXRFbGVtZW50QnlJZCgnbWVudS1tZW51LXByaW5jaXBhbCcpO1xyXG4gICAgY29uc3QgaXRlbXMgPSB3cE5hdkJhci5nZXRFbGVtZW50c0J5VGFnTmFtZSgnYScpO1xyXG5cclxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgaXRlbXMubGVuZ3RoOyBpKyspIHtcclxuXHJcbiAgICAgICAgaWYgKGl0ZW1zW2ldLmhyZWYuaW5jbHVkZXMoZG9jc1VybCkgJiYgcGF0aC5pbmNsdWRlcyhkb2NzVXJsKSAmJiAhaXRlbXNbaV0uaHJlZi5pbmNsdWRlcygnIycpKSB7XHJcbiAgICAgICAgICAgICQoaXRlbXNbaV0pLmFkZENsYXNzKCdhY3RpdmVwYXRoJyk7XHJcbiAgICAgICAgfVxyXG4gICAgfVxyXG5cclxuICAgIGNvbnN0IHF1ZXJ5U3RyaW5nID0gd2luZG93LmxvY2F0aW9uLnNlYXJjaDtcclxuXHJcbiAgICBpZiAocXVlcnlTdHJpbmcpIHtcclxuICAgICAgICBjb25zdCBxdWVyeVN0cmluZ0NvbXBvbmVudHMgPSBxdWVyeVN0cmluZy5zcGxpdCgnPScpO1xyXG4gICAgICAgIGNvbnN0IHNlYXJjaFBhcmFtID0gcXVlcnlTdHJpbmdDb21wb25lbnRzLnNsaWNlKC0xKVswXTtcclxuICAgICAgICAkKCcjc2VhcmNoLXF1ZXJ5JykudmFsKGRlY29kZVVSSShzZWFyY2hQYXJhbSkpO1xyXG4gICAgfVxyXG5cclxufSk7XHJcblxyXG5cclxuLy8gRW5hYmxlIGFuY2hvcnMgZm9yIGhlYWRpbmdzLlxyXG4oZnVuY3Rpb24gKCkge1xyXG4gICAgYW5jaG9ycy5vcHRpb25zID0ge1xyXG4gICAgICAgIHBsYWNlbWVudDogJ3JpZ2h0JyxcclxuICAgICAgICB2aXNpYmxlOiAnaG92ZXInLFxyXG4gICAgICAgIGljb246ICcjJ1xyXG4gICAgfTtcclxuICAgIGFuY2hvcnMuYWRkKCdhcnRpY2xlIGgyOm5vdCgubm8tYW5jaG9yKSwgYXJ0aWNsZSBoMzpub3QoLm5vLWFuY2hvciksIGFydGljbGUgaDQ6bm90KC5uby1hbmNob3IpJyk7XHJcbn0pKCk7XHJcbiIsIi8vIEVuYWJsZSBoaWdobGlnaHQuanNcclxuZnVuY3Rpb24gaGlnaGxpZ2h0KCkge1xyXG5cclxuICAgICQoJ3ByZSBjb2RlJykuZWFjaChmdW5jdGlvbiAoaSwgYmxvY2spIHtcclxuICAgICAgICBobGpzLmhpZ2hsaWdodEJsb2NrKGJsb2NrKTtcclxuICAgIH0pO1xyXG5cclxuICAgICQoJ3ByZSBjb2RlW2hpZ2hsaWdodC1saW5lc10nKS5lYWNoKGZ1bmN0aW9uIChpLCBibG9jaykge1xyXG4gICAgICAgIGlmIChibG9jay5pbm5lckhUTUwgPT09IFwiXCIpIHJldHVybjtcclxuICAgICAgICBjb25zdCBsaW5lcyA9IGJsb2NrLmlubmVySFRNTC5zcGxpdCgnXFxuJyk7XHJcblxyXG4gICAgICAgIGNvbnN0IHF1ZXJ5U3RyaW5nID0gYmxvY2suZ2V0QXR0cmlidXRlKCdoaWdobGlnaHQtbGluZXMnKTtcclxuICAgICAgICBpZiAoIXF1ZXJ5U3RyaW5nKSByZXR1cm47XHJcblxyXG4gICAgICAgIGxldCByYW5nZXNTdHJpbmcgPSBxdWVyeVN0cmluZy5zcGxpdCgnLCcpO1xyXG4gICAgICAgIGxldCByYW5nZXMgPSByYW5nZXNTdHJpbmcubWFwKE51bWJlcik7XHJcblxyXG4gICAgICAgIGZvciAobGV0IHJhbmdlIG9mIHJhbmdlcykge1xyXG4gICAgICAgICAgICBjb25zdCBmb3VuZCA9IHJhbmdlLm1hdGNoKC9eKFxcZCspXFwtKFxcZCspPyQvKTtcclxuICAgICAgICAgICAgbGV0IHN0YXJ0ID0gMDtcclxuICAgICAgICAgICAgbGV0IGVuZCA9IDA7XHJcbiAgICAgICAgICAgIGlmIChmb3VuZCkge1xyXG4gICAgICAgICAgICAgICAgLy8gY29uc2lkZXIgcmVnaW9uIGFzIGB7c3RhcnRsaW5lbnVtYmVyfS17ZW5kbGluZW51bWJlcn1gLCBpbiB3aGljaCB7ZW5kbGluZW51bWJlcn0gaXMgb3B0aW9uYWxcclxuICAgICAgICAgICAgICAgIHN0YXJ0ID0gK2ZvdW5kWzFdO1xyXG4gICAgICAgICAgICAgICAgZW5kID0gK2ZvdW5kWzJdO1xyXG4gICAgICAgICAgICAgICAgaWYgKGlzTmFOKGVuZCkgfHwgZW5kID4gbGluZXMubGVuZ3RoKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgZW5kID0gbGluZXMubGVuZ3RoO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgLy8gY29uc2lkZXIgcmVnaW9uIGFzIGEgc2lnaW5lIGxpbmUgbnVtYmVyXHJcbiAgICAgICAgICAgICAgICBpZiAoaXNOYU4ocmFuZ2UpKSBjb250aW51ZTtcclxuICAgICAgICAgICAgICAgIHN0YXJ0ID0gK3JhbmdlO1xyXG4gICAgICAgICAgICAgICAgZW5kID0gc3RhcnQ7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgaWYgKHN0YXJ0IDw9IDAgfHwgZW5kIDw9IDAgfHwgc3RhcnQgPiBlbmQgfHwgc3RhcnQgPiBsaW5lcy5sZW5ndGgpIHtcclxuICAgICAgICAgICAgICAgIC8vIHNraXAgY3VycmVudCByZWdpb24gaWYgaW52YWxpZFxyXG4gICAgICAgICAgICAgICAgY29udGludWU7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgbGluZXNbc3RhcnQgLSAxXSA9ICc8c3BhbiBjbGFzcz1cImxpbmUtaGlnaGxpZ2h0XCI+JyArIGxpbmVzW3N0YXJ0IC0gMV07XHJcbiAgICAgICAgICAgIGxpbmVzW2VuZCAtIDFdID0gbGluZXNbZW5kIC0gMV0gKyAnPC9zcGFuPic7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBibG9jay5pbm5lckhUTUwgPSBsaW5lcy5qb2luKCdcXG4nKTtcclxuICAgIH0pO1xyXG59XHJcblxyXG4vLyBTdXBwb3J0IGZ1bGwtdGV4dC1zZWFyY2hcclxuZnVuY3Rpb24gZW5hYmxlU2VhcmNoKCkge1xyXG4gICAgbGV0IHF1ZXJ5O1xyXG4gICAgY29uc3QgcmVsSHJlZiA9ICQoXCJtZXRhW3Byb3BlcnR5PSdkb2NmeFxcXFw6cmVsJ11cIikuYXR0cihcImNvbnRlbnRcIik7XHJcblxyXG4gICAgaWYgKHR5cGVvZiByZWxIcmVmID09PSAndW5kZWZpbmVkJykge1xyXG4gICAgICAgIHJldHVybjtcclxuICAgIH1cclxuXHJcbiAgICB0cnkge1xyXG4gICAgICAgIGNvbnN0IHdvcmtlciA9IG5ldyBXb3JrZXIocmVsSHJlZiArICdzdHlsZXMvc2VhcmNoLXdvcmtlci5qcycpO1xyXG4gICAgICAgIGlmICghd29ya2VyICYmICF3aW5kb3cud29ya2VyKSB7XHJcbiAgICAgICAgICAgIGxvY2FsU2VhcmNoKCk7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgd2ViV29ya2VyU2VhcmNoKHdvcmtlcik7XHJcbiAgICAgICAgfVxyXG4gICAgICAgIHJlbmRlclNlYXJjaEJveCgpO1xyXG4gICAgICAgIGhpZ2hsaWdodEtleXdvcmRzKCk7XHJcbiAgICAgICAgYWRkU2VhcmNoRXZlbnQoKTtcclxuICAgIH0gY2F0Y2ggKGUpIHtcclxuICAgICAgICBjb25zb2xlLmVycm9yKGUpO1xyXG4gICAgfVxyXG5cclxuICAgIC8vQWRqdXN0IHRoZSBwb3NpdGlvbiBvZiBzZWFyY2ggYm94IGluIG5hdmJhclxyXG4gICAgZnVuY3Rpb24gcmVuZGVyU2VhcmNoQm94KCkge1xyXG4gICAgICAgIGF1dG9Db2xsYXBzZSgpO1xyXG5cclxuICAgICAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZScsICgpID0+IGF1dG9Db2xsYXBzZSgpKTtcclxuXHJcbiAgICAgICAgJChkb2N1bWVudCkub24oJ2NsaWNrJywgJy5uYXZiYXItY29sbGFwc2UuaW4nLCBmdW5jdGlvbiAoZSkge1xyXG4gICAgICAgICAgICBpZiAoJChlLnRhcmdldCkuaXMoJ2EnKSkge1xyXG4gICAgICAgICAgICAgICAgJCh0aGlzKS5jb2xsYXBzZShoaWRlKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH0pO1xyXG5cclxuICAgICAgICBmdW5jdGlvbiBhdXRvQ29sbGFwc2UoKSB7XHJcbiAgICAgICAgICAgIGNvbnN0IG5hdmJhciA9ICQoJyNhdXRvY29sbGFwc2UnKTtcclxuICAgICAgICAgICAgaWYgKG5hdmJhci5oZWlnaHQoKSA9PT0gbnVsbCkge1xyXG4gICAgICAgICAgICAgICAgc2V0VGltZW91dChhdXRvQ29sbGFwc2UsIDMwMCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgbmF2YmFyLnJlbW92ZUNsYXNzKGNvbGxhcHNlZCk7XHJcbiAgICAgICAgICAgIGlmIChuYXZiYXIuaGVpZ2h0KCkgPiA2MCkge1xyXG4gICAgICAgICAgICAgICAgbmF2YmFyLmFkZENsYXNzKGNvbGxhcHNlZCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgLy8gU2VhcmNoIGZhY3RvcnlcclxuICAgIGZ1bmN0aW9uIGxvY2FsU2VhcmNoKCkge1xyXG4gICAgICAgIGNvbnN0IGx1bnJJbmRleCA9IGx1bnIoZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICAgICB0aGlzLnJlZignaHJlZicpO1xyXG4gICAgICAgICAgICB0aGlzLmZpZWxkKCd0aXRsZScsIHtib29zdDogNTB9KTtcclxuICAgICAgICAgICAgdGhpcy5maWVsZCgna2V5d29yZHMnLCB7Ym9vc3Q6IDIwfSk7XHJcbiAgICAgICAgfSk7XHJcbiAgICAgICAgbHVuci50b2tlbml6ZXIuc2VwZXJhdG9yID0gL1tcXHNcXC1cXC5dKy87XHJcbiAgICAgICAgbGV0IHNlYXJjaERhdGEgPSB7fTtcclxuICAgICAgICBjb25zdCBzZWFyY2hEYXRhUmVxdWVzdCA9IG5ldyBYTUxIdHRwUmVxdWVzdCgpO1xyXG5cclxuICAgICAgICBjb25zdCBpbmRleFBhdGggPSByZWxIcmVmICsgXCJpbmRleC5qc29uXCI7XHJcbiAgICAgICAgaWYgKGluZGV4UGF0aCkge1xyXG4gICAgICAgICAgICBzZWFyY2hEYXRhUmVxdWVzdC5vcGVuKCdHRVQnLCBpbmRleFBhdGgpO1xyXG4gICAgICAgICAgICBzZWFyY2hEYXRhUmVxdWVzdC5vbmxvYWQgPSBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgICAgICBpZiAodGhpcy5zdGF0dXMgIT09IDIwMCkge1xyXG4gICAgICAgICAgICAgICAgICAgIHJldHVybjtcclxuICAgICAgICAgICAgICAgIH1cclxuICAgICAgICAgICAgICAgIHNlYXJjaERhdGEgPSBKU09OLnBhcnNlKHRoaXMucmVzcG9uc2VUZXh0KTtcclxuICAgICAgICAgICAgICAgIGZvciAobGV0IHByb3AgaW4gc2VhcmNoRGF0YSkge1xyXG4gICAgICAgICAgICAgICAgICAgIGlmIChzZWFyY2hEYXRhLmhhc093blByb3BlcnR5KHByb3ApKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgICAgIGx1bnJJbmRleC5hZGQoc2VhcmNoRGF0YVtwcm9wXSk7XHJcbiAgICAgICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIHNlYXJjaERhdGFSZXF1ZXN0LnNlbmQoKTtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgICQoXCJib2R5XCIpLm9uKFwicXVlcnlSZWFkeVwiLCBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgIGNvbnN0IGhpdHMgPSBsdW5ySW5kZXguc2VhcmNoKHF1ZXJ5KTtcclxuICAgICAgICAgICAgY29uc3QgcmVzdWx0cyA9IFtdO1xyXG4gICAgICAgICAgICBoaXRzLmZvckVhY2goZnVuY3Rpb24gKGhpdCkge1xyXG4gICAgICAgICAgICAgICAgY29uc3QgaXRlbSA9IHNlYXJjaERhdGFbaGl0LnJlZl07XHJcbiAgICAgICAgICAgICAgICByZXN1bHRzLnB1c2goeydocmVmJzogaXRlbS5ocmVmLCAndGl0bGUnOiBpdGVtLnRpdGxlLCAna2V5d29yZHMnOiBpdGVtLmtleXdvcmRzfSk7XHJcbiAgICAgICAgICAgIH0pO1xyXG4gICAgICAgICAgICBoYW5kbGVTZWFyY2hSZXN1bHRzKHJlc3VsdHMpO1xyXG4gICAgICAgIH0pO1xyXG4gICAgfVxyXG5cclxuICAgIGZ1bmN0aW9uIHdlYldvcmtlclNlYXJjaCh3b3JrZXIpIHtcclxuICAgICAgICBjb25zdCBpbmRleFJlYWR5ID0gJC5EZWZlcnJlZCgpO1xyXG4gICAgICAgIHdvcmtlci5vbm1lc3NhZ2UgPSBmdW5jdGlvbiAob0V2ZW50KSB7XHJcbiAgICAgICAgICAgIHN3aXRjaCAob0V2ZW50LmRhdGEuZSkge1xyXG4gICAgICAgICAgICAgICAgY2FzZSAnaW5kZXgtcmVhZHknOlxyXG4gICAgICAgICAgICAgICAgICAgIGluZGV4UmVhZHkucmVzb2x2ZSgpO1xyXG4gICAgICAgICAgICAgICAgICAgIGJyZWFrO1xyXG4gICAgICAgICAgICAgICAgY2FzZSAncXVlcnktcmVhZHknOlxyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGhpdHMgPSBvRXZlbnQuZGF0YS5kO1xyXG4gICAgICAgICAgICAgICAgICAgIGhhbmRsZVNlYXJjaFJlc3VsdHMoaGl0cyk7XHJcbiAgICAgICAgICAgICAgICAgICAgYnJlYWs7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGluZGV4UmVhZHkucHJvbWlzZSgpLmRvbmUoZnVuY3Rpb24gKCkge1xyXG5cclxuICAgICAgICAgICAgJChcImJvZHlcIikub24oXCJxdWVyeS1yZWFkeVwiLCBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgICAgICBwb3N0U2VhcmNoUXVlcnkod29ya2VyLCBxdWVyeSk7XHJcbiAgICAgICAgICAgIH0pO1xyXG5cclxuICAgICAgICAgICAgcG9zdFNlYXJjaFF1ZXJ5KHdvcmtlciwgcXVlcnkpO1xyXG5cclxuICAgICAgICB9KTtcclxuICAgIH1cclxuXHJcbiAgICAvKipcclxuICAgICAqIFRoaXMgZnVuY3Rpb24gcG9zdHMgdGhlIG1lc3NhZ2UgdG8gdGhlIHdvcmtlciBpZiB0aGUgc3RyaW5nIGhhcyBhdCBsZWFzdFxyXG4gICAgICogdGhyZWUgY2hhcmFjdGVycy5cclxuICAgICAqXHJcbiAgICAgKiBAcGFyYW0gd29ya2VyIFRoZSBzZWFyY2ggd29ya2VyIHVzZWQgYnkgRG9jRnggKGx1bnIpXHJcbiAgICAgKiBAcGFyYW0gc2VhcmNoUXVlcnkgVGhlIHN0cmluZyB0byBwb3N0IHRvIHRoZSB3b3JrZXIuXHJcbiAgICAgKi9cclxuICAgIGZ1bmN0aW9uIHBvc3RTZWFyY2hRdWVyeSh3b3JrZXIsIHNlYXJjaFF1ZXJ5KSB7XHJcbiAgICAgICAgaWYgKHNlYXJjaFF1ZXJ5ICYmIChzZWFyY2hRdWVyeS5sZW5ndGggPj0gMykpIHtcclxuICAgICAgICAgICAgd29ya2VyLnBvc3RNZXNzYWdlKHtxOiBgJHtzZWFyY2hRdWVyeX0qYH0pO1xyXG4gICAgICAgIH0gZWxzZSB7XHJcbiAgICAgICAgICAgIHdvcmtlci5wb3N0TWVzc2FnZSh7cTogJyd9KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgLyoqXHJcbiAgICAgKiAgIEhpZ2hsaWdodCB0aGUgc2VhcmNoaW5nIGtleXdvcmRzXHJcbiAgICAgKi9cclxuICAgIGZ1bmN0aW9uIGhpZ2hsaWdodEtleXdvcmRzKCkge1xyXG4gICAgICAgIGNvbnN0IHEgPSB1cmwoJz9xJyk7XHJcbiAgICAgICAgaWYgKHEgIT0gbnVsbCkge1xyXG4gICAgICAgICAgICBjb25zdCBrZXl3b3JkcyA9IHEuc3BsaXQoXCIlMjBcIik7XHJcbiAgICAgICAgICAgIGtleXdvcmRzLmZvckVhY2goZnVuY3Rpb24gKGtleXdvcmQpIHtcclxuICAgICAgICAgICAgICAgIGlmIChrZXl3b3JkICE9PSBcIlwiKSB7XHJcbiAgICAgICAgICAgICAgICAgICAgJCgnLmRhdGEtc2VhcmNoYWJsZSAqJykubWFyayhrZXl3b3JkKTtcclxuICAgICAgICAgICAgICAgICAgICAkKCdhcnRpY2xlIConKS5tYXJrKGtleXdvcmQpO1xyXG4gICAgICAgICAgICAgICAgfVxyXG4gICAgICAgICAgICB9KTtcclxuICAgICAgICB9XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gYWRkU2VhcmNoRXZlbnQoKSB7XHJcbiAgICAgICAgJCgnYm9keScpLm9uKFwic2VhcmNoRXZlbnRcIiwgZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICAgICAkKCcjc2VhcmNoLXJlc3VsdHM+LnNyLWl0ZW1zJykuaHRtbCgnPHA+Tm8gcmVzdWx0cyBmb3VuZDwvcD4nKTtcclxuXHJcbiAgICAgICAgICAgIGNvbnN0IHNlYXJjaFF1ZXJ5ID0gJCgnI3NlYXJjaC1xdWVyeScpO1xyXG5cclxuICAgICAgICAgICAgc2VhcmNoUXVlcnkub24oJ2lucHV0JywgZnVuY3Rpb24gKGUpIHtcclxuICAgICAgICAgICAgICAgIHJldHVybiBlLmtleSAhPT0gJ0VudGVyJztcclxuICAgICAgICAgICAgfSk7XHJcblxyXG4gICAgICAgICAgICBzZWFyY2hRdWVyeS5vbihcImtleXVwXCIsIGZ1bmN0aW9uIChlKSB7XHJcbiAgICAgICAgICAgICAgICAkKCcjc2VhcmNoLXJlc3VsdHMnKS5zaG93KCk7XHJcbiAgICAgICAgICAgICAgICBxdWVyeSA9IGAke2UudGFyZ2V0LnZhbHVlfWA7XHJcbiAgICAgICAgICAgICAgICAkKFwiYm9keVwiKS50cmlnZ2VyKFwicXVlcnktcmVhZHlcIik7XHJcbiAgICAgICAgICAgICAgICAkKCcjc2VhcmNoLXJlc3VsdHM+LnNlYXJjaC1saXN0JykudGV4dCgnU2VhcmNoIFJlc3VsdHMgZm9yIFwiJyArIHF1ZXJ5ICsgJ1wiJyk7XHJcbiAgICAgICAgICAgIH0pLm9mZihcImtleWRvd25cIik7XHJcbiAgICAgICAgfSk7XHJcbiAgICB9XHJcblxyXG4gICAgZnVuY3Rpb24gcmVsYXRpdmVVcmxUb0Fic29sdXRlVXJsKGN1cnJlbnRVcmwsIHJlbGF0aXZlVXJsKSB7XHJcbiAgICAgICAgY29uc3QgY3VycmVudEl0ZW1zID0gY3VycmVudFVybC5zcGxpdCgvXFwvKy8pO1xyXG4gICAgICAgIGNvbnN0IHJlbGF0aXZlSXRlbXMgPSByZWxhdGl2ZVVybC5zcGxpdCgvXFwvKy8pO1xyXG4gICAgICAgIGxldCBkZXB0aCA9IGN1cnJlbnRJdGVtcy5sZW5ndGggLSAxO1xyXG4gICAgICAgIGNvbnN0IGl0ZW1zID0gW107XHJcbiAgICAgICAgZm9yIChsZXQgaSA9IDA7IGkgPCByZWxhdGl2ZUl0ZW1zLmxlbmd0aDsgaSsrKSB7XHJcbiAgICAgICAgICAgIGlmIChyZWxhdGl2ZUl0ZW1zW2ldID09PSAnLi4nKSB7XHJcbiAgICAgICAgICAgICAgICBkZXB0aC0tO1xyXG4gICAgICAgICAgICB9IGVsc2UgaWYgKHJlbGF0aXZlSXRlbXNbaV0gIT09ICcuJykge1xyXG4gICAgICAgICAgICAgICAgaXRlbXMucHVzaChyZWxhdGl2ZUl0ZW1zW2ldKTtcclxuICAgICAgICAgICAgfVxyXG4gICAgICAgIH1cclxuICAgICAgICByZXR1cm4gY3VycmVudEl0ZW1zLnNsaWNlKDAsIGRlcHRoKS5jb25jYXQoaXRlbXMpLmpvaW4oJy8nKTtcclxuICAgIH1cclxuXHJcbiAgICBmdW5jdGlvbiBleHRyYWN0Q29udGVudEJyaWVmKGNvbnRlbnQpIHtcclxuICAgICAgICBjb25zdCBicmllZk9mZnNldCA9IDUwO1xyXG4gICAgICAgIGNvbnN0IHdvcmRzID0gcXVlcnkuc3BsaXQoL1xccysvZyk7XHJcbiAgICAgICAgY29uc3QgcXVlcnlJbmRleCA9IGNvbnRlbnQuaW5kZXhPZih3b3Jkc1swXSk7XHJcblxyXG4gICAgICAgIGlmIChxdWVyeUluZGV4ID4gYnJpZWZPZmZzZXQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIFwiLi4uXCIgKyBjb250ZW50LnNsaWNlKHF1ZXJ5SW5kZXggLSBicmllZk9mZnNldCwgcXVlcnlJbmRleCArIGJyaWVmT2Zmc2V0KSArIFwiLi4uXCI7XHJcbiAgICAgICAgfSBlbHNlIGlmIChxdWVyeUluZGV4IDw9IGJyaWVmT2Zmc2V0KSB7XHJcbiAgICAgICAgICAgIHJldHVybiBjb250ZW50LnNsaWNlKDAsIHF1ZXJ5SW5kZXggKyBicmllZk9mZnNldCkgKyBcIi4uLlwiO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxuXHJcbiAgICBmdW5jdGlvbiBoYW5kbGVTZWFyY2hSZXN1bHRzKGhpdHMpIHtcclxuICAgICAgICBpZiAoaGl0cy5sZW5ndGggPT09IDApIHtcclxuICAgICAgICAgICAgJCgnI3NlYXJjaC1yZXN1bHRzPi5zci1pdGVtcycpLmh0bWwoJzxwPk5vIHJlc3VsdHMgZm91bmQ8L3A+Jyk7XHJcbiAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgJCgnI3NlYXJjaC1yZXN1bHRzPi5zci1pdGVtcycpLmVtcHR5KCkuYXBwZW5kKFxyXG4gICAgICAgICAgICAgICAgaGl0cy5zbGljZSgwLCAyMCkubWFwKGZ1bmN0aW9uIChoaXQpIHtcclxuICAgICAgICAgICAgICAgICAgICBjb25zdCBjdXJyZW50VXJsID0gd2luZG93LmxvY2F0aW9uLmhyZWY7XHJcblxyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGl0ZW1SYXdIcmVmID0gcmVsYXRpdmVVcmxUb0Fic29sdXRlVXJsKGN1cnJlbnRVcmwsIHJlbEhyZWYgKyBoaXQuaHJlZik7XHJcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgaXRlbUhyZWYgPSByZWxIcmVmICsgaGl0LmhyZWYgKyBcIj9xPVwiICsgcXVlcnk7XHJcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgaXRlbVRpdGxlID0gaGl0LnRpdGxlO1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGl0ZW1CcmllZiA9IGV4dHJhY3RDb250ZW50QnJpZWYoaGl0LmtleXdvcmRzKTtcclxuXHJcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgaXRlbU5vZGUgPSAkKCc8YT4nKS5hdHRyKCdjbGFzcycsICdzci1pdGVtJykuYXR0cignaHJlZicsIGl0ZW1IcmVmKTtcclxuICAgICAgICAgICAgICAgICAgICBjb25zdCBpdGVtVGl0bGVOb2RlID0gJCgnPGRpdj4nKS5hdHRyKCdjbGFzcycsICdpdGVtLXRpdGxlJykudGV4dChpdGVtVGl0bGUpO1xyXG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGl0ZW1CcmllZk5vZGUgPSAkKCc8ZGl2PicpLmF0dHIoJ2NsYXNzJywgJ2l0ZW0tYnJpZWYnKS50ZXh0KGl0ZW1CcmllZik7XHJcbiAgICAgICAgICAgICAgICAgICAgaXRlbU5vZGUuYXBwZW5kKGl0ZW1UaXRsZU5vZGUpLmFwcGVuZChpdGVtQnJpZWZOb2RlKTtcclxuXHJcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIGl0ZW1Ob2RlO1xyXG4gICAgICAgICAgICAgICAgfSlcclxuICAgICAgICAgICAgKTtcclxuICAgICAgICAgICAgcXVlcnkuc3BsaXQoL1xccysvKS5mb3JFYWNoKGZ1bmN0aW9uICh3b3JkKSB7XHJcbiAgICAgICAgICAgICAgICBpZiAod29yZCAhPT0gJycpIHtcclxuICAgICAgICAgICAgICAgICAgICB3b3JkID0gd29yZC5yZXBsYWNlKC9cXCovZywgJycpO1xyXG4gICAgICAgICAgICAgICAgICAgICQoJyNzZWFyY2gtcmVzdWx0cz4uc3ItaXRlbXMgKicpLm1hcmsod29yZCk7XHJcbiAgICAgICAgICAgICAgICB9XHJcbiAgICAgICAgICAgIH0pO1xyXG4gICAgICAgIH1cclxuICAgIH1cclxufVxyXG4iLCJmdW5jdGlvbiBnZXRBYnNvbHV0ZVBhdGgoaHJlZikge1xyXG4gICAgLy8gVXNlIGFuY2hvciB0byBub3JtYWxpemUgaHJlZlxyXG4gICAgY29uc3QgYW5jaG9yID0gJCgnPGEgaHJlZj1cIicgKyBocmVmICsgJ1wiPjwvYT4nKVswXTtcclxuICAgIC8vIElnbm9yZSBwcm90b2NhbCwgcmVtb3ZlIHNlYXJjaCBhbmQgcXVlcnlcclxuICAgIHJldHVybiBhbmNob3IuaG9zdCArIGFuY2hvci5wYXRobmFtZTtcclxufVxyXG5cclxuZnVuY3Rpb24gaXNSZWxhdGl2ZVBhdGgoaHJlZikge1xyXG4gICAgaWYgKGhyZWYgPT09IHVuZGVmaW5lZCB8fCBocmVmID09PSAnJyB8fCBocmVmWzBdID09PSAnLycpIHtcclxuICAgICAgICByZXR1cm4gZmFsc2U7XHJcbiAgICB9XHJcbiAgICByZXR1cm4gIWlzQWJzb2x1dGVQYXRoKGhyZWYpO1xyXG59XHJcblxyXG5mdW5jdGlvbiBpc0Fic29sdXRlUGF0aChocmVmKSB7XHJcbiAgICByZXR1cm4gKC9eKD86W2Etel0rOik/XFwvXFwvL2kpLnRlc3QoaHJlZik7XHJcbn1cclxuXHJcbmZ1bmN0aW9uIGdldERpcmVjdG9yeShocmVmKSB7XHJcbiAgICBpZiAoIWhyZWYpIHtcclxuICAgICAgICByZXR1cm4gJyc7XHJcbiAgICB9XHJcblxyXG4gICAgY29uc3QgaW5kZXggPSBocmVmLmxhc3RJbmRleE9mKCcvJyk7XHJcblxyXG4gICAgaWYgKGluZGV4ID09PSAtMSkge1xyXG4gICAgICAgIHJldHVybiAnJztcclxuICAgIH1cclxuXHJcbiAgICBpZiAoaW5kZXggPiAtMSkge1xyXG4gICAgICAgIHJldHVybiBocmVmLnN1YnN0cigwLCBpbmRleCk7XHJcbiAgICB9XHJcbn1cclxuXHJcbmZ1bmN0aW9uIGZvcm1MaXN0KGl0ZW0sIGNsYXNzZXMpIHtcclxuICAgIGxldCBsZXZlbCA9IDE7XHJcbiAgICBjb25zdCBtb2RlbCA9IHtcclxuICAgICAgICBpdGVtczogaXRlbVxyXG4gICAgfTtcclxuXHJcbiAgICBjb25zdCBjbHMgPSBbXS5jb25jYXQoY2xhc3Nlcykuam9pbihcIiBcIik7XHJcbiAgICByZXR1cm4gZ2V0TGlzdChtb2RlbCwgY2xzKTtcclxuXHJcbiAgICBmdW5jdGlvbiBnZXRMaXN0KG1vZGVsLCBjbHMpIHtcclxuXHJcbiAgICAgICAgaWYgKCFtb2RlbCB8fCAhbW9kZWwuaXRlbXMpIHtcclxuICAgICAgICAgICAgcmV0dXJuIG51bGw7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBjb25zdCBsID0gbW9kZWwuaXRlbXMubGVuZ3RoO1xyXG5cclxuICAgICAgICBpZiAobCA9PT0gMCkge1xyXG4gICAgICAgICAgICByZXR1cm4gbnVsbDtcclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIGxldCBodG1sID0gJzx1bCBjbGFzcz1cImxldmVsJyArIGxldmVsICsgJyAnICsgKGNscyB8fCAnJykgKyAnXCI+JztcclxuICAgICAgICBsZXZlbCsrO1xyXG5cclxuICAgICAgICBmb3IgKGxldCBpID0gMDsgaSA8IGw7IGkrKykge1xyXG4gICAgICAgICAgICBjb25zdCBpdGVtID0gbW9kZWwuaXRlbXNbaV07XHJcbiAgICAgICAgICAgIGNvbnN0IGhyZWYgPSBpdGVtLmhyZWY7XHJcbiAgICAgICAgICAgIGNvbnN0IG5hbWUgPSBpdGVtLm5hbWU7XHJcblxyXG4gICAgICAgICAgICBpZiAoIW5hbWUpIHtcclxuICAgICAgICAgICAgICAgIGNvbnRpbnVlO1xyXG4gICAgICAgICAgICB9XHJcblxyXG4gICAgICAgICAgICBodG1sICs9IGhyZWYgPyAnPGxpPjxhIGhyZWY9XCInICsgaHJlZiArICdcIj4nICsgbmFtZSArICc8L2E+JyA6ICc8bGk+JyArIG5hbWU7XHJcbiAgICAgICAgICAgIGh0bWwgKz0gZ2V0TGlzdChpdGVtLCBjbHMpIHx8ICcnO1xyXG4gICAgICAgICAgICBodG1sICs9ICc8L2xpPic7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBodG1sICs9ICc8L3VsPic7XHJcbiAgICAgICAgcmV0dXJuIGh0bWw7XHJcbiAgICB9XHJcbn1cclxuXHJcblxyXG4vKipcclxuICogQWRkIDx3YnI+IGludG8gbG9uZyB3b3JkLlxyXG4gKiBAcGFyYW0ge1N0cmluZ30gdGV4dCAtIFRoZSB3b3JkIHRvIGJyZWFrLiBJdCBzaG91bGQgYmUgaW4gcGxhaW4gdGV4dCB3aXRob3V0IEhUTUwgdGFncy5cclxuICovXHJcbmZ1bmN0aW9uIGJyZWFrUGxhaW5UZXh0KHRleHQpIHtcclxuICAgIGlmICghdGV4dCkgcmV0dXJuIHRleHQ7XHJcbiAgICByZXR1cm4gdGV4dC5yZXBsYWNlKC8oW2Etel0pKFtBLVpdKXwoXFwuKShcXHcpL2csICckMSQzPHdicj4kMiQ0JylcclxufVxyXG5cclxuLyoqXHJcbiAqIEFkZCA8d2JyPiBpbnRvIGxvbmcgd29yZC4gVGhlIGpRdWVyeSBlbGVtZW50IHNob3VsZCBjb250YWluIG5vIGh0bWwgdGFncy5cclxuICogSWYgdGhlIGpRdWVyeSBlbGVtZW50IGNvbnRhaW5zIHRhZ3MsIHRoaXMgZnVuY3Rpb24gd2lsbCBub3QgY2hhbmdlIHRoZSBlbGVtZW50LlxyXG4gKi9cclxuZnVuY3Rpb24gYnJlYWtXb3JkKCkge1xyXG4gICAgaWYgKHRoaXMuaHRtbCgpID09PSB0aGlzLnRleHQoKSkge1xyXG4gICAgICAgIHRoaXMuaHRtbChmdW5jdGlvbiAoaW5kZXgsIHRleHQpIHtcclxuICAgICAgICAgICAgcmV0dXJuIGJyZWFrUGxhaW5UZXh0KHRleHQpO1xyXG4gICAgICAgIH0pXHJcbiAgICB9XHJcblxyXG4gICAgcmV0dXJuIHRoaXM7XHJcbn1cclxuXHJcbi8qKlxyXG4gKiBhZGp1c3RlZCBmcm9tIGh0dHBzOi8vc3RhY2tvdmVyZmxvdy5jb20vYS8xMzA2NzAwOS8xNTIzNzc2XHJcbiAqL1xyXG5mdW5jdGlvbiB3b3JrQXJvdW5kRml4ZWRIZWFkZXJGb3JBbmNob3JzKCkge1xyXG4gICAgY29uc3QgSElTVE9SWV9TVVBQT1JUID0gISEoaGlzdG9yeSAmJiBoaXN0b3J5LnB1c2hTdGF0ZSk7XHJcbiAgICBjb25zdCBBTkNIT1JfUkVHRVggPSAvXiNbXiBdKyQvO1xyXG5cclxuICAgIGZ1bmN0aW9uIGdldEZpeGVkT2Zmc2V0KCkge1xyXG4gICAgICAgIHJldHVybiAkKCdoZWFkZXInKS5maXJzdCgpLmhlaWdodCgpO1xyXG4gICAgfVxyXG5cclxuICAgIC8qKlxyXG4gICAgICogSWYgdGhlIHByb3ZpZGVkIGhyZWYgaXMgYW4gYW5jaG9yIHdoaWNoIHJlc29sdmVzIHRvIGFuIGVsZW1lbnQgb24gdGhlXHJcbiAgICAgKiBwYWdlLCBzY3JvbGwgdG8gaXQuXHJcbiAgICAgKiBAcGFyYW0gIHtTdHJpbmd9IGhyZWYgZGVzdGluYXRpb25cclxuICAgICAqIEBwYXJhbSAge0Jvb2xlYW59IHB1c2hUb0hpc3RvcnkgcHVzaCB0byBoaXN0b3J5XHJcbiAgICAgKiBAcmV0dXJuIHtCb29sZWFufSAtIFdhcyB0aGUgaHJlZiBhbiBhbmNob3IuXHJcbiAgICAgKi9cclxuICAgIGZ1bmN0aW9uIHNjcm9sbElmQW5jaG9yKGhyZWYsIHB1c2hUb0hpc3RvcnkpIHtcclxuICAgICAgICBsZXQgbWF0Y2gsIHJlY3QsIGFuY2hvck9mZnNldDtcclxuXHJcbiAgICAgICAgaWYgKCFBTkNIT1JfUkVHRVgudGVzdChocmVmKSkge1xyXG4gICAgICAgICAgICByZXR1cm4gZmFsc2U7XHJcbiAgICAgICAgfVxyXG5cclxuICAgICAgICBtYXRjaCA9IGRvY3VtZW50LmdldEVsZW1lbnRCeUlkKGhyZWYuc2xpY2UoMSkpO1xyXG5cclxuICAgICAgICBpZiAobWF0Y2gpIHtcclxuICAgICAgICAgICAgcmVjdCA9IG1hdGNoLmdldEJvdW5kaW5nQ2xpZW50UmVjdCgpO1xyXG4gICAgICAgICAgICBhbmNob3JPZmZzZXQgPSB3aW5kb3cucGFnZVlPZmZzZXQgKyByZWN0LnRvcCAtIGdldEZpeGVkT2Zmc2V0KCk7XHJcbiAgICAgICAgICAgIHdpbmRvdy5zY3JvbGxUbyh3aW5kb3cucGFnZVhPZmZzZXQsIGFuY2hvck9mZnNldCk7XHJcblxyXG4gICAgICAgICAgICAvLyBBZGQgdGhlIHN0YXRlIHRvIGhpc3RvcnkgYXMtcGVyIG5vcm1hbCBhbmNob3IgbGlua3NcclxuICAgICAgICAgICAgaWYgKEhJU1RPUllfU1VQUE9SVCAmJiBwdXNoVG9IaXN0b3J5KSB7XHJcbiAgICAgICAgICAgICAgICBoaXN0b3J5LnB1c2hTdGF0ZSh7fSwgZG9jdW1lbnQudGl0bGUsIGxvY2F0aW9uLnBhdGhuYW1lICsgaHJlZik7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHJldHVybiAhIW1hdGNoO1xyXG4gICAgfVxyXG5cclxuICAgIC8qKlxyXG4gICAgICogQXR0ZW1wdCB0byBzY3JvbGwgdG8gdGhlIGN1cnJlbnQgbG9jYXRpb24ncyBoYXNoLlxyXG4gICAgICovXHJcbiAgICBmdW5jdGlvbiBzY3JvbGxUb0N1cnJlbnQoKSB7XHJcbiAgICAgICAgc2Nyb2xsSWZBbmNob3Iod2luZG93LmxvY2F0aW9uLmhhc2gsIGZhbHNlKTtcclxuICAgIH1cclxuXHJcbiAgICAkKHdpbmRvdykub24oJ2hhc2hjaGFuZ2UnLCAoKSA9PiBzY3JvbGxUb0N1cnJlbnQoKSk7XHJcbiAgICAvLyBFeGNsdWRlIHRhYmJlZCBjb250ZW50IGNhc2VcclxuICAgIHNjcm9sbFRvQ3VycmVudCgpO1xyXG5cclxuICAgICQoZG9jdW1lbnQpLm9uKCdyZWFkeScsIGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAkKCdib2R5Jykuc2Nyb2xsc3B5KHtvZmZzZXQ6IDE1MH0pO1xyXG4gICAgfSk7XHJcbn1cclxuXHJcbmZ1bmN0aW9uIGJyZWFrVGV4dCgpIHtcclxuICAgICQoXCIueHJlZlwiKS5hZGRDbGFzcyhcInRleHQtYnJlYWtcIik7XHJcbiAgICBjb25zdCB0ZXh0cyA9ICQoXCIudGV4dC1icmVha1wiKTtcclxuICAgIHRleHRzLmVhY2goZnVuY3Rpb24gKCkge1xyXG4gICAgICAgICQodGhpcykuYnJlYWtXb3JkKCk7XHJcbiAgICB9KTtcclxufVxyXG4iXX0=
