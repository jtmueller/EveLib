[<AutoOpen>]
module EveLib.Tests.TestUtils

    open System.IO
    open System.Reflection
    open System.Xml.Linq
    open EveLib
    open EveLib.FSharp

    /// Loads the api key from an xml file not in source control,
    /// located in the "packasge" folder.
    let apiKey =
        let asm = Assembly.GetExecutingAssembly()
        let apiFile = Path.Combine(Path.GetDirectoryName(asm.Location), @"..\..\..\packages\ApiKey.xml")
        let doc = XDocument.Load(apiFile)
        let root = doc.Root
        { Id = root.Element(xn "id") |> int
          VCode = root.Element(xn "vCode") |> string
          AccessMask = root.Element(xn "accessMask") |> int }
