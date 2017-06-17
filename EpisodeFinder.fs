module EpisodeFinder

open EpisodeWebMvc
open System
open System.Text.RegularExpressions

let beforeDatePattern = @".+?(?=_\d\d.\d\d.\d\d)"
let datePattern = @"(\d\d.\d\d.\d\d)"
    
let canonizeEpisodeName (name:string) =
    name.ToLower()
    |> String.filter Char.IsLetter

let getEpisodeInfo (file:string) episodeShow (api:TvDbApi) = 
        let getEpisode = async{
            match episodeShow with
            | None -> return None
            | Some show -> 
                if file.Contains("__") then
                    let episodePart = file.Split([|"__"|], StringSplitOptions.RemoveEmptyEntries).[1]
                    let episodeNameMatch = Regex.Match(episodePart, beforeDatePattern)
                    let episodeName = episodeNameMatch.Value
                    let! showEpisodes = api.GetEpisodes show.id 
                    let foundEpisode = showEpisodes
                                       |> Seq.tryFind(fun ep -> (ep.episodeName |> canonizeEpisodeName) = (episodeName |> canonizeEpisodeName))
                    return foundEpisode
                else
                    let aired = match Regex.Match(file, datePattern).Value.Split('.') with
                                |[|yy;mm;dd|] -> sprintf "20%s-%s-%s" yy mm dd
                                |_ -> ""
                    if aired = "" then
                        return None
                    else
                        let! showEpisodes = api.GetEpisodes show.id
                        let foundEpisode = showEpisodes
                                           |> Seq.tryFind(fun ep -> ep.firstAired = aired)
                        return foundEpisode
        }
        (getEpisode |> Async.RunSynchronously)