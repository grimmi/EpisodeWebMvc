module ConfigReader

open System.Collections.Generic
open System.IO

let ReadConfigToDict file =
    let config = File.ReadAllLines file
                 |> Seq.choose(fun l -> match l.Split('=') with
                                        |[|key;value|] -> Some (key, value)
                                        |_ -> None)
                 |> dict
    config