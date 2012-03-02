namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal MapQueries(baseClient: FSharp.IMapQueries, store: IDocumentStore) =
    
    // TODO: this crashes RavenDB with a Stack Overflow Exception
    let getRecentKills () = async {
        use session = store.OpenAsyncSession()

        let! kills = session.Query<RecentKills>() |> AsyncQuery.head

        match kills with
        | None ->
            let! updated = baseClient.GetRecentKills()
            session.Store(updated)
            do! session.AsyncSaveChanges()
            return updated
        | Some k when k.CachedUntil < DateTimeOffset.UtcNow ->
            let! updated = baseClient.GetRecentKills()
            session.Delete(k)
            session.Store(updated)
            do! session.AsyncSaveChanges()
            return updated
        | Some k ->
            return k
    }

    interface EveLib.FSharp.IMapQueries with
        member x.GetRecentKills() = getRecentKills()
    interface EveLib.Async.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.StartAsTask
    interface EveLib.Sync.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.RunSynchronously
