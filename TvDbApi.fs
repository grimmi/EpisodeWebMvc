namespace EpisodeWebMvc

open ConfigReader
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text

type Episode = { airedEpisodeNumber: int; airedSeason: int; episodeName: string; firstAired: string }

type Show = { seriesName: string; id: int; }

type TvDbApi() =

    let mutable loggedIn = false
    let mutable apiClient: HttpClient = null
    let apiUrl = "https://api.thetvdb.com"

    let getCredentialsBody =
        let cfg = ReadConfigToDict "./tvdb.cfg"
        let body = JObject()
        body.Add("apikey", JToken.FromObject(cfg.["apikey"]))
        body.Add("username", JToken.FromObject(cfg.["username"]))
        body.Add("userkey", JToken.FromObject(cfg.["userkey"]))
        body.ToString()
    let login =
        let token = async{
            use loginClient = new HttpClient()
            let body = getCredentialsBody
            let request = new HttpRequestMessage(HttpMethod.Post, (apiUrl + "/login"))
            request.Content <- new StringContent(body, Encoding.UTF8, "application/json")
            let! response = (loginClient.SendAsync(request) |> Async.AwaitTask)
            let! responseString = (response.Content.ReadAsStringAsync() |> Async.AwaitTask)
            let jsonToken = JObject.Parse(responseString)
            return jsonToken.["token"] |> string } |> Async.RunSynchronously
        apiClient <- new HttpClient()
        apiClient.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token)
        apiClient.DefaultRequestHeaders.Add("Accept-Language", "en") |> ignore
        loggedIn <- true

    let deserializeEpisode (line:string) =
        match line.Split([|"***"|], StringSplitOptions.RemoveEmptyEntries) with
        |[|season;epno;aired;epname|] -> Some { airedSeason = (season |> int); airedEpisodeNumber = (epno |> int); firstAired = aired; episodeName = epname }
        |_ -> None

    let serializeEpisode episode = 
        sprintf "%d *** %d *** %s *** %s" episode.airedSeason episode.airedEpisodeNumber episode.firstAired episode.episodeName
    
    let cacheEpisodes episodes showId= 
        if not (Directory.Exists "./showcache") then
            Directory.CreateDirectory("./showcache") |> ignore

        let cachePath = sprintf "./showcache/%d.cache" showId
        File.WriteAllLines(cachePath, episodes |> Seq.map serializeEpisode, Encoding.UTF8)

    let loadEpisodesFromCache showId =
        let cachePath = sprintf "./showcache/%d.cache" showId
        match File.Exists cachePath with
        |true -> Some(File.ReadAllLines cachePath
                      |> Seq.choose deserializeEpisode)
        |_ -> None

    member private this.LoadEpisodesFromApi(showId) = async {

        let getEpisodePage showId page : Async<JObject>= async{
            let! response = this.GetAsync(sprintf "/series/%d/episodes?page=%d" showId page)
            return response
        }

        let! firstPage = getEpisodePage showId 1
        let lastPageNo = firstPage.["links"].Value<int>("last")

        let pages = if lastPageNo > 1 then
                        [ 2 .. lastPageNo ]
                        |> Seq.map(fun p -> async { let! pageResult = getEpisodePage showId p
                                                    return pageResult } |> Async.RunSynchronously)
                        |> List.ofSeq
                        |> List.append [firstPage]  
                    else
                        [firstPage]

        return pages
        |> Seq.collect(fun p -> p.["data"] |> Seq.choose(fun e -> 
                                                        try
                                                            Some(JsonConvert.DeserializeObject<Episode>(e.ToString()))
                                                        with
                                                            | :? Exception as ex -> None))
                                                            
        |> Seq.sortBy(fun ep -> (ep.airedSeason, ep.airedEpisodeNumber))
    }

    member this.GetAsync uri = 
        let rec get retry = async{
            if not loggedIn then
                login
            let request = new HttpRequestMessage(HttpMethod.Get, Uri(apiUrl + uri))
            let! response = (apiClient.SendAsync(request) |> Async.AwaitTask)
            if response.StatusCode = HttpStatusCode.Unauthorized && retry then
                login
                let! result = get false
                return result
            else 
                let! responseString = (response.Content.ReadAsStringAsync() |> Async.AwaitTask)
                return JObject.Parse(responseString)
        }

        get true

    member this.SearchShow show = async{
        let! response = this.GetAsync("/search/series?name=" + show)
        let dbShows = JsonConvert.DeserializeObject<seq<Show>>(response.["data"].ToString())
        return dbShows
    }

    member this.GetEpisodes showId = async{
        let cachedEpisodes = loadEpisodesFromCache showId
        match cachedEpisodes with
        |Some episodes -> return episodes
        |None -> let! apiEpisodes = this.LoadEpisodesFromApi(showId)
                 cacheEpisodes apiEpisodes showId
                 return apiEpisodes
    }