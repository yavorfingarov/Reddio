"use strict";

function addLeadingZero(n) {
    if (n <= 9) {
        return "0" + n;
    }
    return n;
}

function createHistoryRow(track) {
    let row = document.createElement("div");
    let timestamp = document.createElement("div");
    timestamp.className = "timestamp";
    let playedDate = new Date(track.played);
    timestamp.innerText = playedDate.getFullYear() + "-" +
        addLeadingZero(playedDate.getMonth() + 1) + "-" +
        addLeadingZero(playedDate.getDate()) + " " +
        addLeadingZero(playedDate.getHours()) + ":" +
        addLeadingZero(playedDate.getMinutes());
    row.append(timestamp);
    let entry = document.createElement("div");
    let text = document.createTextNode("[" + track.station + "] ");
    entry.append(text);
    let link = document.createElement("a");
    link.href = createRedditLink(track.station, track.threadId);
    link.innerText = track.title;
    entry.append(link);
    row.append(entry);
    return row;
}

function clearHistory() {
    const message = "Your locally saved history enables the application to keep playing music that you have not yet heard.\n\n" + 
        "Do you want to irreversibly delete your local history?";
    if (confirm(message)) {
        storage.history = [];
        renderHistory();
    }
}

function renderHistory() {
    const historyContainer = document.getElementById("history");
    historyContainer.replaceChildren();
    const history = storage.history ?? [];
    if (history.length === 0) {
        document.getElementById("clear-history").disabled = true;
    } else {
        for (let track of history) {
            if (track.played) {
                let row = createHistoryRow(track);
                historyContainer.append(row);
            }
        }
        document.getElementById("clear-history")
            .addEventListener("click", clearHistory);
    }
}

renderHistory();
