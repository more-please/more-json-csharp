using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

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
			var result = new JsonList();
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
			var result = new JsonDict();
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
        private int _pos;
        private StringBuilder _capture;
    }
}
