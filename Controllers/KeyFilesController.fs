namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Configuration
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<Route("api/keyfiles")>]
type KeyFilesController(config:IConfigurationRoot) =
    inherit Controller()

    [<HttpGet>]
    member this.Get() =  
        let dir = config.GetValue("keyfiledirectory", "")      
        let files = Directory.GetFiles(dir, "*.otrkey")
                    |> Seq.map FileInfo
                    |> Seq.sortBy(fun i -> i.CreationTime)
                    |> Seq.map(fun i -> i.Name)

        let response = JObject()
        response.Add("files", JToken.FromObject(files))

        response

