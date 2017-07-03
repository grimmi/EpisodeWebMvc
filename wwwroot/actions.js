var fileinfos = [];

function loadFiles(){
    var req = new XMLHttpRequest();
    req.open("GET", "./api/fileinfo", true);
    req.setRequestHeader("Content-Type", "application/json");
    req.onload = function(e){
        fileinfos = [];
        var infos = JSON.parse(req.responseText)["infos"];
        for(var i = 0; i < infos.length; i++){
            var info = infos[i];
            var episode = info["episode"];
            var show = info["show"];
            var file = info["file"];

            fileinfos.push({"show": show["name"], "episodename": episode["name"], "episodenumber": episode["number"], "season": episode["season"], "file": file });
        }
    }
    req.send(null);
}

function sendInfos(){
    var req = new XMLHttpRequest();
    req.open("POST", "./api/process", true);
    req.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    req.onload = function(e){
        console.log(req.responseText);
    }
    req.send("infos=" + JSON.stringify(fileinfos));
}