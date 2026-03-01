namespace SimpleTests

open System.Threading.Tasks
open Microsoft.Testing.Platform.Capabilities.TestFramework
open Microsoft.Testing.Platform.Extensions
open Microsoft.Testing.Platform.Extensions.Messages
open Microsoft.Testing.Platform.Extensions.TestFramework
open Microsoft.Testing.Platform.Requests

// -- Capabilities (none needed for this demo) --
type SimpleCapabilities() =
    interface ITestFrameworkCapabilities with
        member _.Capabilities = [||]

// -- Test framework that runs a single test --
type SimpleFramework(testUid: string, testName: string, runTest: unit -> Result<unit, string>) as self =
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
            Task.FromResult(CreateTestSessionResult())

        member _.CloseTestSessionAsync(_context) =
            Task.FromResult(CloseTestSessionResult())

        member _.ExecuteRequestAsync(context) =
            task {
                match context.Request with
                | :? DiscoverTestExecutionRequest as request ->
                    let testNode =
                        TestNode(
                            Uid = TestNodeUid(testUid),
                            DisplayName = testName,
                            Properties = PropertyBag(DiscoveredTestNodeStateProperty())
                        )

                    let message =
                        TestNodeUpdateMessage(request.Session.SessionUid, testNode)

                    do! context.MessageBus.PublishAsync(self, message)

                | :? RunTestExecutionRequest as request ->
                    let result = runTest ()

                    let stateProperty: IProperty =
                        match result with
                        | Ok () -> PassedTestNodeStateProperty(testName) :> IProperty
                        | Error msg -> FailedTestNodeStateProperty(msg) :> IProperty

                    let testNode =
                        TestNode(
                            Uid = TestNodeUid(testUid),
                            DisplayName = testName,
                            Properties = PropertyBag(stateProperty)
                        )

                    let message =
                        TestNodeUpdateMessage(request.Session.SessionUid, testNode)

                    do! context.MessageBus.PublishAsync(self, message)
                | _ -> ()

                context.Complete()
            }
