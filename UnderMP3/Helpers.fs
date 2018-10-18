namespace UnderMP3.AutoTag

open System
open System.Drawing

module Helpers =
    let whenSome f = function
        | None -> None
        | Some x -> Some <| f x

    let singleSplit (text: string) (separator: string) =
        text.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

    let trim (text: string) =
        text.Trim()
    
    let endsWith (text: string) (search: string) =
        text.EndsWith(search)

    let createBitmap (path: string) =
        new Bitmap(path)