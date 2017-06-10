namespace EpisodeWebMvc

open ConfigReader
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System
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
        loggedIn <- true

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
        return response
    }
