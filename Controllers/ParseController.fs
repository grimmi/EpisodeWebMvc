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

    let makeResponse file (show: Show option) (episode: Episode option) =
        let unknownShow = { seriesName = "unknown"; id = -1 }
        let unknownEpisode  = { airedEpisodeNumber = -1; airedSeason = -1; episodeName = "unknown"; firstAired = "1970-01-01" }
        let responseShow = if show.IsNone then unknownShow else show.Value
        let responseEpisode = if episode.IsNone then unknownEpisode else episode.Value

        let response = JObject()
        response.Add("file", JToken.FromObject(file))
        let showJson = JObject()
        showJson.Add("name", JToken.FromObject(responseShow.seriesName))
        showJson.Add("id", JToken.FromObject(responseShow.id))
        response.Add("show", showJson)
        let episodeJson = JObject()
        episodeJson.Add("season", JToken.FromObject(responseEpisode.airedSeason))
        episodeJson.Add("episode", JToken.FromObject(responseEpisode.airedEpisodeNumber))
        episodeJson.Add("name", JToken.FromObject(responseEpisode.episodeName))
        episodeJson.Add("firstaired", JToken.FromObject(responseEpisode.firstAired))
        response.Add("episode", episodeJson)
        response

    [<HttpGet>]
    member this.Get(file:string) = 
        let (parsedShow, dbShow) = getShowInfo file
        let episodeInfo = getEpisodeInfo file dbShow

        makeResponse file dbShow episodeInfo