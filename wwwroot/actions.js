var fileinfos = [];

function loadFiles() {
    var req = new XMLHttpRequest();
    req.open("GET", "./api/fileinfo", true);
    req.setRequestHeader("Content-Type", "application/json");
    req.onload = function (e) {
        fileinfos = [];
        var infos = JSON.parse(req.responseText)["infos"];
        for (var i = 0; i < infos.length; i++) {
            var info = infos[i];
            var episode = info["episode"];
            var show = info["show"];
            var file = info["file"];

            fileinfos.push({ "show": show["name"], "episodename": episode["name"], "episodenumber": episode["number"], "season": episode["season"], "file": file });
        }

        listInfos();
    }
    req.send(null);
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
        liDiv.setAttribute("onclick", "editinfo(" + i + ")");
        li.appendChild(liDiv);
        ul.appendChild(li);
    }
}

function editinfo(i) {
    var info = fileinfos[i];
    var editDiv = document.getElementById("episodeedit");
    editDiv.setAttribute("style", "visibility:visible");
    var showEdit = document.getElementById("editshow");
    showEdit.setAttribute("value", info["show"]);
}

function searchShow() {
    var showBox = document.getElementById("editshow");
    var show = showBox.value;
    var req = new XMLHttpRequest();
    req.open("GET", "./api/showmapping?show=" + show, true);
    req.setRequestHeader("Content-Type", "application/json");
    req.send(null);
    req.onload = function (e) {
        if (req.readyState == 4) {
            handleResponse(req);
        }
    }
}

function handleResponse(e) {
    if (e.responseText.length > 0) {
        var shows = JSON.parse(e.responseText)["shows"];
        var mappinglist = document.getElementById("mappinglist");
        mappinglist.innerHTML = "";
        var parsedShow = document.getElementById("editshow").value;
        for(var i = 0; i < shows.length; i++){
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

function sendmapping(parsed, mapped, id){
    var req = new XMLHttpRequest();
    req.open("POST", "./api/showmapping");
    req.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    req.onload = function(e){
        alert("mapping eingetragen: " + e.responseText);
    }
    req.send("parsed=" + parsed + "&mapped=" + mapped + "&id=" + id);
}

function sendInfos() {
    var req = new XMLHttpRequest();
    req.open("POST", "./api/process", true);
    req.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    req.onload = function (e) {
        console.log(req.responseText);
    }
    req.send("infos=" + JSON.stringify(fileinfos));
}