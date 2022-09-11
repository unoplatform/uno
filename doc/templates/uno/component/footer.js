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
