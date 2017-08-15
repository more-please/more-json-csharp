using System.IO;

namespace More.Json.Test
{
    class TestStream : MemoryStream
	{
		public int DisposeCount = 0;

		protected override void Dispose(bool disposing)
		{
			++DisposeCount;
		}
	}
}
