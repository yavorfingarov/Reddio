"use strict";

function acceptCookies(callback) {
    storage.cookiesAccepted = true;
    document.getElementById("cookie-dialog").style.display = "none";
    callback();
}

function showCookieDialog(callback) {
    let dialog = document.getElementById("cookie-dialog");
    dialog.style.display = "initial";
    document.getElementById("accept-cookies")
        .addEventListener("click", () => acceptCookies(callback));
}
