namespace EveLib.FSharp
open System
open EveLib

// http://wiki.eve-id.net/APIv2_Page_Index
// http://community.eveonline.com/community/toolkit.asp

type ICharQueries =
    abstract member GetAccountBalance : int -> Async<WalletSet>
    //abstract member GetMailHeaders : int -> Async<MailHeaderList>
    //abstract member GetMailBodies : int -> Async<MailBodyList>

type ICorpQueries =
    abstract member GetAccountBalance : int -> Async<WalletSet>

type IEveQueries =
    abstract member GetItemIds : [<ParamArray>] names : string[] -> Async<seq<NamedItem>>
    abstract member GetItemNames : [<ParamArray>] ids : int[] -> Async<seq<NamedItem>>

type IMapQueries =
    abstract member GetRecentKills : unit -> Async<seq<SolarSystemKills>>

type IEveClient =
    abstract member GetCharacters : unit -> Async<CharacterList>
    abstract member GetServerStatus : unit -> Async<ServerStatus>
    abstract member Character : ICharQueries with get
    abstract member Corporation : ICorpQueries with get
    abstract member Eve : IEveQueries with get
    abstract member Map : IMapQueries with get

namespace EveLib.Async
open System
open System.Threading.Tasks
open EveLib

type ICharQueries =
    abstract member GetAccountBalance : int -> Task<WalletSet>
    //abstract member GetMailHeaders : int -> Task<MailHeaderList>
    //abstract member GetMailBodies : int -> Task<MailBodyList>

type ICorpQueries =
    abstract member GetAccountBalance : int -> Task<WalletSet>

type IEveQueries =
    abstract member GetItemIds : [<ParamArray>] names : string[] -> Task<seq<NamedItem>>
    abstract member GetItemNames : [<ParamArray>] ids : int[] -> Task<seq<NamedItem>>

type IMapQueries =
    abstract member GetRecentKills : unit -> Task<seq<SolarSystemKills>>

type IEveClient =
    abstract member GetCharacters : unit -> Task<CharacterList>
    abstract member GetServerStatus : unit -> Task<ServerStatus>
    abstract member Character : ICharQueries with get
    abstract member Corporation : ICorpQueries with get
    abstract member Eve : IEveQueries with get
    abstract member Map : IMapQueries with get

namespace EveLib.Sync
open System
open EveLib

type ICharQueries =
    abstract member GetAccountBalance : int -> WalletSet
    //abstract member GetMailHeaders : int -> MailHeaderList
    //abstract member GetMailBodies : int -> MailBodyList

type ICorpQueries =
    abstract member GetAccountBalance : int -> WalletSet

type IEveQueries =
    abstract member GetItemIds : [<ParamArray>] names : string[] -> seq<NamedItem>
    abstract member GetItemNames : [<ParamArray>] ids : int[] -> seq<NamedItem>

type IMapQueries =
    abstract member GetRecentKills : unit -> seq<SolarSystemKills>

type IEveClient =
    abstract member GetCharacters : unit -> CharacterList
    abstract member GetServerStatus : unit -> ServerStatus
    abstract member Character : ICharQueries with get
    abstract member Corporation : ICorpQueries with get
    abstract member Eve : IEveQueries with get
    abstract member Map : IMapQueries with get

