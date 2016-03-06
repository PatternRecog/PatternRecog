// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open PatternRecog
open Nessos.UnionArgParser

type Arguments =
    | DryRun
//    | Dir of string[]
    | Log_Level of int
    | Dir of string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | DryRun -> "DryRun"
            | _ -> sprintf "%A" s
[<EntryPoint>]
let main argv =
    let parser = UnionArgParser<Arguments>()
    let parsed = parser.ParseCommandLine(argv);
    let c:ConfigType = { Config.loadOrDefault() with dryRun = parsed.Contains <@ DryRun @> }
    printfn "%A" c
//    printfn "%A" argv
//    printfn "%A" parsed
    let dirs = parsed.GetResults <@ Dir @> |> Array.ofList
//    printfn "%A" dirs
    let descs = dirs |> Array.collect (Desc.fromDir c false)
//    descs |> Array.iter (printfn "%A")
    let matches = descs |> Matches.fromPaths c
    matches |> Array.iter ((printfn "%s") << Matches.print)
    let mutable line = ""
    while line <> "q" do
        match line with
        | "y" -> Matches.rename c matches |> printfn "%A"
        | _ -> ()
        printfn "y to proceed"
        printfn "q exit"
        line <- System.Console.ReadLine()
//    printfn parser.
    0 // return an integer exit code
