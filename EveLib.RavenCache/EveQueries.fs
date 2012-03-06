namespace EveLib.RavenCache

open System
open System.Linq
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal EveQueries(baseClient: FSharp.IEveQueries, store: IDocumentStore) =

    let getItemNames (ids:int[]) = async {
        use session = store.OpenAsyncSession()

        // TODO: raven returns batches of 128 only.
        //       need to detect if more id's requested, use paging.
        let! matches = session.AsyncLoad<NamedItem>(ids |> Array.map (sprintf "nameditems/%i"))

        if matches.Length = 0 then
            let! updated = baseClient.GetItemNames(ids)
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            return updated
        else
            let outdated, stillGood =
                let now = DateTimeOffset.UtcNow
                matches
                |> List.ofSeq 
                |> List.partition (fun m -> m.CachedUntil < now)

            let missing =
                stillGood
                |> Seq.map (fun m -> m.Id)
                |> Set.ofSeq
                |> Set.difference (ids |> Set.ofArray)

            let toLoad =
                outdated
                |> Seq.map (fun m -> m.Id)
                |> Seq.append missing

            if toLoad |> Seq.exists (fun _ -> true) then
                let! updated = baseClient.GetItemNames(toLoad |> Array.ofSeq)
                let merged = stillGood |> Seq.append updated |> Seq.cache
                outdated |> List.iter session.Advanced.Evict
                merged |> Seq.iter session.Store
                do! session.AsyncSaveChanges()
                return merged
            else
                return upcast stillGood
    }

    let getItemIds (names:string[]) = async {
        use session = store.OpenAsyncSession()

        // TODO: raven returns batches of 128 only.
        //       need to detect if more id's requested, use paging.
        let! matches =
            query {
                for ni in session.Query<NamedItem>() do
                where (LinqExtensions.In(ni.Name, names))
                select ni
            } |> AsyncQuery.asIList

        if matches.Count = 0 then
            let! updated = baseClient.GetItemIds(names)
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            return updated
        else
            let outdated, stillGood =
                let now = DateTimeOffset.UtcNow
                matches
                |> List.ofSeq 
                |> List.partition (fun m -> m.CachedUntil < now)

            let missing =
                stillGood
                |> Seq.map (fun m -> m.Name)
                |> Set.ofSeq
                |> Set.difference (names |> Set.ofArray)

            let toLoad =
                outdated
                |> Seq.map (fun m -> m.Name)
                |> Seq.append missing

            if toLoad |> Seq.exists (fun _ -> true) then
                let! updated = baseClient.GetItemIds(toLoad |> Array.ofSeq)
                let merged = stillGood |> Seq.append updated |> Seq.cache
                outdated |> List.iter session.Advanced.Evict
                merged |> Seq.iter session.Store
                do! session.AsyncSaveChanges()
                return merged
            else
                return upcast stillGood
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
