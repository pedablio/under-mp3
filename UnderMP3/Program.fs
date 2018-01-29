open System
open System.IO

let musicsFolder = "Musics"
let buildFolder = "Tagged"

// Create folder if not exists
Directory.CreateDirectory musicsFolder |> ignore
Directory.CreateDirectory buildFolder |> ignore

// Get MP3 Files
let files = 
    Directory.GetDirectories musicsFolder
    |> Array.collect Directory.GetFiles
    |> Array.filter (fun s -> s.EndsWith (".mp3", StringComparison.OrdinalIgnoreCase))

printfn "%A" files

Console.ReadKey() |> ignore