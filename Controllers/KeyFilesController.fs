namespace EpisodeWebMvc.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<Route("api/keyfiles")>]
type KeyFilesController() =
    inherit Controller()

    [<HttpGet>]
    member this.Get() =
        JObject.Parse("{'someother':'value'}")

