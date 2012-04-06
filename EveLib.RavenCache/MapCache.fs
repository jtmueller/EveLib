namespace EveLib.RavenCache

open System
open Raven.Client
open Raven.Client.Linq
open EveLib

type internal MapCache(baseClient: FSharp.IMapQueries, store: IDocumentStore) =

    let pageSize = 128

    let getKills (session:IAsyncDocumentSession) skipCount =
        async {
            return! query {
                for k in session.Query<SolarSystemKills>() do
                skip skipCount
                take pageSize
            } |> AsyncQuery.asIList
        }
    
    let getRecentKills () = async {
        use session = store.OpenAsyncSession()

        let! kills = getKills session 0

        if kills.Count = 0 then
            let! updated = baseClient.GetRecentKills()
            updated |> Seq.iter session.Store
            do! session.AsyncSaveChanges()
            return updated
        else
            // Query returns 128 kills at a time. Keep querying until we have them all.
            let go = ref true
            while !go do
                let! nextPage = getKills session kills.Count
                if nextPage.Count = 0 then
                    go := false
                else
                    nextPage |> Seq.iter kills.Add

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
