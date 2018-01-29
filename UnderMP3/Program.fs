open System
open System.IO

let musicsFolder = "Musics"

// Create folder if not exists
Directory.CreateDirectory musicsFolder |> ignore

Console.ReadKey() |> ignore