namespace EpisodeWebMvc.Controllers

open ConfigReader
open EpisodeWebMvc
open System
open System.Net.Http
open System.Net.Http.Headers
open System.IO
open System.Text
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Options
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open ShowFinder
open EpisodeFinder
open JsonTransformations

[<Route("api/updatemedialibrary")>]
type UpdateMediaLibraryController() =
    inherit Controller()

    let client = new HttpClient()

    [<HttpGet>]
    member this.Get() = 
        let makeCall = async{
            try
                let kodiCfg = ReadConfigToDict "./kodi.cfg"
                let url = sprintf "http://%s:%s@%s" kodiCfg.["user"] kodiCfg.["password"] kodiCfg.["url"]
                let req = new HttpRequestMessage(HttpMethod.Post, url)
                let content = JObject()
                content.Add("jsonrpc", JToken.FromObject("2.0"))
                content.Add("method", JToken.FromObject("VideoLibrary.Scan"))
                let stringContent = content.ToString()
                req.Content <- new StringContent(stringContent, Encoding.UTF8, "application/json")
                let! response = (client.SendAsync(req) |> Async.AwaitTask)
                let! responseText = (response.Content.ReadAsStringAsync() |> Async.AwaitTask)
                return responseText
            with
                | :? System.AggregateException as ex -> for innerex in ex.InnerExceptions do
                                                            printfn "inner ex: %s" innerex.Message
                                                        return ex.Message
        }
        let kodiResponse = makeCall |> Async.RunSynchronously
        kodiResponse