#EveLib#

This is a (currently incomplete) .NET client library for the [EVE Online API](http://wiki.eve-id.net/APIv2_Page_Index), implemented in F# in a completely async manner, and using either [RavenDB](http://ravendb.net/) or the filesystem for caching.

See also the [EVE Community Toolkit](http://community.eveonline.com/community/toolkit.asp).

##Usage##

**F#**
    
    open EveLib
    open EveLib.RavenCache
    
    let getOnlinePlayers () = async {
        let client = RavenEveClient.CreateFSharp()
        let! status = client.GetServerStatus()
        return status.OnlinePlayers
    }

**C#**

    using EveLib;
    using EveLib.RavenCache;

    static class EveExample
    {
        async Task<int> GetOnlinePlayers()
        {
            var client = RavenEveClient.CreateAsync();
            var status = await client.GetServerStatus();
            return status.OnlinePlayers;
        }
    }