namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open EpisodeFinder
open JsonTransformations
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc
open ShowFinder
open System
open System.IO
open System.Text.RegularExpressions

[<Route("api/parse")>]
type ParseController(api:TvDbApi) =
    inherit Controller()

    let makeResponse file (show: Show option) (episode: Episode option) =
        let unknownShow = { seriesName = "unknown"; id = -1 }
        let unknownEpisode  = { airedEpisodeNumber = -1; airedSeason = -1; episodeName = "unknown"; firstAired = "1970-01-01" }
        let responseShow = if show.IsNone then unknownShow else show.Value
        let responseEpisode = if episode.IsNone then unknownEpisode else episode.Value

        let response = JObject()
        response.Add("file", JToken.FromObject(file))
        let showJson = ShowToJson responseShow
        let episodeJson = EpisodeToJson responseEpisode
        response.Add("show", showJson)
        response.Add("episode", episodeJson)
        response

    [<HttpGet>]
    member this.Get(file:string) = 
        let (parsedShow, dbShow) = api |> getShowInfo file
        let episodeInfo = api |> getEpisodeInfo file dbShow

        makeResponse file dbShow episodeInfo