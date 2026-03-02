namespace SimpleTests

open System.Collections.Generic
open System.Reflection
open System.Threading.Tasks
open Microsoft.Testing.Platform.Builder
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions
open Microsoft.Testing.Platform.Extensions.Messages
open Microsoft.Testing.Platform.Extensions.TestFramework
open Microsoft.Testing.Platform.Requests

// -- Capabilities (none needed for this demo) --
type SimpleCapabilities() =
    interface ITestFrameworkCapabilities with
        member _.Capabilities = [||]

// -- Test framework --
type SimpleFramework(testFolders: IReadOnlyCollection<TestFolder>, [<Struct>] ?oneTimeSetup: unit -> unit) as self =
    let assemblyName = Assembly.GetEntryAssembly().GetName().Name

    let methodIdentifier (ns: string, typeName: string, methodName: string) : TestMethodIdentifierProperty =
        TestMethodIdentifierProperty(assemblyName, ns, typeName, methodName, 0, [||], "System.Void")

    let makeTestNode (uid: string, displayName: string, properties: IProperty array) : TestNode =
        TestNode(Uid = TestNodeUid(uid), DisplayName = displayName, Properties = PropertyBag(properties))

    interface IExtension with
        member _.Uid = "SimpleFramework"
        member _.Version = "1.0.0"
        member _.DisplayName = "Simple Framework"
        member _.Description = "Minimal direct Microsoft.Testing.Platform demo"
        member _.IsEnabledAsync() = Task.FromResult(true)

    interface IDataProducer with
        member _.DataTypesProduced = [| typeof<TestNodeUpdateMessage> |]

    interface ITestFramework with
        member _.CreateTestSessionAsync(_context) =
            Task.FromResult(CreateTestSessionResult(IsSuccess = true))

        member _.CloseTestSessionAsync(_context) =
            Task.FromResult(CloseTestSessionResult(IsSuccess = true))

        member _.ExecuteRequestAsync(context) =
            task {
                match context.Request with
                | :? DiscoverTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            for test in testList.Tests do
                                let discoverCases(name: string, caseNames: IReadOnlyCollection<string>) : Task = task {
                                    for caseName in caseNames do
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                }
                                match test with
                                | Test.ѪSync(name, _, _) | Test.ѪAsync(name, _, _) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync cases ->
                                    do! discoverCases(cases.Name, cases.Names)
                                | Test.ICasesAsync cases ->
                                    do! discoverCases(cases.Name, cases.Names)

                | :? RunTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    oneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        folder.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            testList.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                            for test in testList.Tests do
                                match test with
                                | Test.ѪSync(name, run, ignored) ->
                                    let stateProperty: IProperty =
                                        if ignored then
                                            SkippedTestNodeStateProperty("Ignored")
                                        else
                                            try
                                                run ()
                                                PassedTestNodeStateProperty(name)
                                            with ex ->
                                                FailedTestNodeStateProperty(ex, ex.Message)
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.ѪAsync(name, run, ignored) ->
                                    let! stateProperty: IProperty =
                                        task {
                                            if ignored then
                                                return SkippedTestNodeStateProperty("Ignored") :> IProperty
                                            else
                                                try
                                                    do! run ()
                                                    return PassedTestNodeStateProperty(name) :> IProperty
                                                with ex ->
                                                    return FailedTestNodeStateProperty(ex, ex.Message) :> IProperty
                                        }
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync cases ->
                                    let name: string = cases.Name
                                    for caseName, run in cases.Runs do
                                        let displayName: string = $"{name}({caseName})"
                                        let stateProperty: IProperty =
                                            try
                                                run ()
                                                PassedTestNodeStateProperty(displayName)
                                            with ex ->
                                                FailedTestNodeStateProperty(ex, ex.Message)
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesAsync cases ->
                                    let name: string = cases.Name
                                    for caseName, run in cases.Runs do
                                        let displayName: string = $"{name}({caseName})"
                                        let! stateProperty: IProperty =
                                            task {
                                                try
                                                    do! run ()
                                                    return PassedTestNodeStateProperty(displayName) :> IProperty
                                                with ex ->
                                                    return FailedTestNodeStateProperty(ex, ex.Message) :> IProperty
                                            }
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| stateProperty; methodIdentifier(ns, listName, name) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                | _ -> ()

                context.Complete()
            }

type Runner =
    static member Run(args: string array, testFolders: IReadOnlyCollection<TestFolder>, ?oneTimeSetup: unit -> unit) : int =
        task {
            let! builder: ITestApplicationBuilder = TestApplication.CreateBuilderAsync(args)

            builder.RegisterTestFramework(
                (fun _ -> SimpleCapabilities() :> ITestFrameworkCapabilities),
                (fun _ _ ->
                    match oneTimeSetup with
                    | Some setup -> SimpleFramework(testFolders, oneTimeSetup = setup) :> ITestFramework
                    | None -> SimpleFramework(testFolders) :> ITestFramework)
            )
            |> ignore

            let! app: ITestApplication = builder.BuildAsync()
            return! app.RunAsync()
        }
        |> fun t -> t.GetAwaiter().GetResult()