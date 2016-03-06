
/// A Redux IAction containing a value - intended for discriminated unions
type FAction<'a>(value) =
    member val Value: 'a = value
    interface Redux.IAction

let dispatch (store:Redux.Store<_>) action = action |> FAction |> store.Dispatch |> ignore

/// Takes a F# function and return a wrapped Func applying this function to the FAction Value
let reducerBoilerplate (reducer:'a->'b->'a) : Redux.Reducer<'a> =
    let inner  state (action:Redux.IAction) =
        match action with
        | :? FAction<_> as faction -> reducer state faction.Value
        | _ -> state
    Redux.Reducer<'a>(inner)

open System
open System.IO
open System.Windows
open System.Windows.Controls
open MahApps.Metro.Controls
open PatternRecog
open PatternRecog.GUI.Views

type Model = { config: ConfigType
               descs: DescType array
               matches: MatchType array }

// Action and reducer
type Action =
| AddPaths of string array
| Clear
| SetAllChecked of bool
| DoRename

let reducer state action =
    match action with
    | AddPaths paths ->
        let newPaths = paths |> Array.collect (fun p ->
            let attr = File.GetAttributes p
            if attr.HasFlag(FileAttributes.Directory) then Desc.fromDir state.config false p
            else [|p|])
        let newDescs = newPaths |> Array.choose (Desc.parse state.config)
        let descs = (Array.append state.descs newDescs)
        { state with descs = descs
                     matches = Matches.fromDescs state.config descs }
    | DoRename ->
        let renResult = state.matches |> Array.filter (fun x -> x.Checked) |> Matches.rename state.config
        state
    | Clear -> { state with descs = [||]
                            matches = [||] }
    | SetAllChecked isChecked -> { state with matches = state.matches |> Array.map (fun m -> m.Checked <- isChecked; m) }

let view (w:MainView) (state:Model) =
    w.DataGrid.ItemsSource <- state.matches |> Seq.ofArray |> System.Collections.Generic.List


[<EntryPoint>]
[<STAThread>]
let main argv =
    let state = { config = Config.loadOrDefault()
                  descs = [||]
                  matches = [||] }
    let store = Redux.Store((reducerBoilerplate reducer), state)
    let w = MainView()
    // View-action wiring
    store.Add(view w)

    w.BtnRename.Click.Add (fun _ -> DoRename |> dispatch store)
    w.BtnClear.Click.Add (fun _ -> Clear |> dispatch store)
    w.BtnSelectAll.Click.Add (fun _ -> SetAllChecked true |> dispatch store)
    w.BtnDeselectAll.Click.Add (fun _ -> SetAllChecked false |> dispatch store)

    w.DataGrid.Drop.Add (fun args ->
        if args.Data.GetDataPresent(DataFormats.FileDrop)
        then AddPaths (args.Data.GetData(DataFormats.FileDrop) :?> string[]) |> dispatch store )

    let app = new Application()
//    app.Startup.Add (fun args -> AddPaths argv |> dispatch store)
    app.Run(w)