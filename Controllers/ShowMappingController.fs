namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc
open System
open System.IO

[<Route("api/showmapping")>]
type ShowMappingController(api: TvDbApi) =
    inherit Controller()

    let dbApi = api

    let loadMappings = 
        File.ReadAllLines("./shows.map")
        |> Seq.map(fun (l:string) -> match l.Split("***", StringSplitOptions.RemoveEmptyEntries) with
                                     |[|parsed; mapped; id|] -> (parsed.Trim(),mapped.Trim(),id.Trim())
                                     |_ -> ("","",""))
        |> Seq.filter(fun triple -> triple <> ("","",""))

    let cacheShow parsed mapped id =
        File.AppendAllText("./shows.map", (sprintf "%s *** %s *** %d" parsed mapped id))

    let mutable showMappings = loadMappings

    let mappingToJson (parsed, mapped, id) =
        let mapping = JObject()
        mapping.Add("parsed", JToken.FromObject(parsed))
        mapping.Add("mapped", JToken.FromObject(mapped))
        mapping.Add("tvdbid", JToken.FromObject(id))
        mapping

    let showToJson (show:Show) =
        let json = JObject()
        json.Add("tvdbid", JToken.FromObject(show.id))
        json.Add("name", JToken.FromObject(show.seriesName))
        json

    let showsToJson (shows:seq<Show>) =
        let json = JObject()
        let showsJson = shows |> Seq.map showToJson |> Array.ofSeq
        json.Add("shows", JToken.FromObject(showsJson))
        json

    [<HttpGet>]
    member this.Get(show:string) = 
        let result = async{
            if String.IsNullOrWhiteSpace show then
                let response = JObject()
                response.Add("mappings", JToken.FromObject(showMappings |> Seq.map mappingToJson))
                return response
            else
                let! shows = dbApi.SearchShow show
                return showsToJson shows
        }
        result |> Async.RunSynchronously                                                                
            

    [<HttpPost>]
    member this.Post(parsed: string, mapped: string, id: int) = 
        cacheShow parsed mapped id
        showMappings <- showMappings |> Seq.append [(parsed, mapped, (id |> string))]
        let response = JObject()
        response.Add("message", JToken.FromObject(sprintf "added mapping: %s --> %s" parsed mapped))
        response