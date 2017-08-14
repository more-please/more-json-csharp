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
			using (var json = new JsonWriter(writer))
				json.WriteValue(obj);
		}

		// ---------------------------------------------------------------------
		// Override hooks

		public virtual void Fail(string message)
		{
			throw new FormatException($"JsonWriter error: {message}");
		}

		// ---------------------------------------------------------------------
		// Write any JSON value

		public void WriteValue(object obj)
		{
			if (obj == null)
			{
				_writer.Write("null");
			}
			else if (obj is string)
			{
				WriteString(obj as string);
			}
			else if (obj is Int16 || obj is Int32 || obj is Int64 || obj is Enum)
			{
				WriteInt(Convert.ToInt64(obj));
			}
			else if (obj is Single || obj is Double || obj is Decimal)
			{
				WriteFloat(Convert.ToDouble(obj));
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
				Fail($"failed to write object: ${obj}");
			}
		}

		// ---------------------------------------------------------------------
		// Write a specific JSON type

		public void WriteInt(long i)
		{
			_writer.Write(i.ToString("D", CultureInfo.InvariantCulture));
		}

		public void WriteFloat(double f)
		{
			_writer.Write(f.ToString("R", CultureInfo.InvariantCulture));
		}

		private const string _hexDigits = "0123456789ABCDEF";

		public void WriteString(string s)
		{
			_writer.Write('"');
			foreach (char c in s)
			{
				if (c == '\\')
				{
					_writer.Write("\\\\");
				}
				else if (c == '"')
				{
					_writer.Write("\\\"");
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
