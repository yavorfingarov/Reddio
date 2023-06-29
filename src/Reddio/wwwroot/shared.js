"use strict";

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
