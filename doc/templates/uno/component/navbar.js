/**
 * Changes the logo on resize
 */
function updateLogo() {
    const curWidth = window.innerWidth;
    if (curWidth < 980) {
        $('#logo').attr('src', '../images/UnoLogoSmall.png');
    } else {
        $('#logo').attr('src', '../images/uno-logo.svg');
    }
}

function updateLogoOnResize() {
    $(window).on('resize', function () {
        updateLogo();
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
