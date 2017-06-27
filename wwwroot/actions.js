var fileinfos = [];

function loadFiles(){
    window.alert("loading...");
    var req = new XMLHttpRequest();
    req.open("GET", "./api/fileinfo", true);
    req.setRequestHeader("Content-Type", "application/json");
    req.onload = function(e){
        fileinfos = [];
        console.log(req.responseText);
        var infos = JSON.parse(req.responseText)["infos"];
        for(var i = 0; i < infos.length; i++){
            var info = infos[i];
            var episode = info["episode"];
            var show = info["show"];
            var file = info["file"];

            fileinfos.push({"show": show["name"], "episodename": episode["name"], "episodenumber": episode["number"], "season": episode["season"], "file": file });
        }

        for(var j = 0; j < fileinfos.length; j++){
            console.log(fileinfos[j]);
        }
    }
    req.send(null);
}