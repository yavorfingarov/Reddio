"use strict";

async function fetchQueue(appendToQueue) {
    let ignoreThreadIds = (storage.history ?? [])
        .filter(t => t.station === station)
        .map(t => t.threadId);
    let body = {
        station,
        ignoreThreadIds: [ ...new Set(ignoreThreadIds) ]
    };
    let requestOptions = {
        method: "POST",
        headers: {
            "content-type": "application/json; charset=utf-8"
        },
        body: JSON.stringify(body)
    };
    let response = await fetch("/api/queue", requestOptions);
    let queue = await response.json();
    if (appendToQueue) {
        storage.queue = storage.queue.concat(queue);
    } else {
        storage.current = queue.shift();
        storage.queue = queue;
    }
}

function loadTrack(track, playing) {
    const player = document.getElementById("player");
    let playerHeight = player.clientWidth * (9 / 16);
    renderReactPlayer(document.getElementById("player"), {
        url: track.url,
        playing,
        width: "100%",
        height: playerHeight,
        controls: true,
        onStart,
        onEnded: loadNextTrack,
        config: {
            youtube: {
                playerVars: {
                    modestbranding: 1
                }
            }
        }
    });
    document.getElementById("track-title").innerText = track.title;
}

async function onStart() {
    storage.current = { ...storage.current, played: Date.now() };
    if (storage.queue.length < 5) {
        await fetchQueue(true);
    }
}

function loadNextTrack() {
    let queue = storage.queue;
    if (queue.length > 0) {
        let current = queue.shift();
        loadTrack(current, true);
        let history = storage.history ?? [];
        if (history.length === 1000) {
            history.pop();
        }
        history.unshift({ ...storage.current, station })
        storage.history = history;
        storage.current = current;
        storage.queue = queue;
    }
}

function init() {
    fetchQueue(false).then(() => {
        document.getElementById("player-container").style.display = "initial";
        loadTrack(storage.current, false);
        document.getElementById("load-next-track")
            .addEventListener("click", loadNextTrack);
    });
}

if (typeof showCookieDialog === "function" && !storage.cookiesAccepted) {
    showCookieDialog(init);
} else {
    init();
}



