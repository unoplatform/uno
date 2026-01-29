using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.Samples.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationDataContainer
	{
		[TestCleanup]
		public void Cleanup()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			SUT.Values.Clear();
		}

		[TestInitialize]
		public void Initialize()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			SUT.Values.Clear();
		}

		[TestMethod]
		public void When_Nothing()
		{
			var SUT = ApplicationData.Current.LocalSettings;
		}

		[TestMethod]
		public void When_SetString()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			SUT.Values["test"] = "42";

			Assert.AreEqual("42", SUT.Values["test"]);
		}

		[TestMethod]
		public void When_AllValues()
		{
			var values = new object[] {
				(bool)false,
				(bool)true,

				(byte)0,
				(byte)0xFF,
				(byte)0x7F,

				// (char)0, // Not supported yet
				(char)0xFF,

				DateTimeOffset.FromUnixTimeSeconds(0),
				DateTimeOffset.FromUnixTimeSeconds(0xFFFFF),

				(double)0,
				(double)42.0,
				//(double)double.NaN, // Not supported yet
				//(double)double.MaxValue, // Not supported yet
				//(double)double.MinValue, // Not supported yet

				Guid.Parse("{52460AC2-5AFA-4677-A0C2-52B6B5729006}"),

				(short)0,
				(short)0x7FFF,

				(int)0,
				(int)int.MinValue,
				(int)int.MaxValue,

				(long)0,
				(long)long.MinValue,
				(long)long.MaxValue,

				(float)0.0,
				(float)42.0,

				(string)"test",

				(byte)0,
				(byte)0x0FF,

				(ushort)0x0,
				(ushort)0xFFFF,

				(uint)0x0,
				(uint)uint.MaxValue,
				(uint)uint.MinValue,

				(ulong)0x0,
				(ulong)ulong.MaxValue,
				(ulong)ulong.MinValue,

				// new Windows.Foundation.Size(0, 0), // Not supported yet
				// Windows.Foundation.Size.Empty, // Not supported yet
				// new Windows.Foundation.Size(42, 43), // Not supported yet

				TimeSpan.FromSeconds(42),
				TimeSpan.Zero,

				// new Windows.Foundation.Point(0, 0), // Not supported yet
				// new Windows.Foundation.Point(43, 42), // Not supported yet

				// new Windows.Foundation.Rect(0, 0, 0, 0), // Not supported yet
				// new Windows.Foundation.Rect(1, 2, 3, 4), // Not supported yet

				// new Uri("http://www.microsoft.com"), // Not supported yet
			};

			var SUT = ApplicationData.Current.LocalSettings;

			for (int i = 0; i < values.Length; i++)
			{
				var propertyName = $"Prop{i}";
				SUT.Values[propertyName] = values[i];

				Assert.AreEqual(values[i], SUT.Values[propertyName]);
			}
		}

		[TestMethod]
		public void When_UpdateString()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			SUT.Values["test"] = "42";
			Assert.AreEqual("42", SUT.Values["test"]);

			SUT.Values["test"] = "43";
			Assert.AreEqual("43", SUT.Values["test"]);
		}

		[TestMethod]
		public void When_Remove()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			SUT.Values["test"] = "42";
			Assert.AreEqual("42", SUT.Values["test"]);

			SUT.Values.Remove("test");
			Assert.IsFalse(SUT.Values.ContainsKey("test"));
		}

		[TestMethod]
		public void When_TryGetValue()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			SUT.Values.Add("test", "42");
			Assert.AreEqual("42", SUT.Values["test"]);

			var result = SUT.Values.TryGetValue("test", out var value);
			Assert.IsTrue(result);
			Assert.AreEqual("42", value);

			var result2 = SUT.Values.TryGetValue("test2", out var value2);
			Assert.IsFalse(result2);
			Assert.IsNull(value2);
		}

		[TestMethod]
		public void When_GetAllKeys()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var originalCount = SUT.Values.Count;

			Assert.IsFalse(SUT.Values.Keys.Contains("test"));
			Assert.IsFalse(SUT.Values.Keys.Contains("test2"));
			Assert.IsFalse(SUT.Values.Values.Contains("42"));
			Assert.IsFalse(SUT.Values.Values.Contains("43"));

			SUT.Values.Add("test", "42");
			SUT.Values.Add("test2", "43");

			Assert.AreEqual(originalCount + 2, SUT.Values.Count);
			Assert.AreEqual(originalCount + 2, SUT.Values.Keys.Count);
			Assert.IsTrue(SUT.Values.Keys.Contains("test"));
			Assert.IsTrue(SUT.Values.Keys.Contains("test2"));

			Assert.AreEqual(originalCount + 2, SUT.Values.Values.Count);
			Assert.IsTrue(SUT.Values.Values.Contains("42"));
			Assert.IsTrue(SUT.Values.Values.Contains("43"));
		}

		[TestMethod]
		public void When_EnumerateOverValues()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			SUT.Values.Add("enumerate_over_values_one", "11");
			SUT.Values.Add("enumerate_over_values_two", "22");
			SUT.Values.Add("enumerate_over_values_three", "33");

			List<string> keysPresent = SUT.Values.Keys.ToList();

			foreach (var value in SUT.Values)
			{
				keysPresent.Remove(value.Key);
			}

			Assert.IsEmpty(keysPresent);
		}

		[TestMethod]
		public void When_NullValueSetViaIndexer()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			SUT.Values[key] = null;
			Assert.IsFalse(SUT.Values.ContainsKey(key));
		}

		[TestMethod]
		public void When_NullValueAdded()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			SUT.Values.Add(key, null);
			Assert.IsFalse(SUT.Values.ContainsKey(key));
		}

		[TestMethod]
		public void When_AddIsCalledWithKeyValuePair()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			var value = "something";
			SUT.Values.Add(new KeyValuePair<string, object>(key, value));
			Assert.AreEqual(value, SUT.Values[key]);
		}

		[TestMethod]
		public void When_AddIsCalledRepeatedly()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			var value = "something";
			var secondValue = "somethingElse";
			SUT.Values.Add(key, value);
			Assert.ThrowsExactly<ArgumentException>(
				() => SUT.Values.Add(key, secondValue));
		}

		[TestMethod]
		public void When_IndexerClearsSettingWithNull()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			var value = "something";
			SUT.Values.Add(key, value);
			SUT.Values[key] = null;
			Assert.IsFalse(SUT.Values.ContainsKey(key));
		}

		[TestMethod]
		public void When_AddNullTriesToClearSetting()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var key = "test";
			var value = "something";
			SUT.Values.Add(key, value);
			Assert.ThrowsExactly<ArgumentException>(
				() => SUT.Values.Add(key, null));
		}

		[TestMethod]
		public void When_KeyDoesNotExist()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			Assert.IsNull(SUT.Values["ThisKeyDoesNotExist"]);
		}

		[TestMethod]
		public void When_KeyDoesNotExist_TryGetValue()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			Assert.IsFalse(SUT.Values.TryGetValue("ThisKeyDoesNotExist", out var value));
			Assert.IsNull(value);
		}

		[TestMethod]
		public void When_KeyDoesNotExist_Remove()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			Assert.IsFalse(SUT.Values.Remove("ThisKeyDoesNotExist"));
		}

		[TestMethod]
		public void When_KeyDoesNotExist_ContainsKey()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			Assert.IsFalse(SUT.Values.ContainsKey("ThisKeyDoesNotExist"));
		}

		[TestMethod]
		public void When_Store_Composite()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			var composite = new ApplicationDataCompositeValue();
			composite["key1"] = "value1";
			composite["key2"] = "value2";

			SUT.Values["composite"] = composite;

			var result = SUT.Values["composite"] as ApplicationDataCompositeValue;

			Assert.IsNotNull(result);
			Assert.IsTrue(result.ContainsKey("key1"));
			Assert.IsFalse(result.ContainsKey("key3"));
		}

		[TestMethod]
		public void When_Change_Key_In_Composite_Instance()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			var composite = new ApplicationDataCompositeValue();
			composite["key1"] = "value1";
			composite["key2"] = "value2";

			SUT.Values["composite"] = composite;

			var result = SUT.Values["composite"] as ApplicationDataCompositeValue;

			Assert.IsNotNull(result);
			Assert.IsTrue(result.ContainsKey("key1"));
			Assert.IsFalse(result.ContainsKey("key3"));

			result["key1"] = "value3";

			result = SUT.Values["composite"] as ApplicationDataCompositeValue;
			Assert.IsNotNull(result);

			Assert.AreEqual("value1", result["key1"]);
		}

		[TestMethod]
		public void When_Remove_Key_From_Composite_Instance()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			var composite = new ApplicationDataCompositeValue();
			composite["key1"] = "value1";
			composite["key2"] = "value2";

			SUT.Values["composite"] = composite;

			var result = SUT.Values["composite"] as ApplicationDataCompositeValue;

			Assert.IsNotNull(result);
			Assert.IsTrue(result.ContainsKey("key1"));
			Assert.IsFalse(result.ContainsKey("key3"));

			result.Remove("key1");

			result = SUT.Values["composite"] as ApplicationDataCompositeValue;
			Assert.IsNotNull(result);

			Assert.IsTrue(result.ContainsKey("key1"));
		}

		[TestMethod]
		public void When_Clear_Composite_Instance()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			var composite = new ApplicationDataCompositeValue();
			composite["key1"] = "value1";
			composite["key2"] = "value2";

			SUT.Values["composite"] = composite;

			var result = SUT.Values["composite"] as ApplicationDataCompositeValue;

			Assert.IsNotNull(result);
			Assert.IsTrue(result.ContainsKey("key1"));
			Assert.IsFalse(result.ContainsKey("key3"));

			result.Clear();

			result = SUT.Values["composite"] as ApplicationDataCompositeValue;
			Assert.IsNotNull(result);

			Assert.AreEqual(2, result.Count);
		}

		[TestMethod]
		public void When_Composite_Changed_Overwrite()
		{
			var SUT = ApplicationData.Current.LocalSettings;

			var composite = new ApplicationDataCompositeValue();
			composite["key1"] = "value1";
			composite["key2"] = "value2";

			SUT.Values["composite"] = composite;

			var result = SUT.Values["composite"] as ApplicationDataCompositeValue;

			Assert.IsNotNull(result);
			Assert.IsTrue(result.ContainsKey("key1"));
			Assert.IsFalse(result.ContainsKey("key3"));

			result["key1"] = "value3";
			result["key3"] = "value4";

			SUT.Values["composite"] = result;

			result = SUT.Values["composite"] as ApplicationDataCompositeValue;
			Assert.IsNotNull(result);

			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("value3", result["key1"]);
			Assert.AreEqual("value2", result["key2"]);
			Assert.AreEqual("value4", result["key3"]);
		}
	}
}
