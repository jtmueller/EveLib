namespace EveLib.Tests

open System
open EveLib
open Xunit

[<AbstractClass>]
type BaseClientTests(clientFactory: unit -> FSharp.IEveClient) =

    [<Fact>]
    let ``Can get characters`` () = 
        async {
            let client = clientFactory()
            let! charList = client.GetCharacters()
            Assert.NotEmpty charList.Characters
            let subject = charList.Characters |> Seq.last
            Assert.False(String.IsNullOrEmpty(subject.Name))
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get server status`` () =
        async {
            let client = clientFactory()
            let! status = client.GetServerStatus()
            Assert.True(status.OnlinePlayers > 0)
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get recent kills`` () =
        async {
            let client = clientFactory()
            let! kills = client.Map.GetRecentKills()
            Assert.NotEmpty(kills)
            Assert.True(kills |> Seq.length > 128)
        } |> Async.StartAsTask


type RavenClientTests() =
    inherit BaseClientTests(fun () -> upcast EveLib.RavenCache.RavenEveClient(apiKey))
