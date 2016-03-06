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
          numberRegexes = [| @"S(?<season>\d+)E(?<episode>\d+)"; @"(?<season>\d)(?<episode>\d\d)" |]
          dryRun = false }

type Kind = Video | Subtitle

type DescType = { path : string
                  kind : Kind
                  season : int
                  episode : int }

module Desc =
    let parseNumber (s:string) (p:string) : (int * int) option =
        let m = Regex.Match(s, p)
        if not m.Success then None
        else
            match Int32.TryParse m.Groups.["season"].Value,
                  Int32.TryParse(m.Groups.["episode"].Value) with
            | (true,s),(true,e) -> Some(s,e)
            | _ -> None

    let parse (c:ConfigType) (s:string) : DescType option =
        let ext = System.IO.Path.GetExtension(s)
        let num = Array.tryPick (parseNumber s) c.numberRegexes
        match num with
        | None -> None
        | Some(ns,ne) ->
            { path = s
              kind = if Array.contains ext c.videosExts then Kind.Video else Kind.Subtitle
              season = ns
              episode = ne } |> Some

    let fromDir (c:ConfigType) (recursive:bool) (dir:string) : string array =
        try
            Directory.EnumerateFiles(dir, "*", if recursive then SearchOption.AllDirectories else SearchOption.TopDirectoryOnly)
            |> Array.ofSeq
        with
        | e -> [||]

type MatchType = { subtitle:DescType
                   video:DescType }

module Matches =
    let findMatch h x =
        match h |> Map.tryFind (x.season,x.episode) with
        | Some sub -> Some { subtitle=sub; video= x}
        | None -> None

    let fromPaths (c:ConfigType) (paths:string array) : MatchType array =
        let vids,subs = paths
                        |> Array.choose (Desc.parse c)
                        |> Array.map (fun x -> (x.season,x.episode),x)
                        |> Array.partition (fun (_,desc) -> desc.kind = Kind.Video)
        let hsubs = Map.ofArray subs
        vids |> Array.map snd |> Array.choose (findMatch hsubs)

    let renameMatch (c:ConfigType) (m:MatchType) : Result<string,string> =
        let ext = Path.GetExtension m.subtitle.path
        let newName = Path.ChangeExtension(m.video.path, ext)
        try
            if not c.dryRun then
                File.Move(m.subtitle.path, newName)
            ok newName
        with
        | e -> fail(e.ToString())

    let rename (c:ConfigType) (matches:MatchType array) : Chessie.ErrorHandling.Result<string list,string> =
        matches
        |> Array.map (renameMatch c)
        |> collect

    let print (m:MatchType) =
        sprintf "S%i E%i: %s %s" m.video.season m.video.episode m.video.path m.subtitle.path
