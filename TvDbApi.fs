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
        |[|season;epno;aired;epname|] -> Some { airedSeason = (season |> int); airedEpisodeNumber = (epno |> int); firstAired = aired.Trim(); episodeName = epname.Trim() }
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

    let loadShowMappings = 
        File.ReadAllLines("./shows.map")
        |> Array.choose(fun (l:string) -> match l.Split("***", StringSplitOptions.RemoveEmptyEntries) with
                                          |[|parsed; mapped; id|] -> Some (parsed.Trim(),mapped.Trim(),id.Trim())
                                          |_ -> None)

    let cacheShow parsed mapped id =
        File.AppendAllText("./shows.map", (sprintf "%s%s *** %s *** %d" Environment.NewLine parsed mapped id))

    let getShowFromMap showName =
        let mappings = loadShowMappings
                       |> Seq.filter(fun (parsed, name, id) -> parsed = showName)

        match mappings |> Seq.length with
        |1 -> let (p, n, i) = Seq.head mappings
              Some { seriesName = n; id = (i |> int) }
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
            match (response.StatusCode, retry) with
            |(HttpStatusCode.Unauthorized, true) -> login
                                                    let! result = get false
                                                    return result
            |(HttpStatusCode.OK, _) -> let! responseString = (response.Content.ReadAsStringAsync() |> Async.AwaitTask)
                                       return JObject.Parse(responseString)
            |_ -> let errResponse = JObject()
                  errResponse.Add("code", JToken.FromObject(response.StatusCode))
                  errResponse.Add("data", JToken.FromObject([||]))
                  return errResponse
        }

        get true

    member this.SearchShow show = async{
        let mappedShow = getShowFromMap show
        match mappedShow with
        |Some s -> return [s] |> Seq.ofList
        |None -> let! response = this.GetAsync("/search/series?name=" + show)
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

    member this.CacheShow parsed show =
        let cachedShows = loadShowMappings
        if cachedShows |> Seq.exists(fun (p, n, id) -> p = parsed && n = show.seriesName && id = (show.id |> string)) then
            ()
        else
            cacheShow parsed show.seriesName show.id