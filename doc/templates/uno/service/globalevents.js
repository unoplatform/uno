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
