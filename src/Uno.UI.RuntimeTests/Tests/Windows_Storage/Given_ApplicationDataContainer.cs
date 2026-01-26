using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Uno.UI.Samples.Tests.Windows_Storage
{
	[TestClass]
	public class Given_ApplicationDataContainer
	{
		[TestCleanup]
		public void Cleanup() => ClearAllSettings();

		[TestInitialize]
		public void Initialize() => ClearAllSettings();

		private void ClearAllSettings()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			SUT.Values.Clear();
			foreach (var container in SUT.Containers)
			{
				SUT.DeleteContainer(container.Key);
			}
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

			Assert.DoesNotContain("test", SUT.Values.Keys);
			Assert.DoesNotContain("test2", SUT.Values.Keys);
			Assert.DoesNotContain("42", SUT.Values.Values);
			Assert.DoesNotContain("43", SUT.Values.Values);

			SUT.Values.Add("test", "42");
			SUT.Values.Add("test2", "43");

			Assert.HasCount(originalCount + 2, SUT.Values);
			Assert.HasCount(originalCount + 2, SUT.Values.Keys);
			Assert.Contains("test", SUT.Values.Keys);
			Assert.Contains("test2", SUT.Values.Keys);

			Assert.HasCount(originalCount + 2, SUT.Values.Values);
			Assert.Contains("42", SUT.Values.Values);
			Assert.Contains("43", SUT.Values.Values);
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

			Assert.HasCount(0, keysPresent);
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
		public void When_Container_Values_Type()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var instance = SUT.Values;
			Assert.IsInstanceOfType(SUT.Values, typeof(ApplicationDataContainerSettings));
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

			Assert.HasCount(2, result);
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

			Assert.HasCount(3, result);
			Assert.AreEqual("value3", result["key1"]);
			Assert.AreEqual("value2", result["key2"]);
			Assert.AreEqual("value4", result["key3"]);
		}

		[TestMethod]
		public void When_Nested_Container_Create()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
		}

		[TestMethod]
		public void When_Nested_Container_Create_Existing_Always()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			container.Values["test"] = "42";
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
			var container2 = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container2);
			Assert.AreEqual("nested", container2.Name);
			Assert.HasCount(1, SUT.Containers);
			Assert.AreEqual("42", container2.Values["test"]);
		}

		[TestMethod]
		public void When_Nested_Container_Create_Existing_NotFound()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nonexistent", ApplicationDataCreateDisposition.Existing);
			Assert.IsNull(container);
			Assert.HasCount(0, SUT.Containers);
		}

		[TestMethod]
		public void When_Nested_Container_Create_Existing_Found()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			container.Values["test"] = "42";
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);

			var container2 = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Existing);
			Assert.IsNotNull(container2);
			Assert.AreEqual("nested", container2.Name);
			Assert.HasCount(1, SUT.Containers);
			Assert.AreEqual("42", container2.Values["test"]);
		}

		[TestMethod]
		public void When_Nested_Container_Create_Existing_Nested_NotFound()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);

			var nestedContainer = container.CreateContainer("nonexistent", ApplicationDataCreateDisposition.Existing);
			Assert.IsNull(nestedContainer);
			Assert.HasCount(0, container.Containers);
		}

		[TestMethod]
		public void When_Nested_Container_Create_Existing_Nested_Found()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			var innerContainer = container.CreateContainer("inner", ApplicationDataCreateDisposition.Always);
			innerContainer.Values["innerValue"] = "123";

			var foundContainer = container.CreateContainer("inner", ApplicationDataCreateDisposition.Existing);
			Assert.IsNotNull(foundContainer);
			Assert.AreEqual("inner", foundContainer.Name);
			Assert.AreEqual("123", foundContainer.Values["innerValue"]);
		}

		[TestMethod]
		public void When_Multiple_Nesting_Containers_Structure()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
			var container2 = container.CreateContainer("nested2", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container2);
			Assert.AreEqual("nested2", container2.Name);
			Assert.HasCount(1, container.Containers);

			var container3 = container.CreateContainer("nested3", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container3);
			Assert.AreEqual("nested3", container3.Name);
			Assert.HasCount(2, container.Containers);

			Assert.HasCount(1, SUT.Containers);

			var containerRoot2 = SUT.CreateContainer("nestedRoot2", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(containerRoot2);
			Assert.AreEqual("nestedRoot2", containerRoot2.Name);
			Assert.HasCount(2, SUT.Containers);
		}

		[TestMethod]
		public void When_Nested_Container_Delete()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
			SUT.DeleteContainer("nested");
			Assert.HasCount(0, SUT.Containers);
		}

		[TestMethod]
		public void When_Nested_Container_Value_Not_In_Root()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
			container.Values["test"] = "42";
			Assert.AreEqual("42", container.Values["test"]);
			Assert.IsFalse(SUT.Values.ContainsKey("test"));

			SUT.Values["test"] = "43";
			Assert.AreEqual("43", SUT.Values["test"]);
			Assert.AreEqual("42", container.Values["test"]);
		}

		[TestMethod]
		public void When_Nested_Container_Not_Included_In_Values()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var container = SUT.CreateContainer("nested", ApplicationDataCreateDisposition.Always);
			Assert.IsNotNull(container);
			Assert.AreEqual("nested", container.Name);
			Assert.HasCount(1, SUT.Containers);
			container.Values["test"] = "42";
			Assert.HasCount(1, SUT.Containers);
			Assert.HasCount(0, SUT.Values);
		}

		// Note: WinUI does not raise MapChanged events for ApplicationDataContainerSettings,
		// but Uno Platform does for consistency with other IObservableMap implementations.

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Indexer_Insert()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			CollectionChange? lastChange = null;
			string lastKey = null;
			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				lastChange = args.CollectionChange;
				lastKey = args.Key;
				eventCount++;
			};

			SUT.Values["mapchanged_insert_test"] = "value";

			Assert.AreEqual(1, eventCount);
			Assert.AreEqual(CollectionChange.ItemInserted, lastChange);
			Assert.AreEqual("mapchanged_insert_test", lastKey);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Indexer_Update()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			SUT.Values["mapchanged_update_test"] = "value1";

			CollectionChange? lastChange = null;
			string lastKey = null;
			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				lastChange = args.CollectionChange;
				lastKey = args.Key;
				eventCount++;
			};

			SUT.Values["mapchanged_update_test"] = "value2";

			Assert.AreEqual(1, eventCount);
			Assert.AreEqual(CollectionChange.ItemChanged, lastChange);
			Assert.AreEqual("mapchanged_update_test", lastKey);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Add()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			CollectionChange? lastChange = null;
			string lastKey = null;
			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				lastChange = args.CollectionChange;
				lastKey = args.Key;
				eventCount++;
			};

			SUT.Values.Add("mapchanged_add_test", "value");

			Assert.AreEqual(1, eventCount);
			Assert.AreEqual(CollectionChange.ItemInserted, lastChange);
			Assert.AreEqual("mapchanged_add_test", lastKey);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Remove()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			SUT.Values["mapchanged_remove_test"] = "value";

			CollectionChange? lastChange = null;
			string lastKey = null;
			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				lastChange = args.CollectionChange;
				lastKey = args.Key;
				eventCount++;
			};

			SUT.Values.Remove("mapchanged_remove_test");

			Assert.AreEqual(1, eventCount);
			Assert.AreEqual(CollectionChange.ItemRemoved, lastChange);
			Assert.AreEqual("mapchanged_remove_test", lastKey);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Remove_NonExistent()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				eventCount++;
			};

			SUT.Values.Remove("mapchanged_nonexistent_key");

			Assert.AreEqual(0, eventCount);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MapChanged_Clear()
		{
			var SUT = ApplicationData.Current.LocalSettings;
			var settings = (IObservableMap<string, object>)SUT.Values;

			SUT.Values["mapchanged_clear_test1"] = "value1";
			SUT.Values["mapchanged_clear_test2"] = "value2";

			CollectionChange? lastChange = null;
			string lastKey = null;
			int eventCount = 0;

			settings.MapChanged += (sender, args) =>
			{
				lastChange = args.CollectionChange;
				lastKey = args.Key;
				eventCount++;
			};

			SUT.Values.Clear();

			Assert.AreEqual(1, eventCount);
			Assert.AreEqual(CollectionChange.Reset, lastChange);
			Assert.IsNull(lastKey);
		}
	}
}
