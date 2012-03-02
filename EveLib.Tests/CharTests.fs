namespace EveLib.Tests

open System
open EveLib
open Xunit

[<AbstractClass>]
type BaseCharTests(clientFactory: unit -> FSharp.IEveClient) =

    [<Fact>]
    let ``Can get account balance`` () =
        async {
            let client = clientFactory()
            let! charInfo = client.Eve.GetItemIds("Sigur Yassavi")
            let subject = charInfo |> Seq.head
            let! wallets = client.Character.GetAccountBalance(subject.ItemId)
            Assert.Equal(subject.ItemId, wallets.CharacterId)
            Assert.NotEmpty(wallets.Accounts)
        } |> Async.StartAsTask

type RavenCharTests() =
    inherit BaseCharTests(fun () -> upcast EveLib.RavenCache.RavenEveClient(apiKey))
