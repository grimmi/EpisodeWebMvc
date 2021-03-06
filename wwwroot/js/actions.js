var fileinfos = [];
var editInfo = {};
var editIndex = -1;
var processIndex = 0;

function loadFiles() {
    fetch("/api/fileinfo")
        .then(resp => resp.json())
        .then(function (response) {
            fileinfos = [];
            var infos = response["infos"];
            for (var i = 0; i < infos.length; i++) {
                var info = infos[i];
                var episode = info["episode"];
                var show = info["show"];
                var file = info["file"];

                fileinfos.push({ "show": show["name"], "episodename": episode["name"], "episodenumber": episode["number"], "season": episode["season"], "file": file });
            }
            listInfos();
            processIndex = 0;
        });
}

function listInfos() {
    var ul = document.getElementById("thelist");
    if (ul == null) {
        var listDiv = document.getElementById("infolist");
        var ul = document.createElement("ul");
        ul.setAttribute("id", "thelist");
        listDiv.appendChild(ul);
    }
    else {
        ul.innerHTML = "";
    }

    for (var i = 0; i < fileinfos.length; i++) {
        var info = fileinfos[i];
        var li = document.createElement("li");
        var liDiv = document.createElement("div");
        liDiv.setAttribute("id", "ep-" + i);
        liDiv.innerHTML = info["show"] + ": " + info["episodename"] + " (" + info["season"] + "x" + info["episodenumber"] + ") <span class='filename'>" + info["file"] + "</span>";
        if (i % 2 == 0) {
            liDiv.classList.add("evenrow");
        }
        else {
            liDiv.classList.add("oddrow");
        }
        if (info["episodenumber"] == -1) {
            liDiv.classList.add("missinginfo");
        }
        else {
            liDiv.classList.add("found");
        }
        if (info["changed"] == true) {
            liDiv.classList.remove("missinginfo");
            liDiv.classList.remove("found");
            liDiv.classList.add("edited");
        }
        liDiv.setAttribute("onclick", "editinfo(" + i + ")");
        li.appendChild(liDiv);
        ul.appendChild(li);
    }
}

function editinfo(i) {
    var episodeElement = document.getElementById("ep-" + i);
    episodeElement.classList.add("markedepisode");
    if (i > 0) {
        document.getElementById("ep-" + (i - 1)).scrollIntoView();
    }
    $('#editepisode').modal('open',{
        dismissible: true,
        opacity: .1,
        inDuration: 300,
        outDuration: 200,
        complete: function () { unmarkEpisode(i); }
    });
    editInfo = fileinfos[i];
    var info = fileinfos[i];
    var fileSpan = document.getElementById("filename");
    fileSpan.innerText = info["file"];
    var showEdit = document.getElementById("editshow");
    showEdit.value = info["show"];
    var epNameEdit = document.getElementById("editepisodename");
    epNameEdit.value = info["episodename"];
    var seasonEdit = document.getElementById("editseasonnumber");
    seasonEdit.value = info["season"];
    var epNoEdit = document.getElementById("editepisodenumber");
    epNoEdit.value = info["episodenumber"];
}

function markEpisode(i) {
    document.getElementById("ep-" + i).classList.add("markedepisode");
}

function unmarkEpisode(i) {
    document.getElementById("ep-" + i).classList.remove("markedepisode");
}

function saveEdit() {
    var showName = document.getElementById("editshow").value;
    var episodeName = document.getElementById("editepisodename").value;
    var season = document.getElementById("editseasonnumber").value;
    var episodeNumber = document.getElementById("editepisodenumber").value;

    editInfo["show"] = showName;
    editInfo["episodename"] = episodeName;
    editInfo["season"] = Number(season);
    editInfo["episodenumber"] = Number(episodeNumber);
    editInfo["changed"] = true;

    listInfos();
}

function searchShow() {
    var showBox = document.getElementById("editshow");
    var show = showBox.value;
    fetch("/api/showmapping?show=" + show, {
        headers: { "Content-Type": "application/json" }
    })
        .then((resp) => resp.json())
        .then(function (response) { handleResponse(response); });
}

function handleResponse(e) {
    if (e !== null) {
        var shows = e["shows"];
        var mappinglist = document.getElementById("mappinglist");
        mappinglist.innerHTML = "";
        var parsedShow = document.getElementById("editshow").value;
        for (var i = 0; i < shows.length; i++) {
            var mapping = shows[i];
            var li = document.createElement("li");
            li.setAttribute("id", "map-" + i);
            var liDiv = document.createElement("div");
            liDiv.setAttribute("onclick", "sendmapping('" + parsedShow + "','" + mapping["name"] + "'," + mapping["tvdbid"] + ")");
            liDiv.innerText = mapping["name"] + " (id: " + mapping["tvdbid"] + ")";
            li.appendChild(liDiv);
            mappinglist.appendChild(li);
        }
    }
}

function sendmapping(parsed, mapped, id) {
    if (confirm(unescape("Sicher, dass Sie f%FCr die erkannte Show '" + parsed + "' [" + mapped + "] als g%FCltigen Namen eintragen m%F6chten?"))) {
        fetch("/api/showmapping", {
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            method: "POST",
            body: "parsed=" + parsed + "&mapped=" + mapped + "&id=" + id
        })
            .then(resp => resp.json())
            .then(function (response) {
                showResponse(JSON.stringify(response));
            });
    }
}

function showResponse(e) {
    alert("response: " + e);
}

function sendInfos() {
    for (var i = processIndex; i < fileinfos.length; i++) {
        var procInfo = fileinfos[i];
        if (procInfo["show"] !== "unknown"
            && procInfo["season"] !== -1
            && procInfo["episodename"] !== "unknown"
            && procInfo["episodenumber"] !== -1) {
            processIndex = i;
            sendInfo(i);
            break;
        }
        else {
            Materialize.toast("skipping " + procInfo["file"] + " because the info is incomplete", 3500);
        }
        if (i === fileinfos.length - 1) {
            Materialize.toast("all done!", 4000);
        }
    }
}

function sendInfo(index) {
    var infoToSend = fileinfos[index];
    fetch("/api/process", {
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        method: "POST",
        body: "infos=" + encodeURIComponent(JSON.stringify([infoToSend]))
    }).then(function (r) {
        showStatus();
    });
}

function infoString(info) {
    return info["show"] + ": " + info["episodename"] + " (" + info["season"] + "x" + info["episodenumber"] + ")";
}

function showStatus() {
    fetch("/api/process", {
        headers: { "Content-Type": "application/json" },
        method: "GET"
    }).then(resp => resp.json())
        .then(function (response) {
            var currentInfo = fileinfos[processIndex];
            if (!response["done"]) {
                setTimeout(showStatus, 2500);
                Materialize.toast(response["progress"] + "% (" + infoString(currentInfo) + ")", 2250);
            }
            else {
                markFinished(processIndex);
                processIndex = processIndex + 1;
                Materialize.toast(infoString(currentInfo) + " processed!", 2500)
                sendInfos();
            }
        });
}

function markFinished(index) {
    var episodeDiv = document.getElementById("ep-" + index);
    episodeDiv.classList.remove("missinginfo");
    episodeDiv.classList.remove("edited");
    episodeDiv.classList.remove("found");
    episodeDiv.classList.add("processed");
    episodeDiv.style.fontWeight = "bold";
    episodeDiv.style.color = "green";
}

function updateLibrary() {
    fetch("/api/updatemedialibrary", {
        method: "GET"
    });
}