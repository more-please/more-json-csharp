using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace More.Json
{
	public class JsonWriter : IDisposable
	{
		// ---------------------------------------------------------------------
		// Constructor and convenience functions

		public JsonWriter(TextWriter writer, bool leaveOpen = false)
		{
			_writer = writer;
			_dispose = !leaveOpen;
		}

		public static string ToString(object obj)
		{
			using (var s = new StringWriter())
			{
				Write(obj, s);
				return s.ToString();
			}
		}

		public static void Write(object obj, TextWriter writer, bool leaveOpen = false)
		{
			using (var json = new JsonWriter(writer, leaveOpen))
				json.WriteValue(obj);
		}

		public static void Write(object obj, Stream stream, bool leaveOpen = false, int bufferSize = 4096)
		{
			var utf8 = new UTF8Encoding(
				encoderShouldEmitUTF8Identifier: false,
				throwOnInvalidBytes: true);
			var writer = new StreamWriter(stream, utf8, bufferSize, leaveOpen);
			Write(obj, writer);
		}

		// ---------------------------------------------------------------------
		// Override hooks

		public Exception Error(string message)
		{
			return new FormatException($"JsonWriter error: {message}");
		}

		// ---------------------------------------------------------------------
		// Write any JSON value

		public void WriteValue(object obj)
		{
			if (obj is IJsonValue)
				obj = (obj as IJsonValue).ToJsonValue();

			if (obj == null)
			{
				_writer.Write("null");
			}
			else if (obj is string)
			{
				WriteString(obj as string);
			}
			else if (obj is Int16 || obj is Int32 || obj is Int64
				|| obj is UInt16 || obj is UInt32 || obj is UInt64
				|| obj is Enum || obj is Char)
			{
				WriteInt(Convert.ToInt32(obj));
			}
			else if (obj is Single || obj is Double || obj is Decimal)
			{
				WriteDouble(Convert.ToDouble(obj));
			}
			else if (obj is bool)
			{
				_writer.Write((bool)obj ? "true" : "false");
			}
			else if (obj is IDictionary)
			{
				WriteDict(obj as IDictionary);
			}
			else if (obj is IEnumerable<KeyValuePair<string, object>>)
			{
				WriteDict(obj as IEnumerable<KeyValuePair<string, object>>);
			}
			else if (obj is IEnumerable)
			{
				WriteArray(obj as IEnumerable);
			}
			else
			{
				throw Error($"failed to write object: ${obj}");
			}
		}

		// ---------------------------------------------------------------------
		// Write a specific JSON type

		public void WriteInt(int n)
		{
			var s = n.ToString("D", CultureInfo.InvariantCulture);
			_writer.Write(s);
		}

		private static readonly char[] _floatChars = { '.', 'e', 'E' };

		public void WriteDouble(double f)
		{
			var s = f.ToString("R", CultureInfo.InvariantCulture);
			if (s.IndexOfAny(_floatChars) < 0)
			{
				s = s + ".0";
			}
			_writer.Write(s);
		}

		private const string _hexDigits = "0123456789ABCDEF";

		public void WriteString(string s)
		{
			_writer.Write('"');
			foreach (char c in s)
			{
				if (c == '"')
				{
					_writer.Write("\\\"");
				}
				else if (c == '\\')
				{
					_writer.Write("\\\\");
				}
				else if (c == '\b')
				{
					_writer.Write("\\b");
				}
				else if (c == '\f')
				{
					_writer.Write("\\f");
				}
				else if (c == '\n')
				{
					_writer.Write("\\n");
				}
				else if (c == '\r')
				{
					_writer.Write("\\r");
				}
				else if (c == '\t')
				{
					_writer.Write("\\t");
				}
				else if (c < 32)
				{
					_writer.Write("\\u00");
					_writer.Write(_hexDigits[c / 16]);
					_writer.Write(_hexDigits[c % 16]);
				}
				else
				{
					_writer.Write(c);
				}
			}
			_writer.Write('"');
		}

		public void WriteDict(IDictionary dict)
		{
			_writer.Write('{');
			var e = dict.GetEnumerator();
			if (e.MoveNext())
			{
				WriteString(e.Key.ToString());
				_writer.Write(':');
				WriteValue(e.Value);
				while (e.MoveNext())
				{
					_writer.Write(',');
					WriteString(e.Key.ToString());
					_writer.Write(':');
					WriteValue(e.Value);
				}
			}
			_writer.Write('}');
		}

		public void WriteDict(IEnumerable<KeyValuePair<string, object>> dict)
		{
			_writer.Write('{');
			var e = dict.GetEnumerator();
			if (e.MoveNext())
			{
				WriteString(e.Current.Key as string);
				_writer.Write(':');
				WriteValue(e.Current.Value);
				while (e.MoveNext())
				{
					_writer.Write(',');
					WriteString(e.Current.Key as string);
					_writer.Write(':');
					WriteValue(e.Current.Value);
				}
			}
			_writer.Write('}');
		}

		public void WriteArray(IEnumerable array)
		{
			_writer.Write('[');
			var e = array.GetEnumerator();
			if (e.MoveNext())
			{
				WriteValue(e.Current);
				while (e.MoveNext())
				{
					_writer.Write(',');
					WriteValue(e.Current);
				}
			}
			_writer.Write(']');
		}

		// ---------------------------------------------------------------------
		// State

		public void Dispose()
		{
			if (_dispose)
				_writer.Dispose();
		}

		private readonly TextWriter _writer;
		private readonly bool _dispose;
	}
}
