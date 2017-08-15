using System.Collections.Generic;

namespace More.Json
{
	public class JsonDict : SortedDictionary<string, object>
	{
		public void Add(string key, IJsonValue val)
		{
			base.Add(key, val.ToJsonValue());
		}

		public void Add<L, T>(string key, IEnumerable<T> val)
			where T : IJsonValue
		{
			base.Add(key, val.ToJsonValue());
		}

		public void Add<D, T>(string key, D val)
			where D : IDictionary<string, T>
			where T : IJsonValue
		{
			base.Add(key, val.ToJsonValue());
		}
	}
}
