namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal MapQueries(baseClient: FSharp.IMapQueries, store: IDocumentStore) =
    
    // TODO: this crashes RavenDB with a Stack Overflow Exception
    let getRecentKills () = async {
        use session = store.OpenAsyncSession()

        // TODO: if there's more than 128 solar systems, must query in batches
        let! kills = session.Query<SolarSystemKills>() |> AsyncQuery.asIList

        if kills.Count = 0 then
            let! updated = baseClient.GetRecentKills()
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            return updated
        else
            let now = DateTimeOffset.UtcNow
            if kills |> Seq.exists (fun k -> k.CachedUntil < now) then
                // we've got at least one expired kill record, so query again
                kills |> Seq.iter session.Delete
                do! session.AsyncSaveChanges()
                kills |> Seq.iter session.Advanced.Evict
                let! updated = baseClient.GetRecentKills()
                updated |> Seq.iter session.Store
                do! session.AsyncSaveChanges()
                return updated
            else
                return upcast kills
    }

    interface EveLib.FSharp.IMapQueries with
        member x.GetRecentKills() = getRecentKills()
    interface EveLib.Async.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.StartAsTask
    interface EveLib.Sync.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.RunSynchronously
