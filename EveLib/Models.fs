namespace EveLib

open System
open System.Xml.Linq
open EveLib.FSharp

type ApiKey = { 
    Id : int
    VCode : string
    AccessMask : int
}

type ServerStatus = {
    ServerOpen : bool
    OnlinePlayers : int
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type Character = 
    { CharId : int
      Name : string
      CorpId : int
      CorpName : string }
    with
        member x.PortraitUrl =
            sprintf "http://image.eveonline.com/Character/%i_128.jpg" x.CharId

type CharacterList = {
    KeyId : int
    Characters : seq<Character>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type NamedItem = {
    Name : string
    ItemId : int
    CachedUntil : DateTimeOffset
}

type WalletType =
    | Personal = 0
    | Corporate = 1

type WalletAccount = {
    AccountId : int
    AccountKey : int
    Balance : decimal
}

type WalletSet = {
    CharacterId : int
    Type : WalletType
    Accounts : seq<WalletAccount>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type SolarSystemKills = {
    SolarSystemId : int
    ShipKills : int
    FactionKills : int
    PodKills : int
}

type RecentKills = {
    SolarSystems : seq<SolarSystemKills>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type MailHeader = {
    MessageId : int
    SenderId : int
    SentDate : DateTimeOffset
    Title : string
    ToCorpOrAllianceId : int option
    ToCharacterIds : seq<int>
    ToListIds : seq<int>
}

type MailHeaderList = {
    CharacterId : int
    MailHeaders : seq<MailHeader>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type MailBody = {
    MessageId : int
    Text : string
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type MailBodyList = {
    MailBodies : seq<MailBody>
    MissingMessageIds : seq<int>
}