namespace EveLib

open System
open System.Xml
open System.Xml.Linq
open EveLib
open EveLib.FSharp
open ClientUtils

type private CharacterQueries(apiValues: (string * string) list) =
    let getAccountBalance charId = async {
        let values = ("characterID", string charId) :: apiValues
        let! response = getResponse "/char/AccountBalance.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return 
            rowset.Rows
            |> Seq.map (fun r -> { AccountId = xval r?accountID; 
                                   AccountKey = xval r?accountKey; 
                                   Balance = xval r?balance; 
                                   QueryTime = response.QueryTime
                                   CachedUntil = response.CachedUntil })
            |> Seq.cache
    }

    interface EveLib.FSharp.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
    interface EveLib.Async.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
    interface EveLib.Sync.ICharQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously

type private CorporationQueries(apiValues: (string * string) list) =
    let getAccountBalance charId = async {
        let values = ("characterID", string charId) :: apiValues
        let! response = getResponse "/corp/AccountBalance.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return 
            rowset.Rows
            |> Seq.map (fun r -> { AccountId = xval r?accountID; 
                                   AccountKey = xval r?accountKey; 
                                   Balance = xval r?balance; 
                                   QueryTime = response.QueryTime
                                   CachedUntil = response.CachedUntil })
            |> Seq.cache
    }

    interface EveLib.FSharp.ICorpQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId
    interface EveLib.Async.ICorpQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.StartAsTask
    interface EveLib.Sync.ICorpQueries with
        member x.GetAccountBalance(charId) = getAccountBalance charId |> Async.RunSynchronously

type private EveQueries(apiValues: (string * string) list) =
    let getItemNames (ids:seq<int>) = async {
        let values = ("ids", String.Join(",", ids)) :: apiValues
        let! response = getResponse "/eve/CharacterName.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return rowset.Rows
               |> Seq.map (fun r -> { Name = xval r?name; ItemId = xval r?characterID; CachedUntil = response.CachedUntil })
               |> Seq.cache
    }

    let getItemIds (names:seq<string>) = async {
        let values = ("names", String.Join(",", names)) :: apiValues
        let! response = getResponse "/eve/CharacterID.xml.aspx" values
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return rowset.Rows
               |> Seq.map (fun r -> { Name = xval r?name; ItemId = xval r?characterID; CachedUntil = response.CachedUntil })
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

type private MapQueries(apiValues: (string * string) list) =
    let getRecentKills () = async {
        let queryTime = DateTimeOffset.Now
        let! response = getResponse "/map/Kills.xml.aspx" []
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return {
            QueryTime = response.QueryTime
            CachedUntil = response.CachedUntil
            SolarSystems =
                rowset.Rows
                |> Seq.map (fun r -> { SolarSystemId = xval r?solarSystemID;
                                       ShipKills = xval r?shipKills;
                                       FactionKills = xval r?factionKills;
                                       PodKills = xval r?podKills })
                |> List.ofSeq
        }
    }

    interface EveLib.FSharp.IMapQueries with
        member x.GetRecentKills() = getRecentKills()
    interface EveLib.Async.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.StartAsTask
    interface EveLib.Sync.IMapQueries with
        member x.GetRecentKills() = getRecentKills() |> Async.RunSynchronously

type EveClient (apiKey:ApiKey) =

    let apiValues = [ ("keyID", string apiKey.Id); ("vCode", apiKey.VCode) ]
    let character = lazy( CharacterQueries(apiValues) )
    let corporation = lazy( CorporationQueries(apiValues) )
    let eve = lazy( EveQueries(apiValues) )
    let map = lazy( MapQueries(apiValues) )
        
    let getCharacters () = async {
        let! response = getResponse "/account/Characters.xml.aspx" apiValues
        let rowset = RowSet(response.Result.Element(xn "rowset"))
        return { 
            KeyId = apiKey.Id;
            QueryTime = response.QueryTime;
            CachedUntil = response.CachedUntil;
            Characters =
                rowset.Rows 
                |> Seq.map (fun r -> { CharId = xval r?characterID; 
                                       Name = xval r?name; 
                                       CorpId = xval r?corporationID; 
                                       CorpName = xval r?corporationName })
                |> Seq.cache
        }
    }

    let getServerStatus() = async {
        let! response = getResponse "/server/ServerStatus.xml.aspx" []
        return { ServerOpen = response.Result.Element(xn "serverOpen") |> xval;
                 OnlinePlayers = response.Result.Element(xn "onlinePlayers") |> xval;
                 QueryTime = response.QueryTime;
                 CachedUntil = response.CachedUntil }
    }

    interface EveLib.FSharp.IEveClient with
        member x.GetCharacters() = getCharacters()
        member x.GetServerStatus() = getServerStatus()
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    interface EveLib.Async.IEveClient with
        member x.GetCharacters() = getCharacters() |> Async.StartAsTask
        member x.GetServerStatus() = getServerStatus() |> Async.StartAsTask
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    interface EveLib.Sync.IEveClient with
        member x.GetCharacters() = getCharacters() |> Async.RunSynchronously
        member x.GetServerStatus() = getServerStatus() |> Async.RunSynchronously
        member x.Character = upcast character.Value
        member x.Corporation = upcast corporation.Value
        member x.Eve = upcast eve.Value
        member x.Map = upcast map.Value

    static member CreateFSharp apiKey = EveClient(apiKey) :> EveLib.FSharp.IEveClient
    static member CreateAsync apiKey = EveClient(apiKey) :> EveLib.Async.IEveClient
    static member CreateSync apiKey = EveClient(apiKey) :> EveLib.Sync.IEveClient

