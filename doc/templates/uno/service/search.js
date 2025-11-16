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
     * @param worker The search worker used by docfx (lunr)
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
