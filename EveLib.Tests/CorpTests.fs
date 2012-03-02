namespace EveLib.Tests

open System
open EveLib
open Xunit

[<AbstractClass>]
type BaseCorpTests(clientFactory: unit -> FSharp.IEveClient) =
    class end