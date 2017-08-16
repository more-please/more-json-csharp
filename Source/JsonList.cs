using System.Collections.Generic;

namespace More.Json
{
	public class JsonList : List<object>
	{
		public JsonList() { }
		public JsonList(int capacity) : base(capacity) { }
	}
}
