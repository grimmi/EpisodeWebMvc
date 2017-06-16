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

    let canonizeEpisodeName (name:string) =
        name.ToLower()
        |> String.filter Char.IsLetter

    let getEpisodeInfo (file:string) episodeShow = 
        let getEpisode = async{
            match episodeShow with
            | None -> return None
            | Some show -> 
                if file.Contains("__") then
                    let episodePart = file.Split([|"__"|], StringSplitOptions.RemoveEmptyEntries).[1]
                    let episodeNameMatch = Regex.Match(episodePart, beforeDatePattern)
                    let episodeName = episodeNameMatch.Value
                    let! showEpisodes = api.GetEpisodes show.id 
                    let foundEpisode = showEpisodes
                                       |> Seq.tryFind(fun ep -> (ep.episodeName |> canonizeEpisodeName) = (episodeName |> canonizeEpisodeName))
                    return foundEpisode
                else
                    let aired = match Regex.Match(file, datePattern).Value.Split('.') with
                                |[|yy;mm;dd|] -> sprintf "20%s-%s-%s" yy mm dd
                                |_ -> ""
                    if aired = "" then
                        return None
                    else
                        let! showEpisodes = api.GetEpisodes show.id
                        let foundEpisode = showEpisodes
                                           |> Seq.tryFind(fun ep -> ep.firstAired = aired)
                        return foundEpisode
        }
        (getEpisode |> Async.RunSynchronously)



    [<HttpGet>]
    member this.Get(file:string) = 
        let showInfo = getShowInfo file
        let unknownShow = { seriesName = "unknown"; id = -1 }
        let unknownEpisode  = { airedEpisodeNumber = -1; airedSeason = -1; episodeName = "unknown"; firstAired = "1970-01-01" }

        let episodeInfo = getEpisodeInfo file (showInfo |> snd)

        match (showInfo, episodeInfo) with
        |((Some parsed, Some show), Some episode) -> (parsed, show, episode)
        |((Some parsed, _), _) -> (parsed, unknownShow, unknownEpisode)
        |((_, _), _) -> ("<unable to parse showname>", unknownShow, unknownEpisode)