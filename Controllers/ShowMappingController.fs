namespace EpisodeWebMvc.Controllers

open Microsoft.AspNetCore.Mvc
open System
open System.IO

[<Route("api/showmapping")>]
type ShowMappingController() =
    inherit Controller()
    let showMappings = 
        File.ReadAllLines("./shows.map")
        |> Seq.map(fun (l:string) -> match l.Split("***", StringSplitOptions.RemoveEmptyEntries) with
                                     |[|parsed; mapped; id|] -> (parsed,mapped,id)
                                     |_ -> ("","",""))


    [<HttpPost>]
    member this.Post(parsed: string, mapped: string) = ()