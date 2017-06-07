namespace EpisodeWebMvc

open System
open System.Threading.Tasks
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type DecodeJob() =
    let id = Guid.NewGuid()
    let mutable progValue = 0.
    let mutable progress = 0m
    let mutable currentStep = ""
    let mutable jobDone = false
    let mutable files : string list = []

    member this.Id
        with get () = id

    member this.Progress 
        with get () = Decimal.Parse(progValue.ToString("N2"))

    member internal this.ProgValue
        with get () = progValue
        and set (value) = progValue <- value

    member this.CurrentStep
        with get () = currentStep
        and set (value) = currentStep <- value

    member this.Files
        with get () = files
        and set(value) = files <- value

    member this.IsDone 
        with get () = jobDone
        and set(value) = jobDone <- value

    member this.Run = async {
        for i in [ 1 .. 10 ] do
            let! x = (Task.Delay(1000) |> Async.AwaitTask)
            this.ProgValue <- double(i * 10)
            printfn "aktueller fortschritt: %f" this.ProgValue
        this.IsDone <- true
    }

    member this.ToJson() =
        let json = JObject()
        json.Add("id", JToken.FromObject(this.Id))
        json.Add("progress", JToken.FromObject(this.Progress))
        json.Add("currentstep", JToken.FromObject(this.CurrentStep))
        json.Add("done", JToken.FromObject(this.IsDone))

        json