using System.Collections.Generic;

namespace More.Json
{
	public class JsonDict : OrderPreservingDictionary<string, object>
	{
		public JsonDict() { }
		public JsonDict(int capacity) : base(capacity) { }
	}
}
