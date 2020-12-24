namespace FarNet.FSharp
open FarNet
open FarNet.Forms
open System

(*
Examine job error dialogs. Titles must be exception short names.
Full names mean leaked exceptions handled and shown by the core.

Do not use PostStep(), wait for steps instead, when appropriate.
*)

[<AbstractClass; Sealed>]
type Job =
    /// Creates a job from the macro keys.
    static member Keys keys = async {
        do! Tasks.Keys keys |> Async.AwaitTask
    }

    /// Creates a job from the macro text.
    static member Macro text = async {
        do! Tasks.Macro text |> Async.AwaitTask
    }

    /// Waits for the predicate returning true.
    /// Returns true if this happens before the timeout.
    /// delay: Milliseconds to sleep before the first check.
    /// sleep: Milliseconds to sleep after the predicate returning false.
    /// timeout: Maximum waiting time in milliseconds, non positive ~ infinite.
    /// predicate: Returns true to stop waiting.
    static member Wait (delay, sleep, timeout, (predicate: unit -> bool)) = async {
        return! Tasks.Wait(delay, sleep, timeout, Func<bool>(predicate)) |> Async.AwaitTask
    }

    /// Posts the Far job for the function.
    /// f: The function invoked in the main Far thread.
    static member inline private PostJob f =
        far.PostJob (Action f)

    /// Creates a job from the callback, see Async.FromContinuations.
    static member inline FromContinuations f =
        Async.FromContinuations (fun (cont, econt, ccont) ->
            Job.PostJob (fun () -> f (cont, econt, ccont))
        )

    /// Posts an error dialog for the exception.
    /// The exception Name is shown in the title instead of FullName,
    /// in order to be slightly different from other FarNet errors.
    /// Full details are available as usual on [More].
    // _201221_2o This helps to reveal bugs. When our catch is not called
    // unexpectedly then the core error dialog is shown, a bit different.
    static member PostShowError (exn: exn) =
        let exn =
            match exn with
            | :? AggregateException as exn when exn.InnerExceptions.Count = 1 ->
                exn.InnerExceptions.[0];
            | _ ->
                exn;
        Job.PostJob (fun () -> far.ShowError (exn.GetType().Name, exn))

    static member private CatchShowError job = async {
        match! Async.Catch job with
        | Choice2Of2 exn -> Job.PostShowError exn
        | _ -> ()
    }

    /// Starts the job by Async.Start with exceptions caught and shown as dialogs.
    static member Start job =
        Async.Start <| Job.CatchShowError job

    /// Starts the job by Async.StartImmediate with exceptions caught and shown as dialogs.
    static member StartImmediate job =
        Async.StartImmediate <| Job.CatchShowError job

    /// Creates a job from the function dealing with Far.
    /// f: The function with any result.
    static member From f =
        Job.FromContinuations (fun (cont, econt, _) ->
            try
                use _std = new FarNet.FSharp.Works.FarStdWriter()
                cont (f ())
            with exn ->
                econt exn
        )

    /// The job to cancel the flow.
    static member Cancel () =
        Job.FromContinuations (fun (_, _, ccont) ->
            ccont (OperationCanceledException ())
        )

    /// Opens the editor and waits for its closing.
    static member FlowEditor (editor: IEditor) = async {
        let mutable closed = false
        do! Job.FromContinuations (fun (cont, econt, ccont) ->
            try
                editor.Closed.Add (fun _ -> closed <- true)
                editor.Open ()
                cont ()
            with exn ->
                econt exn
        )
        //! check for closed in a job
        match! Job.From (fun () -> if closed then None else Some (Async.AwaitEvent editor.Closed)) with
        | Some wait ->
            do! wait |> Async.Ignore
        | None ->
            ()
    }

    /// Opens the viewer and waits for its closing.
    static member FlowViewer (viewer: IViewer) = async {
        let mutable closed = false
        do! Job.FromContinuations (fun (cont, econt, ccont) ->
            try
                viewer.Closed.Add (fun _ -> closed <- true)
                viewer.Open ()
                cont ()
            with exn ->
                econt exn
        )
        //! check for closed in a job
        match! Job.From (fun () -> if closed then None else Some (Async.AwaitEvent viewer.Closed)) with
        | Some wait ->
            do! wait |> Async.Ignore
        | None ->
            ()
    }

    /// Waits until the modal state is over.
    static member SkipModal () = async {
        do! Job.Wait (0, 1000, 0, fun () -> not far.Window.IsModal) |> Async.Ignore
    }

    /// Opens the non modal dialog with the closing function.
    /// dialog: Dialog to open.
    /// closing: Function like Closing handler but with a result, normally an option:
    ///     if isNull args.Control then // canceled
    ///         None
    ///     elif ... then // do not close
    ///         args.Ignore <- true
    ///         None
    ///     else // some result
    ///         Some ...
    static member FlowDialog (dialog: IDialog, closing) =
        Job.FromContinuations (fun (cont, econt, _) ->
            dialog.Closing.Add (fun args ->
                try
                    let r = closing args
                    if not args.Ignore then
                        cont r
                with exn ->
                    if not args.Ignore then
                        econt exn
                    else
                        reraise ()
            )
            dialog.Open ()
        )

    /// Opens the panel and waits for its closing.
    static member FlowPanel (panel: Panel) = async {
        do! Job.OpenPanel panel
        do! Job.WaitPanelClosed panel
    }

    /// Opens the specified panel.
    static member OpenPanel (panel: Panel) = async {
        // open
        do! Job.From panel.Open

        // wait
        do! Works.Far2.Api.WaitSteps() |> Async.AwaitTask

        // test
        do! Job.From (fun () ->
            if far.Panel <> upcast panel then
                invalidOp "Panel was not opened."
        )
    }

    /// Calls the function which opens a panel and returns this panel.
    static member OpenPanel (f) = async {
        // open
        let! oldPanel = Job.From (fun () ->
            let panel = far.Panel
            f ()
            panel
        )

        // wait
        do! Works.Far2.Api.WaitSteps() |> Async.AwaitTask

        // test and return new panel
        return! Job.From (fun () ->
            let newPanel = far.Panel
            match newPanel with
            | :? Panel as panel when newPanel <> oldPanel ->
                // module panel different from old
                panel
            | _ ->
                // not module panel or old panel
                invalidOp "Panel was not opened."
        )
    }

    /// Waits for the specified panel to be closed.
    /// Does nothing if the panel is not opened (already closed).
    static member WaitPanelClosed (panel: Panel) = async {
        match! Job.From (fun () -> if far.Panel <> upcast panel && far.Panel2 <> upcast panel then None else Some (Async.AwaitEvent panel.Closed)) with
        | Some wait ->
            do! wait |> Async.Ignore
        | None ->
            ()
    }

    /// Waits for the panel to be closed with the specified closing handler.
    /// Returns the default if the panel is not opened (already closed).
    /// Other return values are provided by the closing.
    static member WaitPanelClosing (panel: Panel, closing: _ -> 'result) = async {
        let mutable res = Unchecked.defaultof<'result>
        panel.Closing.Add (fun args ->
            try
                let r = closing args
                if not args.Ignore then
                    res <- r
            with exn ->
                far.ShowError ("Closing function error", exn)
        )
        do! Job.WaitPanelClosed panel
        return res
    }

[<AutoOpen>]
module JobBuilder =
    /// Builds a job from a block.
    type JobBuilder () =
        inherit BlockBuilder ()
        member __.Run (f) =
            Job.From f

    /// Job helper: job {...} ~ Job.From (fun () -> ...)
    /// This shortcut is designed for very primitive code blocks.
    /// Use more effective Job.From for code with loops, try, use.
    let job = JobBuilder ()
