
/// A Redux IAction containing a value - intended for discriminated unions
type FAction<'a>(value) =
    member val Value: 'a = value
    interface Redux.IAction

/// Takes a F# function and return a wrapped Func applying this function to the FAction Value
let reducerBoilerplate (reducer:'a->'b->'a) : Redux.Reducer<'a> =
    let inner  state (action:Redux.IAction) =
        match action with
        | :? FAction<_> as faction -> reducer state faction.Value
        | _ -> state
    Redux.Reducer<'a>(inner)

// View (could use FsXaml instead, done by hand for brevity)
open System
open System.Windows
open System.Windows.Controls
open MahApps.Metro.Controls
open PatternRecog.GUI.Views

// Action and reducer
type CounterAction = Inc | Dec
let reducer state value =
    match value with
    | Inc -> state + 1
    | Dec -> state - 1


[<EntryPoint>]
[<STAThread>]
let main argv =
    let store = Redux.Store((reducerBoilerplate reducer), 0)
    let w = MainView()

    // View-action wiring
    store.Add(fun i -> w.CounterRun.Text <- i.ToString())
    w.AddButton.Click.Add (fun _ -> FAction Inc |> store.Dispatch |> ignore)
    w.SubButton.Click.Add (fun _ -> FAction Dec |> store.Dispatch |> ignore)

    let app = new Application()
    app.Run(w)