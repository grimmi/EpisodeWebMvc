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

    let makeFileName info =
        let invalidChars = Array.concat[|Path.GetInvalidFileNameChars(); Path.GetInvalidPathChars()|]
        let isValid c = not (Seq.exists(fun ch -> ch = c) invalidChars)
        let cleanShow = info.show |> String.filter isValid
        let cleanEpisode = info.episodename |> String.filter isValid

        sprintf "%s %dx%d %s" cleanShow info.season info.episodenumber cleanEpisode
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
        let targetPath = ReadConfigToDict("./otr.cfg").["keyfilepath"]

        infos
        |> List.ofSeq
        |> List.iter(fun info -> let decoder = OtrFileDecoder()
                                 this.CurrentStep <- info.file
                                 this.ProgValue <- 20.
                                 let decodedFile = decoder.DecodeFile (Path.Combine(targetPath,info.file)) getOptions
                                 this.ProgValue <- 80.
                                 let targetDir = ReadConfigToDict("./otr.cfg").["targetpath"]
                                 if not (Directory.Exists targetDir) then
                                    (Directory.CreateDirectory targetDir) |> ignore
                                 let targetFile = Path.Combine(targetDir, info.show, (makeFileName info))
                                 printfn "targetfile: %s" targetFile
                                 if not (Directory.Exists(Path.Combine(targetDir, info.show))) then
                                    Directory.CreateDirectory(Path.Combine(targetDir, info.show)) |> ignore
                                 this.ProgValue <- 100.
                                 File.Copy(decodedFile, (targetFile + ".avi"))
                                 File.Move(Path.Combine(targetPath, info.file), Path.Combine(targetPath, "processed", info.file))
        )
        |> ignore
        this.IsDone <- true
    }

    member this.ToJson() =
        let json = JObject()
        json.Add("id", JToken.FromObject(this.Id))
        json.Add("progress", JToken.FromObject(this.Progress))
        json.Add("currentstep", JToken.FromObject(this.CurrentStep))
        json.Add("done", JToken.FromObject(this.IsDone))

        json