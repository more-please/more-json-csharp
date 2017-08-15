using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace More.Json.Test
{
    [TestFixture]
    public class JsonReaderTest
    {
        [Test]
        public void TestReadNumber()
        {
            Check(1, "1");
			Check(-1, "-1");

            Check(1.0, "1.0");
			Check(-1.0, "-1.0");
			Check(1.0, "1e0");
			Check(0.1, "1E-1");
			Check(1200.0, "1.2e+3");

            CheckFails("+1");
			CheckFails("99999999999999999999999999999999999999999999999999999");
		}

        [Test]
        public void TestReadString()
        {
            Check("", "\"\"");
			Check("Unicode escape: \0", "\"Unicode escape: \\u0000\"");
			Check("\" \\ \b \f \n \r \t", "\"\\\" \\\\ \\b \\f \\n \\r \\t\"");
			Check("Unicode: (╯°□°）╯︵ ┻━┻", "\"Unicode: (╯°□°）╯︵ ┻━┻\"");
			Check("Emoji: \ud83e\udd14", "\"Emoji: \ud83e\udd14\"");

			CheckFails("");
			CheckFails("\"");
			CheckFails("\\x");
			CheckFails("\\u123");
			CheckFails("\\u120x");
		}

		[Test]
		public void TestReadArray()
		{
            IList<object> expected = new List<object> { 1, 2, 3, 4 };
			Check(expected, "[1, 2, 3, 4]");

			CheckFails("[1, 2, 3, 4");
			CheckFails("[1, 2, 3 4]");
			CheckFails("[1, 2, 3,, 4");
		}

		[Test]
		public void TestReadDict()
		{
            // For the purposes of Equals(), ordering shouldn't matter
            IDictionary<string, object> expected = new Dictionary<string, object> {
				{ "one", 1 },
				{ "two", 2 },
				{ "three", 3 },
				{ "four", 4 },
				{ "five", 5 },
			};

            Check(expected, "{\"one\":1,\"two\":2,\"three\":3,\"four\":4,\"five\":5}");

            CheckFails("{");
            CheckFails("{ 1: 2 }");
            CheckFails("{ \"one\": 1 \"two\": 2 }");
            CheckFails("{ \"one\": 1, \"two\": 2, }");
		}

		private void Check<T>(T expected, string json)
		{
            object obj = JsonReader.Read(json);
            T result = (T)obj; // Should be able to cast result to the exact type
            Assert.AreEqual(expected, result);
		}

        private void CheckFails(string json)
        {
            Assert.Throws<FormatException>(() => JsonReader.Read(json));
        }
	}
}
