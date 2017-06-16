namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc
open System
open System.IO
open System.Text.RegularExpressions

[<Route("api/parse")>]
type ParseController(api:TvDbApi) =
    inherit Controller()

    let showNameRegex = @".+?(?=_\d\d.\d\d.\d\d)"

    let parseShowName file =
        let nameMatch = Regex.Match(file, showNameRegex)
        match nameMatch.Length with
        |0 -> None
        |_ -> Some (nameMatch.Value.Split([|"__"|], StringSplitOptions.RemoveEmptyEntries)
                    |> Seq.head
                    |> String.map(fun c -> match c with
                                           |'_' -> ' '
                                           |_ -> c))

    let getShowInfo file =
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

    [<HttpGet>]
    member this.Get(file:string) = 
        let showInfo = getShowInfo file
        let unknownShow = {seriesName = "unknown"; id = -1}

        match showInfo with
        |(Some parsed, Some show) -> (parsed, show)
        |(Some parsed, _) -> (parsed, unknownShow)
        |(_, _) -> ("<unable to parse showname>", unknownShow)