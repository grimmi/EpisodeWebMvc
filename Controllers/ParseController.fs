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
        |_ -> Some (nameMatch.Value |> String.map(fun c -> match c with
                                                           |'_' -> ' '
                                                           |_ -> c))

    [<HttpGet>]
    member this.Get(file:string) = 
        let getInfo = async{
            let parsedShow = parseShowName file
            if parsedShow.IsNone then
                return ("", "")
            else 
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
                let dbShowName = match dbShow with
                                 |None -> "<not found>"
                                 |Some show -> show.seriesName
                return (parsedShow.Value, dbShowName)
        }

        let result = (getInfo |> Async.RunSynchronously)
        result