# SessionTableStorage

I continue to explore ASP.NET Session replacements. I started with a [database-backed](https://github.com/adamosoftware/SessionData) approach, although I pretty quickly saw the error in that. Database-backed session storage is likely to be slow. Also, my approach to serialization was a little complicated due to trying to maintain dictionary indexer syntax. If you dispense with the indexer, you can use generic Set/Get accessors, which works better with Json.NET serialization.

I made another attempt using [Azure blob storage](https://github.com/adamosoftware/JsonStorage), but I figured it was time to look at a Table Storage approach since that is closer to my intented use case. Of course, Cosmos DB and Redis seem to be the big players in this space. I really didn't care for the [Redis example](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-web-app-howto) I looked at because it didn't seem simple enough. The cache methods themselves aren't generic (for example they use `StringSet` and `StringGet` instead of, for example, `Set<string>` or `Get<string>`). Type-specific methods don't bode well for handling arbitrary types. Also, Redis seems to have its own little command language of `PING`, `GET`, and `SET`, and I don't like the feel of this. I would rather services not expose internals like this. I looked briefly at [StackEchange.Redis](https://github.com/StackExchange/StackExchange.Redis). Gravell, Craver, and co seem to have done for Redis what they did for ADO.NET with Dapper. I love Dapper, but I'm looking for a simple, lightweight solution. If Redis requires this much built on top of it to be productive or easy to use, then that doesn't feel right.

The reason I didn't go for Cosmos DB is because I'm cost-conscious, and I wanted to be more educated about its precursor Azure Table Storage. Table Storage has been around for a long time, but I haven't looked in-depth at it until now. If it turns out that Cosmos DB is considerably faster, then it will make sense to look at that. But, I wanted to get a simple solution working with Table Storage since, like I say, it's been around a while, and I've generally overlooked it until now.


