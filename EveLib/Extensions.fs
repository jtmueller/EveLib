#nowarn "77"
namespace EveLib.FSharp

open System
open System.Collections.Generic
open System.Xml.Linq
open System.Net

[<AutoOpen>]
module Extensions =

    let isNull (o:obj) = Object.ReferenceEquals(o, null)
    let isNotNull : obj -> bool = (isNull >> not)

    let ns = XNamespace.op_Implicit
    let xn = XName.op_Implicit
    let inline xval (x : ^a when ^a :> XObject) : ^b = ((^a or ^b) : (static member op_Explicit : ^a -> ^b) x)
    
    type WebRequest with
        member x.AsyncGetRequestStream() =
            Async.FromBeginEnd(x.BeginGetRequestStream, x.EndGetRequestStream)
