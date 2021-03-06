﻿namespace EveLib.Tests

open System
open EveLib
open Xunit

[<AbstractClass>]
type BaseCharTests(clientFactory: unit -> FSharp.IEveClient) =

    [<Fact>]
    let ``Can get account balance`` () =
        async {
            let client = clientFactory()
            let! charInfo = client.GetCharacters()
            let subject = charInfo.Characters |> Seq.head
            let! wallets = client.Character.GetAccountBalance(subject.Id)
            Assert.NotEmpty(wallets.Accounts)
            Assert.Equal(subject.Id, wallets.Id)
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get mail headers`` () =
        async {
            let client = clientFactory()
            let! charInfo = client.GetCharacters()
            let subject = charInfo.Characters |> Seq.head
            let! mailHeaders = client.Character.GetMailHeaders(subject.Id)
            Assert.NotEmpty(mailHeaders)
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get mail bodies`` () =
        async {
            let client = clientFactory()
            let! charInfo = client.GetCharacters()
            let subject = charInfo.Characters |> Seq.head
            let! mailHeaders = client.Character.GetMailHeaders(subject.Id)
            let mailIds = mailHeaders |> Seq.map (fun h -> h.Id) |> Array.ofSeq
            let! mailBodies = client.Character.GetMailBodies(subject.Id, mailIds)
            Assert.NotEmpty(mailBodies)
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get all character sheets`` () =
        async {
            let client = clientFactory()
            let! charInfo = client.GetCharacters()

            let! charSheets =
                charInfo.Characters
                |> Seq.map (fun c -> client.Character.GetCharacterSheet(c.Id))
                |> Async.Parallel

            Assert.NotEmpty(charSheets)
            for cs in charSheets do
                Assert.NotNull(cs.CharacterName)
                Assert.NotEqual<string>("", cs.CharacterName)
        } |> Async.StartAsTask

type RavenCharTests() =
    inherit BaseCharTests(fun () -> upcast EveLib.RavenCache.RavenEveClient(apiKey))
