using System.Collections.Generic;

namespace More.Json
{
	public class JsonList : List<object>
	{
		public void Add(IJsonValue val)
		{
			base.Add(val.ToJsonValue());
		}

		public void Add<T>(IEnumerable<T> val) where T : IJsonValue
		{
			base.Add(val.ToJsonValue());
		}

		public void Add<T>(IDictionary<string, T> val) where T : IJsonValue
		{
			base.Add(val.ToJsonValue());
		}
	}
}
