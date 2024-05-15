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
