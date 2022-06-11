"use strict";

function createRedditLink(station, threadId) {
    return "https://www.reddit.com/r/" + station + "/comments/" + threadId;
}

const storage = new Proxy({}, {
    get(_, key) {
        return JSON.parse(localStorage.getItem(key));
    },
    set(_, key, value) {
        localStorage.setItem(key, JSON.stringify(value));
        return true;
    }
});

storage.version = 1;
