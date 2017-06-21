namespace EpisodeWebMvc

open EpisodeWebMvc
open System
open System.IO
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Options
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open ShowFinder
open EpisodeFinder
open JsonTransformations

[<Route("api/fileinfo")>]
type FileInfoController(directoryOptions: IOptions<DirectoryOptions>, api : TvDbApi) =
    inherit Controller()

    let options = directoryOptions.Value
    [<HttpGet>]
    member this.Get() =
        let files = Directory.GetFiles(options.KeyFileDirectory, "*.otrkey")
                    |> Seq.map FileInfo
                    |> Seq.sortBy(fun i -> i.CreationTime)
                    |> Seq.map(fun i -> i.Name)

        let infos = Seq.map ((fun f -> (f, api |> getShowInfo f)) >> (fun (file, (parsed, dbShow)) -> (file, dbShow, api |> getEpisodeInfo file dbShow))) files
        let jsons = infos
                    |> Seq.choose(fun (f, s, e) -> match (s, e) with
                                                   |(_, None) -> None
                                                   |(None, _) -> None
                                                   |(_, _) -> Some (f, s.Value, e.Value))
                    |> Seq.map(fun (file, show, episode) -> 
                                let info = JObject()
                                info.Add("file", JToken.FromObject(file))
                                info.Add("show", JToken.FromObject((ShowToJson show)))
                                info.Add("episode", JToken.FromObject((EpisodeToJson episode)))
                                info)

        let response = JObject()
        response.Add("infos", JToken.FromObject(jsons))

        response