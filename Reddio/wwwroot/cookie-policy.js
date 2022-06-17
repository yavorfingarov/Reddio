"use strict";

function acceptCookies(callback) {
    storage.cookiesAccepted = true;
    document.getElementById("cookie-dialog").style.display = "none";
    callback();
}

function showCookieDialog(callback) {
    document.getElementById("cookie-dialog").style.display = "initial";
    document.getElementById("accept-cookies")
        .addEventListener("click", () => acceptCookies(callback));
}
