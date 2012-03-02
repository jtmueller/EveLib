module EveLib.Tests.BaseClientTests

    open System
    open EveLib
    open Xunit

    [<Fact>]
    let ``Async Get Characters`` () = 
        async {
            let client = EveClient.CreateFSharp apiKey
            let! charList = client.GetCharacters()
            Assert.NotEmpty charList.Characters
        } |> Async.StartAsTask


