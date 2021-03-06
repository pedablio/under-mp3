﻿namespace UnderMP3.AutoTag

open System
open System.IO
open System.Drawing
open TagLib

open UnderMP3.AutoTag.Helpers

module Program =
    let musicsFolder = "Musics"
    let buildFolder = "Tagged"

    type FileInfo = {
        Title: string
        Album: string
        Artists: string[]
        Genres: string[]
    }

    // Create folder if not exists
    Directory.CreateDirectory musicsFolder |> ignore
    Directory.CreateDirectory buildFolder |> ignore

    let getLastItem =
        Array.rev >> Array.head

    let getFileName (path: string, showExtension) =
        if showExtension then Path.GetFileName path
        else Path.GetFileNameWithoutExtension path

    let replaceGenre genre =
        match genre with
        | "R&B" -> "R&B/Soul"
        | "Hip Hop" -> "Hip Hop/Rap"
        | gen -> trim gen

    let getFileInfo filePath =
        let fileName = getFileName (filePath, false)
        let namePieces = singleSplit fileName " - " |> Array.map trim
        let titleAlbum = namePieces |> getLastItem
        let parentFolderName = (Directory.GetParent filePath).Name

        {
            Title = titleAlbum;
            Album = titleAlbum + " - Single";
            Artists = [| namePieces |> Array.head |];
            Genres = [| replaceGenre parentFolderName |];
        }

    let isImage path =
        [".jpg"; ".png"] 
        |> List.map (endsWith path)
        |> List.contains true

    let makeSquareImage imgPath =
        let image = createBitmap imgPath
        let h = image.Height
        let w = image.Width

        if h <> w
            then 
                let minorSide = min h w
                let divider x = (x - minorSide) / 2

                let section = Rectangle (divider w, divider h, minorSide, minorSide)
                let newImage = image.Clone (section, image.PixelFormat)

                image.Dispose()

                newImage.Save imgPath
                newImage.Dispose()
            else
                image.Dispose()
        
        imgPath

    let createCover imagePath =
        let squareImagePath = makeSquareImage imagePath
        let albumCover = Id3v2.AttachedPictureFrame(Picture squareImagePath) :> IPicture
        albumCover.Type <- PictureType.FrontCover

        albumCover

    let getFilePicture filePath = 
        let fileName = getFileName (filePath, false)
        let genreFolderPath = (Directory.GetParent filePath).FullName

        let picturePath =
            Directory.GetFiles genreFolderPath
            |> Array.filter (fun file -> isImage file && getFileName (file, false) = fileName)
            |> Array.tryHead

        match picturePath with
        | Some path -> [| createCover path |]
        | _ -> [||]

    let deleteFilePicture filePath =
        let fileName = getFileName (filePath, false)
        let genreFolderPath = (Directory.GetParent filePath).FullName
        let imagePath =
            Directory.GetFiles genreFolderPath
            |> Array.filter (fun file -> isImage file && getFileName (file, false) = fileName)
            |> Array.tryHead
    
        imagePath |> whenSome File.Delete |> ignore

    let moveFile path buildPath =
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

        file.Tag.Title <- fileInfo.Title
        file.Tag.Album <- fileInfo.Album
        file.Tag.Performers <- fileInfo.Artists
        file.Tag.AlbumArtists <- fileInfo.Artists
        file.Tag.Composers <- fileInfo.Artists
        file.Tag.Genres <- fileInfo.Genres
        file.Tag.Comment <- String.Empty
        file.Tag.Track <- 1u
        file.Tag.TrackCount <- 1u
        file.Tag.Year <- uint32 DateTime.Now.Year
        file.Tag.Pictures <- pictures

        file.Save()

        moveFile filePath buildFolder
        deleteFilePicture filePath
    )