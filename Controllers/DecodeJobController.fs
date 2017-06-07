namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc

[<Route("api/decodejob")>]
type DecodeJobController(jobService : JobService) =
    inherit Controller()
    let service = jobService

    [<HttpGet>]
    member this.Get() =         
        let job = service.CurrentJob
        match job with
        |None -> let errResponse = JObject()
                 errResponse.Add("message", JToken.FromObject("no job running"))
                 errResponse
        |Some j -> j.ToJson()

    [<HttpPost>]
    member this.Post(files: string) =
        let ops = files.Split([|','|])
        service.Run ops

        match service.CurrentJob with
        |Some job -> job.ToJson()
        |_ -> JObject.Parse("{'message':'error creating job'}")