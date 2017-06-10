namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc
open System
open System.IO

[<Route("api/showmapping")>]
type ShowMappingController() =
    inherit Controller()

    let loadMappings = 
        File.ReadAllLines("./shows.map")
        |> Seq.map(fun (l:string) -> match l.Split("***", StringSplitOptions.RemoveEmptyEntries) with
                                     |[|parsed; mapped; id|] -> (parsed.Trim(),mapped.Trim(),id.Trim())
                                     |_ -> ("","",""))
        |> Seq.filter(fun triple -> triple <> ("","",""))
    let mutable showMappings = loadMappings

    [<HttpGet>]
    member this.Get(show:string) = 
        let result = async{
            if String.IsNullOrWhiteSpace show then
                let response = JObject()
                let mappings = showMappings
                               |> Seq.map(fun (parsed, mapped, id) -> let mapping = JObject()
                                                                      mapping.Add("parsed", JToken.FromObject(parsed))
                                                                      mapping.Add("mapped", JToken.FromObject(mapped))
                                                                      mapping.Add("tvdbid", JToken.FromObject(id))
                                                                      mapping)
                               |> Array.ofSeq
                response.Add("mappings", JToken.FromObject(mappings))
                return response
            else
                let api = TvDbApi()
                let! response = api.SearchShow show
                return response
        }
        result |> Async.RunSynchronously                                                                
            

    [<HttpPost>]
    member this.Post(parsed: string, mapped: string) = 
        showMappings <- showMappings |> Seq.append [(parsed, mapped, "")]
        let response = JObject()
        response.Add("message", JToken.FromObject(sprintf "added mapping: %s --> %s" parsed mapped))
        response