namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Options
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<Route("api/keyfiles")>]
type KeyFilesController(directoryOptions: IOptions<DirectoryOptions>) =
    inherit Controller()

    let options = directoryOptions.Value

    [<HttpGet>]
    member this.Get() =          
        let files = Directory.GetFiles(options.KeyFileDirectory, "*.otrkey")
                    |> Seq.map FileInfo
                    |> Seq.sortBy(fun i -> i.CreationTime)
                    |> Seq.map(fun i -> i.Name)

        let response = JObject()
        response.Add("files", JToken.FromObject(files))

        response

