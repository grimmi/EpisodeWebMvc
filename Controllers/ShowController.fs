namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc
open System
open System.IO

[<Route("api/show")>]
type ShowController() =
    inherit Controller()

    let episodeToJson (episode:Episode) = 
        let json = JObject()
        json.Add("airedepisodenumber", JToken.FromObject(episode.airedEpisodeNumber))
        json.Add("airedseason", JToken.FromObject(episode.airedSeason))
        json.Add("episodename", JToken.FromObject(episode.episodeName))
        json.Add("firstaired", JToken.FromObject(episode.firstAired))
        json

    [<HttpGet>]
    member this.Get(showId) = 
        let task = async{
            let api = TvDbApi()
            let! episodes = api.GetEpisodes showId
            return episodes
        }
        let result = (task |> Async.RunSynchronously)

        let episodes = result |> Seq.map episodeToJson

        let response = JObject()
        response.Add("episodes", JToken.FromObject((episodes |> Array.ofSeq)))
        response