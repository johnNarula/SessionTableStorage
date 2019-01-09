# SessionTableStorage

I continue to explore ASP.NET in-proc Session replacements. I started with a [database-backed](https://github.com/adamosoftware/SessionData) approach, although I pretty quickly saw the error in that. Database-backed session storage is likely to be slow. Also, my approach to serialization was a little complicated due to trying to maintain dictionary indexer syntax. If you dispense with the indexer, you can use generic Set/Get accessors, which works better with Json.NET serialization.

I made another attempt using [Azure blob storage](https://github.com/adamosoftware/JsonStorage), but I figured it was time to look at a Table Storage approach since that is closer to my intented use case. My original reason for pursuing blob storage instead of Table Storage -- the `TableEntity` dependency -- didn't hold up when I realized I didn't actually have to make clients use it. The `TableEntity` dependency could be just an intermediate, internally-used type.

Of course, Cosmos DB and Redis seem to be the big players in this space. However, I really didn't care for the [Redis example](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/cache-web-app-howto) I looked at because it didn't seem simple enough. The cache methods themselves aren't generic (for example they use `StringSet` and `StringGet` instead of, for example, `Set<string>` or `Get<string>`). Type-specific methods don't bode well for handling arbitrary types. Also, Redis seems to have its own little command language of `PING`, `GET`, and `SET`, and I don't like the feel of this. I would rather services not expose internals like this. I looked briefly at [StackEchange.Redis](https://github.com/StackExchange/StackExchange.Redis). Gravell, Craver, and company seem to have done for Redis what they did for ADO.NET with Dapper. I love Dapper, but I'm looking for a simple, lightweight solution. If Redis requires this much built on top of it to be productive or easy to use, then that doesn't feel right. (Nonetheless, Marc Gravell's reasons for building StackExchange.Redis are interesting to [read about](https://blog.marcgravell.com/2014/03/so-i-went-and-wrote-another-redis-client.html).)

The reason I didn't go for Cosmos DB is because I'm cost-conscious, and I wanted to be more educated about its precursor Azure Table Storage. Table Storage has been around for a long time, but I haven't looked in-depth at it until now. If it turns out that Cosmos DB is considerably faster, then it will make sense to look at that. But, I wanted to get a simple solution working with Table Storage since, like I say, it's been around a while, and it's been a gap in my knowledge that I wanted to address.

## Quickstart

- Install Nuget package **SessionTableStorage.Library**

- Create a class based on [SessionStorageBase](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/SessionStorageBase.cs), and implement the `GetTable` method. See [this example](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/Classes/MySession.cs) from the Tests project. I like using an abstract base class so I don't have to make any assumptions about how a storage account is accessed. Note that I now use a [static helper method](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/Classes/CloudTableHelper.cs) to get the storage table because I'm using the same table in two different test sets now. I'm also using my [DevSecrets](https://github.com/adamosoftware/DevSecrets) package to keep credentials out of source control.

- Instantiate your `SessionStorageBase`-based class, passing a partition key that makes sense in your application. This could be a user name, ASP.NET SessionId or whatever makes sense in your application. (I'm not clear that a SessionId makes sense -- really I envisioned always using a user name as the partition key.)

- Use the [GetAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/SessionStorageBase.cs#L57) and [SetAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/SessionStorageBase.cs#L37) methods to load and save data you want to persist, respectively. These methods use Json.NET serialization under the hood to let you store aribitrary data in any type that Json.NET can handle.

- Use the [ClearAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/SessionStorageBase.cs#L85) method to delete all the values within a partition key. This is effectively the same as abandoning a session in ASP.NET.

- Check out the [integration tests](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/BaseTests.cs) to see common usage examples.

## TimedCacheableStorageBase

A requirement I have in another project turned out to be decent fit for this project. On my personal site [aosoftware.net](https://aosoftware.net/), I use GitHub's API to fill the "What I'm Working On" section -- it shows a summary of my activity over the last several days. GitHub API calls are limited to a certain maximum per hour or something. I don't remember what the limit is, but I don't want to hit that limit by calling their API with every page view of my site. Not that I get a lot of traffic in the first place, but I felt this was a good candidate for caching API call results for 15 minutes at a time. That way, no matter how often my home page is refreshed, it will call the GitHub API once every 15 minutes at most.

To do this, I added [TimedCacheableStorageBase](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/TimedCacheableStorage.cs) and a related interface [ITimedCacheable](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/Interfaces/ITimedCacheable.cs). Have a look at a different [GetAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/TimedCacheableStorage.cs#L41) method that retrieves an entity from storage, and automatically updates it if it's stale. Likewise, a different [SetAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/TimedCacheableStorage.cs#L82) method captures the date the data was updated so the freshness can be determined when it's retrieved later.

To see this in use, check out [TimedCacheableTests](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/TimedCacheableTests.cs) and the model class [GitHubActivityView](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/Models/GithubActivityView.cs). This test is not very effective because it's not efficient for me to wait 15 minutes to see if the cache is updated, but this was good enough for my purposes to view the data in Azure Storage Explorer manually.

## General-purpose caching with CacheableStorage and ICacheable

Time-based cache invalidation is one thing, but I think a more general-purpose requirement is to cache data indefinitely until it changes at the source. For example, a web app might query user profile information with every page view, but the profile info itself changes only when the user updates it. To reduce database traffic, it makes sense to cache the user profile rather than query it from the database all the time assuming that users don't update their profiles very often. There may be lots of cases like this in an application.

For this use case, I added [CacheableStorage](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/CacheableStorage.cs) which combines Azure Table Storage access with checks to see if the data is valid, and queries it live if not. This requires storage objects to implement [ICacheable](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/Interfaces/ICacheable.cs). This comes together in the [CacheableStorage.GetAsync](https://github.com/adamosoftware/SessionTableStorage/blob/master/SessionTableStorage.Library/CacheableStorage.cs#L38) method, which queries table storage for an object, and if it's marked as invalid, it's queried live.

Please see [CacheableTests](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/CacheableTests.cs) to see sample uses of this. I added a hypothetical user profile class [here](https://github.com/adamosoftware/SessionTableStorage/blob/master/Tests/Models/UserProfile.cs) that resembles how I would use this in a real app.




