module EveLib.Tests.BaseClientTests

    open System
    open EveLib
    open Xunit
    open Swensen.Unquote

    [<Fact>]
    let ``Async Get Characters`` () = 
        async {
            let client = EveClient.CreateFSharp apiKey
            let! charList = client.GetCharacters()
            Assert.True(charList.Characters |> Seq.length > 0)
        } |> Async.StartAsTask

    [<Fact>]
    let ``Sync GetCharacters`` () =
        let client = EveClient.CreateSync apiKey
        let charList = client.GetCharacters()
        test <@ charList.Characters |> Seq.length <> 0 @>

    [<Fact>]
    let ``No Unquote`` () =
        Assert.Equal(2 + 2, 4)

    [<Fact>]
    let ``With Unquote`` () =
        test <@ 2 + 2 = 4 @>