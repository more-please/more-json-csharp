using NUnit.Framework;
using System.Collections.Generic;

namespace More.Json.Test
{
    [TestFixture]
    public class JsonWriterTest
    {
        [Test]
        public void TestWriteInt()
        {
            Check("1", 1);
			Check("-1", -1);

            Check("0", TestEnum.Foo);
            Check("1", TestEnum.Bar);
            Check("2", TestEnum.Xyzzy);
        }

        [Test]
        public void TestWriteDouble()
        {
            Check("1.0", 1.0);
            Check("-1.0", -1.0);
        }

		[Test]
		public void TestWriteString()
        {
            Check("\"\"", "");
            Check("\"Unicode escape: \\u0000\"", "Unicode escape: \0");
            Check("\"\\\" \\\\ \\b \\f \\n \\r \\t\"", "\" \\ \b \f \n \r \t");
            Check("\"Unicode: (╯°□°）╯︵ ┻━┻\"", "Unicode: (╯°□°）╯︵ ┻━┻");
            Check("\"Emoji: \ud83e\udd14\"", "Emoji: \ud83e\udd14");
		}

		[Test]
        public void TestWriteDict()
        {
            var expected = "{\"one\":1,\"two\":2,\"three\":3,\"four\":4,\"five\":5}";

            // JsonDict preserves insertion order (needed for exact result)
            var dict = new JsonDict {
                { "one", 1 },
                { "two", 2 },
                { "three", 3 },
                { "four", 4 },
                { "five", 5 },
            };

            Check(expected, dict);

            // Not an IDictionary, but enumerable key-value pairs are supported
            // as long as the keys are strings. This is needed to support some
            // weird system types like ImmutableDictionary.
            var list = new List<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("one", 1),
                new KeyValuePair<string, object>("two", 2),
                new KeyValuePair<string, object>("three", 3),
                new KeyValuePair<string, object>("four", 4),
                new KeyValuePair<string, object>("five", 5),
            };

            Check(expected, list);
        }

		[Test]
		public void TestLeaveOpen()
		{
			var stream = new TestStream();
            JsonWriter.Write("foo", stream, leaveOpen: true);
            Assert.That(stream.DisposeCount == 0);

			JsonWriter.Write("foo", stream, leaveOpen: false);
			Assert.That(stream.DisposeCount == 1);
		}

		[Test]
        public void TestEncoding()
        {
			// Euro symbol (U+20AC) in quotes (U+0022). No BOM.
			byte[] expected = { 0x22, 0xe2, 0x82, 0xac, 0x22 };

			var stream = new TestStream();
            JsonWriter.Write("€", stream);
            Assert.AreEqual(expected, stream.ToArray());
        }

		private void Check(string json, object obj)
		{
			Assert.AreEqual(json, JsonWriter.ToString(obj));
		}
	}
}
