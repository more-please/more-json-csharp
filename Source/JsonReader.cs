using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;

namespace More.Json
{
	using JsonArrayItems = IEnumerable<object>;
	using JsonDictItems = IEnumerable<KeyValuePair<string, object>>;

	public class JsonReader : IDisposable
	{
		public delegate IList<object> ArrayBuilder(JsonArrayItems items);
		public delegate IDictionary<string, object> DictBuilder(JsonDictItems items);

		// ---------------------------------------------------------------------
		// Constructor and convenience functions

		public JsonReader(
			TextReader reader, bool leaveOpen = false, ArrayBuilder array = null, DictBuilder dict = null)
		{
			_reader = reader;
			_dispose = !leaveOpen;
			_array = array ?? List;
			_dict = dict ?? Dictionary;
		}

		public static object Read(string str, ArrayBuilder array = null, DictBuilder dict = null)
		{
			using (var r = new JsonReader(new StringReader(str), array: array, dict: dict))
				return r.ReadValue();
		}

		public static object Read(
			TextReader reader, bool leaveOpen = false, ArrayBuilder array = null, DictBuilder dict = null)
		{
			using (var r = new JsonReader(reader, leaveOpen: leaveOpen, array: array, dict: dict))
				return r.ReadValue();
		}

		public static object Read(
			Stream stream, bool leaveOpen = false, int bufferSize = 4096, ArrayBuilder array = null, DictBuilder dict = null)
		{
			var utf8 = new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false,
				throwOnInvalidBytes: true);
			var reader = new StreamReader(stream, utf8, true, bufferSize, leaveOpen);
			using (var json = new JsonReader(reader, array: array, dict: dict))
				return json.ReadValue();
		}

		// ---------------------------------------------------------------------
		// List and dictionary creation

		public static List<object> List(JsonArrayItems items)
		{
			return new List<object>(items);
		}

		public static IList<object> Immutable(JsonArrayItems items)
		{
			return ImmutableArray.CreateRange(items);
		}

		public static Dictionary<string, object> Dictionary(JsonDictItems items)
		{
			var result = new Dictionary<string, object>();
			foreach (var p in items)
				result.Add(p.Key, p.Value);
			return result;
		}

		public static ImmutableDictionary<string, object> Immutable(JsonDictItems items)
		{
			var result = ImmutableDictionary.CreateBuilder<string, object>();
			foreach (var p in items)
				result.Add(p.Key, p.Value);
			return result.ToImmutable();
		}

		public static SortedDictionary<string, object> Sorted(JsonDictItems items)
		{
			var result = new SortedDictionary<string, object>();
			foreach (var p in items)
				result.Add(p.Key, p.Value);
			return result;
		}

		public static ImmutableSortedDictionary<string, object> ImmutableSorted(JsonDictItems items)
		{
			var result = ImmutableSortedDictionary.CreateBuilder<string, object>();
			foreach (var p in items)
				result.Add(p.Key, p.Value);
			return result.ToImmutable();
		}

		public static IDictionary<string, object> OrderPreserving(JsonDictItems items)
		{
			var result = new JsonDict();
			foreach (var p in items)
				result.Add(p.Key, p.Value);
			return result;
		}

		// ---------------------------------------------------------------------
		// Read any JSON value

		public object ReadValue()
		{
			Trim();
			int c = Peek;
			if (c < 0)
			{
				throw Error("unexpected end of input");
			}
			else if (c == '"')
			{
				return ReadString();
			}
			else if (c == '[')
			{
				return ReadArray();
			}
			else if (c == '{')
			{
				return ReadDict();
			}
			else if (c == '-' || (c >= '0' && c <= '9'))
			{
				return ReadNumber();
			}
			else if (c == 't')
			{
				Expect("true");
				return true;
			}
			else if (c == 'f')
			{
				Expect("false");
				return false;
			}
			else if (c == 'n')
			{
				Expect("null");
				return null;
			}
			else
			{
				throw Error($"unexpected char: '{(char)c}'");
			}
		}

		// ---------------------------------------------------------------------
		// Read a specific JSON type

		public object ReadNumber()
		{
			StartCapture(24);

			Maybe('-');
			if (!Maybe('0'))
				if (Maybe('1', '9'))
					while (Maybe('0', '9'))
						;

			bool isInt = true;
			if (Maybe('.'))
			{
				isInt = false;
				while (Maybe('0', '9'))
					;
			}
			if (Maybe('e') || Maybe('E'))
			{
				isInt = false;
				if (Maybe('+') || Maybe('-') || true)
					while (Maybe('0', '9'))
						;
			}

			string s = EndCapture();
			var c = CultureInfo.InvariantCulture;

			try
			{
				// Note: can't use ?: here, as the whole point is they're different types!
				if (isInt)
					return Int32.Parse(s, c);
				else
					return Double.Parse(s, c);
			}
			catch (Exception e)
			{
				throw Error(e);
			}
		}

		public string ReadString()
		{
			StringBuilder result = new StringBuilder();
			Expect('"');
			for (int c = Pop(); c >= 0; c = Pop())
			{
				if (c == '"')
				{
					return result.ToString();
				}
				if (c == '\\')
				{
					c = Pop();
					switch (c)
					{
						case '"': case '\\': case '/': break;
						case 'b': c = '\b'; break;
						case 'f': c = '\f'; break;
						case 'n': c = '\n'; break;
						case 'r': c = '\r'; break;
						case 't': c = '\t'; break;
						case 'u': c = Unicode(); break;
						default: throw Error("unknown escape code");
					}
				}
				result.Append((char)c);
			}
			throw Error("unterminated string");
		}

		public IList<object> ReadArray()
		{
			Expect('[');
			Trim();
			if (Maybe(']'))
			{
				return _array(NoArrayItems);
			}
			var result = _array(ReadArrayItems());
			Expect(']');
			return result;
		}

		public IDictionary<string, object> ReadDict()
		{
			Expect('{');
			Trim();
			if (Maybe('}'))
			{
				return _dict(Noitems);
			}
			var result = _dict(ReadDictItems());
			Expect('}');
			return result;
		}

		public JsonArrayItems ReadArrayItems()
		{
			do
			{
				yield return ReadValue();
				Trim();
			}
			while (Maybe(','));
		}

		public JsonDictItems ReadDictItems()
		{
			do
			{
				Trim();
				string key = ReadString();
				Trim();
				Expect(':');
				object val = ReadValue();
				yield return new KeyValuePair<string, object>(key, val);
				Trim();
			}
			while (Maybe(','));
		}

		private static readonly object[] NoArrayItems = { };
		private static readonly KeyValuePair<string, object>[] Noitems = { };

		// ---------------------------------------------------------------------
		// Intermediate parsing constructs

		private char Unicode()
		{
			int result = 0;
			for (int i = 0; i < 4; ++i)
			{
				int c = Pop();
				if (c < 0)
				{
					throw Error("Unterminated Unicode escape");
				}
				else if (c >= '0' && c <= '9')
				{
					result = (16 * result) + (c - '0');
				}
				else if (c >= 'a' && c <= 'f')
				{
					result = (16 * result) + (c - 'a' + 10);
				}
				else if (c >= 'A' && c <= 'F')
				{
					result = (16 * result) + (c - 'A' + 10);
				}
				else
				{
					throw Error($"Expected hex digit");
				}
			}
			return (char)result;
		}

		private void Expect(string expected)
		{
			foreach (char c in expected)
				Expect(c);
		}

		private void Expect(char expected)
		{
			if (!Maybe(expected))
				throw Error($"Expected '{expected}'");
		}

		private bool Maybe(char maybe)
		{
			if (Peek != maybe) return false;
			Pop();
			return true;
		}

		private bool Maybe(char lo, char hi)
		{
			if (Peek < lo || Peek > hi) return false;
			Pop();
			return true;
		}

		private void Trim()
		{
			int c = Peek;
			while (c >= 0 && char.IsWhiteSpace((char)c))
			{
				Pop();
				c = Peek;
			}
		}

		public Exception Error(string message)
		{
			return new FormatException($"JsonReader error at index {_pos}: {message}");
		}

		public Exception Error(Exception cause)
		{
			return new FormatException($"JsonReader error at index {_pos}", cause);
		}

		// ---------------------------------------------------------------------
		// Low-level parse functions

		private bool AtEnd => _reader.Peek() < 0;
		private int Peek => _reader.Peek();
		private int Pos => _pos;

		private int Pop()
		{
			int result = _reader.Read();
			if (result >= 0)
			{
				++_pos;
				_capture?.Append((char)result);
			}
			return result;
		}

		private void StartCapture(int expectedSize)
		{
			_capture = new StringBuilder(expectedSize);
		}

		private string EndCapture()
		{
			string result = _capture.ToString();
			_capture = null;
			return result;
		}

		// ---------------------------------------------------------------------
		// State

		public void Dispose()
		{
			if (_dispose)
				_reader.Dispose();
		}

		private readonly TextReader _reader;
		private readonly bool _dispose;
		private readonly ArrayBuilder _array;
		private readonly DictBuilder _dict;
		private int _pos;
		private StringBuilder _capture = new StringBuilder();
	}
}
