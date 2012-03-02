namespace EveLib.Tests

open System
open EveLib
open Xunit

[<AbstractClass>]
type BaseClientTests(clientFactory: unit -> FSharp.IEveClient) =

    [<Fact>]
    let ``Can Get Characters`` () = 
        async {
            let client = clientFactory()
            let! charList = client.GetCharacters()
            Assert.NotEmpty charList.Characters
            let subject = charList.Characters |> Seq.last
            Assert.False(String.IsNullOrEmpty(subject.Name))
        } |> Async.StartAsTask

    [<Fact>]
    let ``Can get recent kills`` () =
        async {
            let client = clientFactory()
            let! kills = client.Map.GetRecentKills()
            Assert.NotEmpty(kills.SolarSystems)
        }