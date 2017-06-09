namespace EpisodeWebMvc.Controllers

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


    [<HttpPost>]
    member this.Post(parsed: string, mapped: string) = 
        showMappings <- showMappings |> Seq.append [(parsed, mapped, "")]
        let response = JObject()
        response.Add("message", JToken.FromObject(sprintf "added mapping: %s --> %s" parsed mapped))
        response