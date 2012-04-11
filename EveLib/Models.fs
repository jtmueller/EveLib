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
    { mutable Id : int
      Name : string
      CorpId : int
      CorpName : string }
    with
        member x.PortraitUrl =
            sprintf "http://image.eveonline.com/Character/%i_128.jpg" x.Id

and CharacterList = {
    mutable Id : int
    Characters : seq<Character>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type NamedItem = {
    mutable Id : int
    ItemName : string
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
    mutable Id : int
    Type : WalletType
    Accounts : seq<WalletAccount>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type SolarSystemKills = {
    mutable Id : int
    ShipKills : int
    FactionKills : int
    PodKills : int
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type MailHeader = {
    mutable Id : int
    RecipientId : int
    SenderId : int
    SentDate : DateTimeOffset
    Title : string
    ToCorpOrAllianceId : int option
    ToCharacterIds : seq<int>
    ToListIds : seq<int>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type MailBody = {
    mutable Id : int
    RecipientId : int
    Text : string
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}

type CharacterSheet = {
    mutable Id : int
    CharacterName : string
    DoB : DateTimeOffset
    Race : string
    Bloodline : string
    Ancestry : string
    Gender : string
    CorpName : string
    CorpId : int
    AllianceName : string option
    AllianceId : int option
    CloneName : string
    CloneSkillPoints : int
    Balance : decimal
    AttributeEnhancers : AttributeEnhancers
    Attributes : Attributes
    Skills : seq<Skill>
    Certificates : seq<int>
    CorpRoles : seq<Role>
    CorpRolesAtHQ : seq<Role>
    CorpRolesAtBase : seq<Role>
    CorpRolesAtOther : seq<Role>
    CorpTitles : seq<Title>
    QueryTime : DateTimeOffset
    CachedUntil : DateTimeOffset
}
and Augmentor = {
    AugmentName : string
    AugmentValue : int
}
and AttributeEnhancers = {
    MemoryBonus : Augmentor option
    PerceptionBonus : Augmentor option
    WillpowerBonus : Augmentor option
    IntelligenceBonus : Augmentor option
    CharismaBonus : Augmentor option
}
and Attributes = {
    Intelligence : int
    Memory : int
    Charisma : int
    Perception : int
    Willpower : int
}
and Skill = {
    TypeId : int
    SkillPoints : int
    Level : int
    Published : bool
}
and Role = {
    RoleId : int64
    RoleName : string
}
and Title = {
    TitleId : int
    TitleName : string
}