open System
open System.IO
open System.Drawing
open TagLib

let musicsFolder = "Musics"
let buildFolder = "Tagged"

type FileInfo = {
    Title: string
    Album: string
    Artist: string
    Genre: string
}

// Create folder if not exists
Directory.CreateDirectory musicsFolder |> ignore
Directory.CreateDirectory buildFolder |> ignore

let getFileInfo fileDir =
    let fileName = Path.GetFileNameWithoutExtension fileDir
    let namePieces = fileName.Split ([|" - "|], StringSplitOptions.RemoveEmptyEntries)
    let titleAlbum = (Array.head << Array.tail) namePieces
    let parentDir = Directory.GetParent fileDir

    {
        Title = titleAlbum;
        Album = titleAlbum + " - Single";
        Artist = Array.head namePieces;
        Genre = parentDir.Name;
    }

let isImage (s: string) =
    [".jpg"; ".png"] 
    |> List.map (fun ext -> s.EndsWith(ext))
    |> List.contains true

let cutImageInSquare (imgDir: string) =
    let image = new Bitmap(imgDir)
    let h = image.Height
    let w = image.Width

    if h <> w
        then 
            let minorSide = min h w
            let divider x =
               float (x - minorSide) * 0.5 |> int

            let section = Rectangle (divider w, divider h, minorSide, minorSide)
            let newImage = image.Clone(section, image.PixelFormat)

            image.Dispose()

            newImage.Save imgDir
            newImage.Dispose()
        else
            image.Dispose()
        
    imgDir

let getFilePicture fileDir = 
    let fileName = Path.GetFileNameWithoutExtension fileDir
    let genreFolder = (Directory.GetParent fileDir).FullName
    let pic =
        Directory.GetFiles genreFolder
        |> Array.filter (fun c -> isImage c && Path.GetFileNameWithoutExtension c = fileName)
        |> Array.tryHead

    match pic with
    | Some c ->
        let imgDir = cutImageInSquare c
        let albumCover = Id3v2.AttachedPictureFrame(Picture imgDir) :> IPicture
        albumCover.Type <- PictureType.FrontCover

        [| albumCover |]
    | _ -> [||]

let deleteFilePicture fileDir =
    let fileName = Path.GetFileNameWithoutExtension fileDir
    let genreFolder = (Directory.GetParent fileDir).FullName
    let pic =
        Directory.GetFiles genreFolder
        |> Array.filter (fun c -> isImage c && Path.GetFileNameWithoutExtension c = fileName)
        |> Array.tryHead
    
    match pic with 
    | Some p -> File.Delete p
    | _ -> ()

let moveFile dir buildDir  =
    let filename = Path.GetFileName dir
    let moveDir = Path.Combine (buildDir, filename)

    File.Move (dir, moveDir)

let MP3Files = 
    Directory.GetDirectories musicsFolder
    |> Array.collect (fun dir -> Directory.GetFiles (dir, "*.mp3"))


MP3Files |> Array.iter (fun fileDir ->
    let fileInfo = getFileInfo fileDir
    let pictures = getFilePicture fileDir
    let file = File.Create fileDir
    let artist = [| fileInfo.Artist |]

    file.Tag.Title <- fileInfo.Title
    file.Tag.Album <- fileInfo.Album
    file.Tag.Performers <- artist
    file.Tag.AlbumArtists <- artist
    file.Tag.Composers <- artist
    file.Tag.Genres <- artist
    file.Tag.Comment <- String.Empty
    file.Tag.Track <- 1u
    file.Tag.TrackCount <- 1u
    file.Tag.Year <- uint32 DateTime.Now.Year
    file.Tag.Pictures <- pictures

    file.Save()

    moveFile fileDir buildFolder
    deleteFilePicture fileDir
)