namespace EpisodeWebMvc

open System
open System.IO
open System.Threading.Tasks
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open OtrDecoder
open ConfigReader

type DecodeJob() =
    let id = Guid.NewGuid()
    let mutable progValue = 0.
    let mutable progress = 0m
    let mutable currentStep = ""
    let mutable jobDone = false
    let mutable files : string list = []


    let getOptions =         
        let options = DecodeOptions()
        let otrConfig = ReadConfigToDict("./otr.cfg")
        options.DecoderPath <- otrConfig.["decoderpath"]        
        options.Email <- otrConfig.["email"]              
        options.OutputDirectory <- otrConfig.["outputpath"]    
        options.Password <- otrConfig.["password"]           
        options

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
        let decoder = OtrFileDecoder()
        for i in [ 0 .. (this.Files |> Seq.length) - 1] do
            let file = this.Files.[i]
            this.CurrentStep <- file
            this.ProgValue <- (100. / float(this.Files |> Seq.length)) * float(i)
            decoder.DecodeFile file getOptions |> ignore

        this.IsDone <- true
    }

    member this.ToJson() =
        let json = JObject()
        json.Add("id", JToken.FromObject(this.Id))
        json.Add("progress", JToken.FromObject(this.Progress))
        json.Add("currentstep", JToken.FromObject(this.CurrentStep))
        json.Add("done", JToken.FromObject(this.IsDone))

        json