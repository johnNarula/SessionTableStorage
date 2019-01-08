﻿using SessionTableStorage.Library.Interfaces;
using System;
using System.Threading.Tasks;

namespace SessionTableStorage.Library
{
	public abstract class CacheableStorage : SessionStorageBase
	{
		public CacheableStorage(string partitionKey) : base(partitionKey)
		{
		}

		public async Task<T> GetAsync<T>(string rowKey, Func<Task<T>> query, T defaultValue = default(T)) where T : ICacheable
		{
			T result = await GetAsync(rowKey, defaultValue);
			if (result.IsValid) return result;

			result = await query.Invoke();
			await SetAsync(rowKey, result);

			return result;
		}

		public async Task SetAsync<T>(string rowKey, T data) where T : ICacheable
		{
			data.IsValid = true;
			await base.SetAsync(rowKey, data);
		}

		public async Task InvalidateAsync<T>(string rowKey) where T : ICacheable
		{
			T result = await GetAsync<T>(rowKey);
			if (result.Equals(default(T))) return;

			result.IsValid = false;
			await base.SetAsync(rowKey, result);
		}

		public void Invalidate<T>(string rowKey) where T : ICacheable
		{
			T result = Get<T>(rowKey);
			if (result.Equals(default(T))) return;

			result.IsValid = false;
			base.Set(rowKey, result);
		}

		public T Get<T>(string rowKey, Func<T> query, T defaultValue = default(T)) where T : ICacheable
		{
			T result = Get(rowKey, defaultValue);
			if (result.IsValid) return result;

			result = query.Invoke();
			Set(rowKey, result);

			return result;
		}

		public void Set<T>(string rowKey, T data) where T : ICacheable
		{
			data.IsValid = true;
			base.Set(rowKey, data);
		}
	}
}