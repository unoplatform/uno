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
