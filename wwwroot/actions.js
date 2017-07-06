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
    else{
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

function editinfo(i){
    var info = fileinfos[i];
    var lblShow = document.createElement("label");
    lblShow.innerHTML = "Show:";
    var editShow = document.createElement("input");
    editShow.type = "text";
    editShow.setAttribute("value", info["show"]);
    var editDiv = document.getElementById("episodeedit");
    editDiv.innerHTML = "";
    editDiv.appendChild(lblShow);
    editDiv.appendChild(editShow);
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