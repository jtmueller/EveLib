namespace EveLib

open System
open System.Xml
open System.Xml.Linq
open EveLib
open EveLib.FSharp
open ClientUtils

type internal MapQueries(apiValues: (string * string) list) =

    let getRecentKills () = async {
        let! response = getResponse "/map/Kills.xml.aspx" []
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return 
            rowset.Rows
            |> Seq.map (fun r -> { Id = xval r?solarSystemID
                                   ShipKills = xval r?shipKills
                                   FactionKills = xval r?factionKills
                                   PodKills = xval r?podKills
                                   QueryTime = response.QueryTime
                                   CachedUntil = response.CachedUntil })
    }

    interface EveLib.FSharp.IMapQueries with
        member x.GetRecentKills() = getRecentKills()

    interface EveLib.Async.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.StartAsTask

