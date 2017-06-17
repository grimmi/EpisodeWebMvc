module ShowFinder

open EpisodeWebMvc
open System
open System.Text.RegularExpressions

let beforeDatePattern = @".+?(?=_\d\d.\d\d.\d\d)"
let datePattern = @"(\d\d.\d\d.\d\d)"

let parseShowName file =
    let nameMatch = Regex.Match(file, beforeDatePattern)
    match nameMatch.Length with
    |0 -> None
    |_ -> Some (nameMatch.Value.Split([|"__"|], StringSplitOptions.RemoveEmptyEntries)
                |> Seq.head
                |> String.map(fun c -> match c with
                                       |'_' -> ' '
                                       |_ -> c))

let getShowInfo file (api:TvDbApi) =
    let getInfo = async{
        let parsedShow = parseShowName file
        let dbShow = match parsedShow with
                     |None -> None
                     |Some name -> 
                            let dbResult = async {
                                let! dbShows = api.SearchShow name
                                match dbShows |> Seq.length with
                                |1 -> return Some (dbShows |> Seq.head)
                                |_ -> return None
                            }
                            (dbResult |> Async.RunSynchronously)
        return (parsedShow, dbShow)
    }

    let result = (getInfo |> Async.RunSynchronously)
    result    