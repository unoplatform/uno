if ( document.body.classList.contains('front-page') ) {

    var last_known_scroll_position = 0;
    var ticking = false;
    var navbar = document.querySelector('header > .navbar');

    function doSomething(scroll_pos) {

        if (scroll_pos>=100)
            navbar.classList.add("scrolled");
        else
            navbar.classList.remove("scrolled");
    }

    window.addEventListener('scroll', function(e) {

        last_known_scroll_position = window.scrollY;

        if (!ticking) {

            window.requestAnimationFrame(function() {
                doSomething(last_known_scroll_position);
                ticking = false;
            });

            ticking = true;
        }
    });
}

