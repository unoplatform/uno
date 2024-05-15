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
}

/**
 * Changes the logo on resize
*/

function updateLogo() {
    const curWidth = window.innerWidth;
    const headerLogo = document.getElementById('logo');
    if (curWidth < 1024) {
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
