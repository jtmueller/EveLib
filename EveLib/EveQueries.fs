namespace EveLib

open System
open System.Xml
open System.Xml.Linq
open EveLib
open EveLib.FSharp
open ClientUtils

type internal EveQueries(apiValues: (string * string) list) =

    let getItemNames (ids:seq<int>) = async {
        let values = ("ids", String.Join(",", ids)) :: apiValues
        let! response = getResponse "/eve/CharacterName.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return rowset.Rows
               |> Seq.map (fun r -> { ItemName = xval r?name; Id = xval r?characterID; CachedUntil = response.CachedUntil })
               |> Seq.cache
    }

    let getItemIds (names:seq<string>) = async {
        let values = ("names", String.Join(",", names)) :: apiValues
        let! response = getResponse "/eve/CharacterID.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return rowset.Rows
               |> Seq.map (fun r -> { ItemName = xval r?name; Id = xval r?characterID; CachedUntil = response.CachedUntil })
               |> Seq.cache
    }

    interface EveLib.FSharp.IEveQueries with
        member x.GetItemIds([<ParamArray>] names : string[]) = getItemIds names
        member x.GetItemNames([<ParamArray>] ids : int[]) = getItemNames ids
    interface EveLib.Async.IEveQueries with
        member x.GetItemIds([<ParamArray>] names : string[]) = getItemIds names |> Async.StartAsTask
        member x.GetItemNames([<ParamArray>] ids : int[]) = getItemNames ids |> Async.StartAsTask
    interface EveLib.Sync.IEveQueries with
        member x.GetItemIds([<ParamArray>] names : string[]) = getItemIds names |> Async.RunSynchronously
        member x.GetItemNames([<ParamArray>] ids : int[]) = getItemNames ids |> Async.RunSynchronously
