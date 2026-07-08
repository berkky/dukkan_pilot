(function () {
    try {
        var key = "dp_cookie_notice_dismissed";
        var root = document.getElementById("dp-cookie-notice");
        if (!root) return;

        if (window.localStorage && localStorage.getItem(key) === "1") {
            return;
        }

        root.hidden = false;

        var btn = document.getElementById("dp-cookie-notice-accept");
        if (btn) {
            btn.addEventListener("click", function () {
                try {
                    if (window.localStorage) {
                        localStorage.setItem(key, "1");
                    }
                } catch (e) { /* ignore quota / private mode */ }
                root.hidden = true;
            });
        }
    } catch (e) {
        /* cookie notice must not break the page */
    }
})();
