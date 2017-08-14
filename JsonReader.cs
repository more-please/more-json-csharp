using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace More.Json
{
	public class JsonReader : IDisposable
	{
		// ---------------------------------------------------------------------
		// Constructor and convenience functions

		public JsonReader(TextReader reader, bool leaveOpen = false)
		{
			_reader = reader;
			_dispose = !leaveOpen;
		}

		public static object Read(string str)
		{
			using (var r = new JsonReader(new StringReader(str)))
				return r.ReadValue();
		}

		public static object Read(TextReader reader, bool leaveOpen = false)
		{
			using (var r = new JsonReader(reader, leaveOpen))
				return r.ReadValue();
		}

		public static object Read(Stream stream, bool leaveOpen = false, int bufferSize = 4096)
		{
			var utf8 = new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false,
				throwOnInvalidBytes: true);
			var reader = new StreamReader(stream, utf8, true, bufferSize, leaveOpen);
			using (var json = new JsonReader(reader))
				return json.ReadValue();
		}

		// ---------------------------------------------------------------------
		// Override hooks

		public virtual void Fail(string message)
		{
			throw new FormatException($"JsonReader error at index {_pos}: {message}");
		}

		public virtual IDictionary<string, object> NewDict()
		{
			return new Dictionary<string, object>();
		}

		public virtual IList<object> NewArray()
		{
			return new List<object>();
		}

		// ---------------------------------------------------------------------
		// Read any JSON value

		public object ReadValue()
		{
			Trim();
			int c = Peek;
			if (c < 0)
			{
				Fail("unexpected end of input");
				return null;
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
				Fail($"unexpected char: '{(char)c}'");
				return null;
			}
		}

		// ---------------------------------------------------------------------
		// Read a specific JSON type

		public object ReadNumber()
		{
			double result = Sign() * Decimal();

			// Return an int if we can
			if (result >= Int32.MinValue && result <= Int32.MaxValue)
				if (Peek != '.' && Peek != 'e' && Peek != 'E')
					return (int)result;

			if (Maybe('.'))
			{
				int fracPos = Pos;
				long frac = Decimal();
				result += frac / Math.Pow(10, Pos - fracPos);
			}
			if (Maybe('e') || Maybe('E'))
			{
				long exp = Sign() * Decimal();
				result *= Math.Pow(10, exp);
			}
			return result;
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
						default: Fail("unknown escape code"); break;
					}
				}
				result.Append((char)c);
			}
			Fail("unterminated string");
			return null;
		}

		public IList<object> ReadArray()
		{
			var result = new List<object>();
			Expect('[');
			Trim();
			if (Maybe(']'))
			{
				return result;
			}
			do
			{
				object item = ReadValue();
				result.Add(item);
				Trim();
			}
			while (Maybe(','));
			Expect(']');
			return result;
		}

		public IDictionary<string, object> ReadDict()
		{
			var result = new SortedDictionary<string, object>();
			Expect('{');
			Trim();
			if (Maybe('}'))
			{
				return result;
			}
			do
			{
				Trim();
				string key = ReadString();
				Trim();
				Expect(':');
				object val = ReadValue();
				result.Add(key, val);
				Trim();
			}
			while (Maybe(','));
			Expect('}');
			return result;
		}

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
					Fail("Unterminated Unicode escape");
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
					Fail($"Expected hex digit");
				}
			}
			return (char)result;
		}

		private int Sign()
		{
			if (Maybe('-')) return -1;
			if (Maybe('+')) return 1;
			return 1;
		}

		private long Decimal()
		{
			long result = 0;
			while (!AtEnd && Peek >= '0' && Peek <= '9')
				result = result * 10 + Pop() - '0';
			return result;
		}

		private void Expect(string expected)
		{
			foreach (char c in expected)
				Expect(c);
		}

		private void Expect(char expected)
		{
			if (!Maybe(expected))
				Fail($"Expected '{expected}'");
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
				++_pos;
			return result;
		}

		private bool Maybe(char maybe)
		{
			if (Peek != maybe) return false;
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

		// ---------------------------------------------------------------------
		// State

		public void Dispose()
		{
			if (_dispose)
				_reader.Dispose();
		}

		private readonly TextReader _reader;
		private readonly bool _dispose;
		private int _pos;
	}
}
