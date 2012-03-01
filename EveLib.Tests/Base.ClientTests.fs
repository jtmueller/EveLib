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
            test <@ charList.Characters |> Seq.length <> 0 @>
        } |> Async.StartAsTask
