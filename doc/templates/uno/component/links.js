// Open links to different host in a new window.
function renderLinks() {
    if ($("meta[property='docfx:newtab']").attr("content") === "true") {
        $(document.links).filter(function () {
            return this.hostname !== window.location.hostname;
        }).attr('target', '_blank');
    }
}
