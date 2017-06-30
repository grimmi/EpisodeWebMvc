namespace EpisodeWebMvc

open System
open System.Threading.Tasks
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type JobService() = 

    let mutable currentJob : DecodeJob option = None

    member this.CurrentJob
        with get () = currentJob

    member this.Run (files : string seq) =
        match currentJob with
        |Some job when not job.IsDone -> ()
        |_ -> let tmpJob = DecodeJob()
              tmpJob.Files <- files |> List.ofSeq
              currentJob <- Some tmpJob
              (tmpJob.Run |> Async.Start) |> ignore

    member this.RunProcess (infos : ProcessInfo seq) =
        match currentJob with
        |Some job when not job.IsDone -> ()
        |_ -> let tmpJob = DecodeJob()
              currentJob <- Some tmpJob
              (tmpJob.RunInfos infos) |> Async.Start