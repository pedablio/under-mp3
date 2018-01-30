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

let getLastItem =
    Array.rev >> Array.head

let singleSplit (text: string) (separator: string) =
    text.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

let trim (text: string) =
    text.Trim()

let getFileName (path, showExtension) =
    if showExtension then Path.GetFileName path
    else Path.GetFileNameWithoutExtension path

let getFileInfo filePath =
    let fileName = getFileName (filePath, false)
    let namePieces = singleSplit fileName " - "
    let titleAlbum = namePieces |> getLastItem |> trim
    let parentFolderName = (Directory.GetParent filePath).Name

    {
        Title = titleAlbum;
        Album = titleAlbum + " - Single";
        Artist = namePieces |> Array.head |> trim;
        Genre = parentFolderName;
    }

let isImage (path: string) =
    [".jpg"; ".png"] 
    |> List.map (fun ext -> path.EndsWith(ext))
    |> List.contains true

let makeSquareImage (imgPath: string) =
    let image = new Bitmap(imgPath)
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

            newImage.Save imgPath
            newImage.Dispose()
        else
            image.Dispose()
        
    imgPath

let getFilePicture filePath = 
    let fileName = getFileName (filePath, false)
    let genreFolderPath = (Directory.GetParent filePath).FullName

    let picture =
        Directory.GetFiles genreFolderPath
        |> Array.filter (fun file -> isImage file && getFileName (file, false) = fileName)
        |> Array.tryHead

    match picture with
    | Some imagePath ->
        let squareImagePath = makeSquareImage imagePath
        let albumCover = Id3v2.AttachedPictureFrame(Picture squareImagePath) :> IPicture
        albumCover.Type <- PictureType.FrontCover

        [| albumCover |]
    | _ -> [||]

let deleteFilePicture filePath =
    let fileName = getFileName (filePath, false)
    let genreFolderPath = (Directory.GetParent filePath).FullName
    let imagePath =
        Directory.GetFiles genreFolderPath
        |> Array.filter (fun file -> isImage file && getFileName (file, false) = fileName)
        |> Array.tryHead
    
    match imagePath with 
    | Some path -> File.Delete path
    | _ -> ()

let moveFile path buildPath  =
    let filename = getFileName (path, true)
    let movePath = Path.Combine (buildPath, filename)

    File.Move (path, movePath)

let MP3Files = 
    Directory.GetDirectories musicsFolder
    |> Array.collect (fun dir -> Directory.GetFiles (dir, "*.mp3"))

MP3Files 
|> Array.iter (fun filePath ->
    let fileInfo = getFileInfo filePath
    let pictures = getFilePicture filePath
    let file = File.Create filePath
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

    moveFile filePath buildFolder
    deleteFilePicture filePath
)