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
