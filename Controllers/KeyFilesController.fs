namespace EpisodeWebMvc.Controllers

open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq

open TvApi

[<Route("api/keyfiles")>]
type KeyFilesController() =
    inherit Controller()

    [<HttpGet>]
    member this.Get() =
        let files = Directory.GetFiles(@"z:\downloads\done", "*.otrkey")
                    |> Seq.map FileInfo
                    |> Seq.sortBy(fun i -> i.CreationTime)
                    |> Seq.map(fun i -> i.Name)

        let response = JObject()
        response.Add("files", JToken.FromObject(files))

        response

