namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc

[<Route("api/decodejob")>]
type DecodeJobController() =
    inherit Controller()
    static let mutable jobService:JobService option = None

    [<HttpGet>]
    member this.Get() = 
        let service =   match jobService with
                        |None -> jobService <- Some(JobService())
                                 jobService.Value
                        |_ -> jobService.Value

        let f = ["somefileabc"]
        service.Run f

        let response = JObject()
        let job = service.CurrentJob
        match job with
        |None -> response.Add("message", JToken.FromObject("no job running"))
        |Some j ->  response.Add("id", JToken.FromObject(j.Id))
                    response.Add("progress", JToken.FromObject(j.Progress))
        response