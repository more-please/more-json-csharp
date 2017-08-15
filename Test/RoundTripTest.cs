using NUnit.Framework;
using System;

namespace More.Json.Test
{
    [TestFixture]
    public class RoundTripTest
    {
        [Test]
        public void TestInts()
        {
            RoundTrip((char)0, Convert.ToChar);
            RoundTrip(Char.MinValue, Convert.ToChar);
            RoundTrip(Char.MaxValue, Convert.ToChar);

            RoundTrip((Int16)0, Convert.ToInt16);
            RoundTrip(Int16.MinValue, Convert.ToInt16);
            RoundTrip(Int16.MaxValue, Convert.ToInt16);

            RoundTrip((UInt16)0, Convert.ToUInt16);
            RoundTrip(UInt16.MinValue, Convert.ToUInt16);
            RoundTrip(UInt16.MaxValue, Convert.ToUInt16);

            RoundTrip((Int32)0, Convert.ToInt32);
            RoundTrip(Int32.MinValue, Convert.ToInt32);
            RoundTrip(Int32.MaxValue, Convert.ToInt32);
        }

        [Test]
        public void TestEnums()
        {
            Func<object, TestEnum> ToEnum = (n) => (TestEnum)n;
            RoundTrip(TestEnum.Foo, ToEnum);
            RoundTrip(TestEnum.Bar, ToEnum);
            RoundTrip(TestEnum.Xyzzy, ToEnum);
        }

        [Test]
        public void TestFloats()
        {
            RoundTrip(0.0f, Convert.ToSingle);
            RoundTrip(Single.MinValue, Convert.ToSingle);
            RoundTrip(Single.MinValue, Convert.ToSingle);
            RoundTrip(Single.Epsilon, Convert.ToSingle);

            RoundTrip(0.0d, Convert.ToDouble);
            RoundTrip(Double.MinValue, Convert.ToDouble);
            RoundTrip(Double.MaxValue, Convert.ToDouble);
            RoundTrip(Double.Epsilon, Convert.ToDouble);
        }

        private void RoundTrip<T>(T original, Func<object, T> convert)
        {
            string json = JsonWriter.ToString(original);
            object obj = JsonReader.Read(json);
            T result = convert(obj);
            Assert.AreEqual(original, result);
        }
    }
}
