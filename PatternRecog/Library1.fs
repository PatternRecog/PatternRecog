namespace PatternRecog

open System
open System.IO
open System.Text.RegularExpressions
open Chessie.ErrorHandling

type ConfigType = { videosExts : string array
                    numberRegexes : string array
                    dryRun : bool }

module Config =
    let loadOrDefault() : ConfigType =
        { videosExts = [| ".avi"; ".mp4" |]
          numberRegexes = [| @"S(?<season>\d+)E(?<episode>\d+)"
                             @"(?<season>\d)(?<episode>\d\d)"
                             @"(?<season>\d\d?)x(?<episode>\d\d)" |]
          dryRun = false }

type Kind = Video | Subtitle

type DescType = { path : string
                  kind : Kind
                  season : int
                  episode : int }

module Desc =
    let parseNumber (s:string) (p:string) : (int * int) option =
        let m = Regex.Match(s, p, RegexOptions.IgnoreCase)
        if not m.Success then None
        else
            match Int32.TryParse m.Groups.["season"].Value,
                  Int32.TryParse(m.Groups.["episode"].Value) with
            | (true,s),(true,e) -> Some(s,e)
            | _ -> None

    let parse (c:ConfigType) (fullPath:string) : DescType option =
        let fileName = Path.GetFileNameWithoutExtension fullPath
        let ext = System.IO.Path.GetExtension(fullPath)
        let num = Array.tryPick (parseNumber fileName) c.numberRegexes
        match num with
        | None -> None
        | Some(ns,ne) ->
            { path = fullPath
              kind = if Array.contains ext c.videosExts then Kind.Video else Kind.Subtitle
              season = ns
              episode = ne } |> Some

    let fromDir (c:ConfigType) (recurse:bool) (dir:string) : string array =
        try
            Directory.EnumerateFiles(dir, "*", if recurse then SearchOption.AllDirectories else SearchOption.TopDirectoryOnly)
            |> Array.ofSeq
        with
        | e -> [||]

type MatchType(subtitle:DescType, video:DescType) =
    let getNewName subtitle =
        let ext = Path.GetExtension subtitle.path
        Path.ChangeExtension(video.path, ext)
    member val Checked = true with get, set
    member val Season = video.season with get
    member val Episode = video.episode with get
    member val VidFile = Path.GetFileName video.path with get
    member val SubFile = Path.GetFileName subtitle.path with get
    member val NewSubFile = getNewName subtitle with get
    member val VidPath = video.path with get
    member val SubPath = subtitle.path with get

module Matches =
    let findMatch h x =
        match h |> Map.tryFind (x.season,x.episode) with
        | Some sub -> Some <| MatchType(sub, x)
        | None -> None

    let fromDescs (c:ConfigType) (paths:DescType array) : MatchType array =
        let vids,subs = paths
                        |> Array.map (fun x -> (x.season,x.episode),x)
                        |> Array.partition (fun (_,desc) -> desc.kind = Kind.Video)
        let hsubs = Map.ofArray subs
        vids |> Array.map snd |> Array.choose (findMatch hsubs)

    let fromPaths (c:ConfigType) (paths:string array) : MatchType array =
        paths
        |> Array.choose (Desc.parse c)
        |> fromDescs c

    let renameMatch (c:ConfigType) (m:MatchType) : Result<unit,string> =
        try
            if not c.dryRun then
                File.Move(m.SubPath, m.NewSubFile)
            ok ()
        with
        | e -> fail(e.ToString())

    let rename (c:ConfigType) (matches:MatchType array) : Chessie.ErrorHandling.Result<unit,string> =
        matches
        |> Array.map (renameMatch c)
        |> collect
        |> bind (fun _ -> ok ())

    let print (m:MatchType) =
        sprintf "S%i E%i: %s %s" m.Season m.Episode m.VidPath m.SubPath
