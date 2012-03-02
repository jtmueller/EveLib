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
            let wallet = wallets |> Seq.head
            Assert.Equal(subject.ItemId, wallet.AccountId)
        }