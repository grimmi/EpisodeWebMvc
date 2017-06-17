module JsonTransformations

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System

let EpisodeToJson episode =
    let json = JObject()
    json.Add("season", JToken.FromObject(episode.airedSeason))
    json.Add("number", JToken.FromObject(episode.airedEpisodeNumber))
    json.Add("name", JToken.FromObject(episode.episodeName))
    json.Add("firstaired", JToken.FromObject(episode.firstAired))

    json

let ShowToJson show = 
    let json = JObject()
    json.Add("name", JToken.FromObject(show.seriesName))
    json.Add("id", JToken.FromObject(show.id))

    json