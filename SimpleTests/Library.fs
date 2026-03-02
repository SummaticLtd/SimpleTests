namespace SimpleTests

// StandardOutputProperty is marked [Experimental("TPEXP")] in Microsoft.Testing.Platform
#nowarn "57"

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading.Tasks
open Microsoft.Testing.Platform.Builder
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions
open Microsoft.Testing.Platform.Extensions.Messages
open Microsoft.Testing.Platform.Extensions.TestFramework
open Microsoft.Testing.Platform.Requests
open Microsoft.Testing.Extensions

// -- Capabilities (none needed for this demo) --
type SimpleCapabilities() =
    interface ITestFrameworkCapabilities with
        member _.Capabilities = [||]

// -- Test framework --
type SimpleFramework(testFolders: IReadOnlyCollection<TestFolder>, [<Struct>] ?oneTimeSetup: unit -> unit) as self =
    let assemblyName = Assembly.GetEntryAssembly().GetName().Name

    let methodIdentifier (ns: string, typeName: string, methodName: string) : TestMethodIdentifierProperty =
        TestMethodIdentifierProperty(assemblyName, ns, typeName, methodName, 0, [||], "System.Void")

    let fileLocation (filePath: string, lineNumber: int) : TestFileLocationProperty =
        let pos: LinePosition = LinePosition(lineNumber, 0)
        TestFileLocationProperty(filePath, LinePositionSpan(pos, pos))

    let makeTestNode (uid: string, displayName: string, properties: IProperty array) : TestNode =
        TestNode(Uid = TestNodeUid(uid), DisplayName = displayName, Properties = PropertyBag(properties))

    let captureOutput (action: unit -> 'a) : 'a * string =
        let original: TextWriter = Console.Out
        use writer: StringWriter = new StringWriter()
        Console.SetOut(writer)
        try
            let result: 'a = action ()
            Console.SetOut(original)
            (result, writer.ToString())
        with ex ->
            Console.SetOut(original)
            reraise ()

    let captureOutputAsync (action: unit -> Task<unit>) : Task<string> =
        task {
            let original: TextWriter = Console.Out
            use writer: StringWriter = new StringWriter()
            Console.SetOut(writer)
            try
                do! action ()
                Console.SetOut(original)
                return writer.ToString()
            with ex ->
                Console.SetOut(original)
                return raise ex
        }

    let buildProperties (stateProperty: IProperty, methodId: IProperty, loc: IProperty, output: string, elapsed: TimeSpan) : IProperty array =
        let timing: IProperty = TimingProperty(TimingInfo(DateTimeOffset.UtcNow - elapsed, DateTimeOffset.UtcNow, elapsed))
        if String.IsNullOrEmpty(output) then
            [| stateProperty; methodId; loc; timing |]
        else
            [| stateProperty; methodId; loc; timing; StandardOutputProperty(output) |]

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
                                let discoverCases(name: string, caseNames: IReadOnlyCollection<string>, filePath: string, lineNumber: int) : Task = task {
                                    for caseName in caseNames do
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        let node: TestNode = makeTestNode(uid, displayName, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name); fileLocation(filePath, lineNumber) |])
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                }
                                match test with
                                | Test.ѪSync(name, _, _, filePath, lineNumber) | Test.ѪAsync(name, _, _, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    let node: TestNode = makeTestNode(uid, name, [| DiscoveredTestNodeStateProperty(); methodIdentifier(ns, listName, name); fileLocation(filePath, lineNumber) |])
                                    let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                    do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync cases ->
                                    do! discoverCases(cases.Name, cases.Names, cases.FilePath, cases.LineNumber)
                                | Test.ICasesAsync cases ->
                                    do! discoverCases(cases.Name, cases.Names, cases.FilePath, cases.LineNumber)

                | :? RunTestExecutionRequest as request ->
                    let sessionUid = request.Session.SessionUid
                    let filterUids: HashSet<string> option =
                        match request.Filter with
                        | :? TestNodeUidListFilter as f ->
                            Some(HashSet(f.TestNodeUids |> Array.map (fun uid -> uid.Value)))
                        | _ -> None
                    let shouldRun (uid: string) : bool =
                        match filterUids with
                        | Some uids -> uids.Contains(uid)
                        | None -> true
                    oneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                    for folder in testFolders do
                        let ns = folder.NamespaceName
                        folder.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                        for testList in folder.TestLists do
                            let listName = testList.Name
                            testList.OneTimeSetup |> ValueOption.iter (fun setup -> setup ())
                            for test in testList.Tests do
                                match test with
                                | Test.ѪSync(name, run, ignored, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    if shouldRun uid then
                                        let sw: Stopwatch = Stopwatch.StartNew()
                                        let (stateProperty: IProperty), (output: string) =
                                            if ignored then
                                                (SkippedTestNodeStateProperty("Ignored") :> IProperty), ""
                                            else
                                                try
                                                    let ((), captured) = captureOutput run
                                                    (PassedTestNodeStateProperty(name) :> IProperty), captured
                                                with ex ->
                                                    (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), ""
                                        sw.Stop()
                                        let methodId: IProperty = methodIdentifier(ns, listName, name)
                                        let loc: IProperty = fileLocation(filePath, lineNumber)
                                        let node: TestNode = makeTestNode(uid, name, buildProperties(stateProperty, methodId, loc, output, sw.Elapsed))
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                | Test.ѪAsync(name, run, ignored, filePath, lineNumber) ->
                                    let uid: string = $"{ns}.{listName}.{name}"
                                    if shouldRun uid then
                                        let sw: Stopwatch = Stopwatch.StartNew()
                                        let! (stateProperty: IProperty), (output: string) =
                                            task {
                                                if ignored then
                                                    return (SkippedTestNodeStateProperty("Ignored") :> IProperty), ""
                                                else
                                                    try
                                                        let! captured: string = captureOutputAsync run
                                                        return (PassedTestNodeStateProperty(name) :> IProperty), captured
                                                    with ex ->
                                                        return (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), ""
                                            }
                                        sw.Stop()
                                        let methodId: IProperty = methodIdentifier(ns, listName, name)
                                        let loc: IProperty = fileLocation(filePath, lineNumber)
                                        let node: TestNode = makeTestNode(uid, name, buildProperties(stateProperty, methodId, loc, output, sw.Elapsed))
                                        let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                        do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesSync cases ->
                                    let name: string = cases.Name
                                    let loc: IProperty = fileLocation(cases.FilePath, cases.LineNumber) :> IProperty
                                    let methodId: IProperty = methodIdentifier(ns, listName, name)
                                    for caseName, run in cases.Runs do
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        if shouldRun uid then
                                            let sw: Stopwatch = Stopwatch.StartNew()
                                            let (stateProperty: IProperty), (output: string) =
                                                try
                                                    let ((), captured) = captureOutput run
                                                    (PassedTestNodeStateProperty(displayName) :> IProperty), captured
                                                with ex ->
                                                    (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), ""
                                            sw.Stop()
                                            let node: TestNode = makeTestNode(uid, displayName, buildProperties(stateProperty, methodId, loc, output, sw.Elapsed))
                                            let message: TestNodeUpdateMessage = TestNodeUpdateMessage(sessionUid, node)
                                            do! context.MessageBus.PublishAsync(self, message)
                                | Test.ICasesAsync cases ->
                                    let name: string = cases.Name
                                    let loc: IProperty = fileLocation(cases.FilePath, cases.LineNumber) :> IProperty
                                    let methodId: IProperty = methodIdentifier(ns, listName, name)
                                    for caseName, run in cases.Runs do
                                        let displayName: string = $"{name}({caseName})"
                                        let uid: string = $"{ns}.{listName}.{displayName}"
                                        if shouldRun uid then
                                            let sw: Stopwatch = Stopwatch.StartNew()
                                            let! (stateProperty: IProperty), (output: string) =
                                                task {
                                                    try
                                                        let! captured: string = captureOutputAsync run
                                                        return (PassedTestNodeStateProperty(displayName) :> IProperty), captured
                                                    with ex ->
                                                        return (FailedTestNodeStateProperty(ex, ex.Message) :> IProperty), ""
                                                }
                                            sw.Stop()
                                            let node: TestNode = makeTestNode(uid, displayName, buildProperties(stateProperty, methodId, loc, output, sw.Elapsed))
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

            builder.AddTrxReportProvider() |> ignore

            let! app: ITestApplication = builder.BuildAsync()
            return! app.RunAsync()
        }
        |> fun t -> t.GetAwaiter().GetResult()