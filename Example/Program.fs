module Program

open SimpleTests
open Microsoft.Testing.Platform.Builder
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions.TestFramework

[<EntryPoint>]
let main (args: string array) =
    task {
        let! builder = TestApplication.CreateBuilderAsync(args)

        builder.RegisterTestFramework(
            (fun _ -> SimpleCapabilities() :> ITestFrameworkCapabilities),
            (fun _ _ ->
                SimpleFramework(
                    "2 + 2 should equal 4",
                    (fun () ->
                        if 2 + 2 = 4 then Ok()
                        else Error "Expected 2 + 2 to equal 4")
                )
                :> ITestFramework)
        )
        |> ignore

        let! app = builder.BuildAsync()
        return! app.RunAsync()
    }
    |> fun t -> t.GetAwaiter().GetResult()