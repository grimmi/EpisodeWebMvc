var fileinfos = [];
var editInfo = {};
var editIndex = -1;

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
        liDiv.innerHTML = info["show"] + ": " + info["episodename"] + " (" + info["season"] + "x" + info["episodenumber"] + ")";
        if(info["episodenumber"] == -1){
            liDiv.style.color = "orange";
            liDiv.style.fontWeight = "bold";
        }
        if(info["changed"] == true){
            liDiv.style.color = "green";
            liDiv.style.fontWeight = "bold";
        }
        liDiv.setAttribute("onclick", "editinfo(" + i + ")");
        li.appendChild(liDiv);
        ul.appendChild(li);
    }
}

function editinfo(i) {
    editInfo = fileinfos[i];
    var info = fileinfos[i];
    var editDiv = document.getElementById("episodeedit");
    editDiv.setAttribute("style", "visibility:visible");
    var fileSpan = document.getElementById("filename");
    fileSpan.innerText = info["file"];
    var showEdit = document.getElementById("editshow");
    showEdit.setAttribute("value", info["show"]);
    var epNameEdit = document.getElementById("editepisodename");
    epNameEdit.setAttribute("value", info["episodename"]);
    var seasonEdit = document.getElementById("editseasonnumber");
    seasonEdit.setAttribute("value", info["season"]);
    var epNoEdit = document.getElementById("editepisodenumber");
    epNoEdit.setAttribute("value", info["episodenumber"]);
}

function saveEdit(){
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
    fetch("/api/process", {
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        method: "POST",
        body: "infos=" + JSON.stringify(fileinfos)
    }).then(function(r){
        processdone = false;
        showStatus();
    });
}

var processdone = false;

function showStatus() {
    if(processdone){ return; }
    setTimeout(showStatus, 5000);
    fetch("/api/process", {
        headers: { "Content-Type": "application/json" },
        method: "GET"
    }).then(resp => resp.json())
        .then(function (response) {
            if (!response["done"]) {
                Materialize.toast(response["progress"] + "% (" + response["currentstep"] + ")", 4000);
            }
            else{
                processdone = true;
                Materialize.toast("all done!", 4000);
            }
        });
}