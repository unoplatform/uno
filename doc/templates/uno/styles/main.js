// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

document.addEventListener(
    "DOMContentLoaded",
    function () {

        initializeNavbar();

        document.addEventListener(
            "click",
            function (e) {
                const t = e.target;
                if (
                    window.innerWidth >= 980 ||
                    !t.matches("#navbar .has-children a")
                )
                    return;
                e.stopImmediatePropagation();
                t.parentElement.classList.toggle("open");
            },
            true
        );
    },
    false
);

