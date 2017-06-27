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

    member val internal ProgValue = 0. with get, set

    member this.Progress 
        with get () = Decimal.Parse(this.ProgValue.ToString("N2"))

    member val CurrentStep = "" with get, set

    member val Files : string list = [] with get, set

    member val IsDone = false with get, set

    member this.Run = async {
        let decoder = OtrFileDecoder()
        for i in [ 0 .. (this.Files |> Seq.length) - 1] do
            let file = this.Files.[i]
            this.CurrentStep <- file
            this.ProgValue <- (100. / float(this.Files |> Seq.length)) * float(i)
            decoder.DecodeFile file getOptions |> ignore

        this.IsDone <- true
    }

    member this.RunInfos (infos : ProcessInfo seq) = async {
        return infos
               |> Seq.map(fun info -> let decoder = OtrFileDecoder()
                                      let decodedFile = decoder.DecodeFile info.file getOptions
                                      let targetDir = ReadConfigToDict("./otr.cfg").["targetpath"]
                                      let targetFile = Path.Combine(targetDir, info.show, (sprintf "%s %dx%d %s" info.show info.season info.episodenumber info.episodename))
                                      File.Copy(decodedFile, targetFile)
                                      targetFile)
    }

    member this.ToJson() =
        let json = JObject()
        json.Add("id", JToken.FromObject(this.Id))
        json.Add("progress", JToken.FromObject(this.Progress))
        json.Add("currentstep", JToken.FromObject(this.CurrentStep))
        json.Add("done", JToken.FromObject(this.IsDone))

        json