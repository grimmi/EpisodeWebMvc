namespace EpisodeWebMvc.Controllers

open EpisodeWebMvc
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Microsoft.AspNetCore.Mvc

[<Route("api/decodejob")>]
type DecodeJobController(service : JobService) =
    inherit Controller()

    [<HttpGet>]
    member this.Get() =   
        match service.CurrentJob with
        |None -> let response = JObject()
                 response.Add("message", JToken.FromObject("no job running"))
                 response
        |Some job -> job.ToJson()

    [<HttpPost>]
    member this.Post(files: string) =        
        service.Run(files.Split([|','|]))

        match service.CurrentJob with
        |Some job -> job.ToJson()
        |_ -> JObject.Parse("{'message':'error creating job'}")