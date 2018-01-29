open System
open System.IO

let musicsFolder = "Musics"
let buildFolder = "Tagged"

// Create folder if not exists
Directory.CreateDirectory musicsFolder |> ignore
Directory.CreateDirectory buildFolder |> ignore

Console.ReadKey() |> ignore