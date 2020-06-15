document.addEventListener('DOMContentLoaded', function(){
    if (document.body.classList.contains("front-page")) {
        var last_known_scroll_position = 0;
        var ticking = false;
        var navbar = document.querySelector("header > .navbar");

        function doSomething(scroll_pos) {
            if (scroll_pos >= 100) navbar.classList.add("scrolled");
            else navbar.classList.remove("scrolled");
        }

        window.addEventListener("scroll", function(e) {
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

    // if local env, use toc.yml for main nav
    // else use wp menu
    if (window.location.hostname !== 'localhost') {
        const unoMenuReq = new XMLHttpRequest();
        const unoMenuEndpoint = window.location.protocol + "//" + window.location.hostname + "/wp-json/wp/v2/menu";
        unoMenuReq.open("get", unoMenuEndpoint, true);

        if (typeof navbar !== 'undefined') {
            unoMenuReq.onload = function (e) {
                if (unoMenuReq.status === 200 && unoMenuReq.responseText)
                    document.getElementById("navbar").innerHTML = JSON.parse(unoMenuReq.responseText);
            };
            unoMenuReq.send();
        }
    }

    document.addEventListener('click', function (e) {
        const t = e.target;
        if (window.innerWidth >= 980 || !t.matches('#navbar .has-children a')) return;
        e.preventDefault();
        t.parentElement.classList.toggle('open');
    }, false);

}, false);

