using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace Uno.UI.Tests.Windows_Storage;

[TestClass]
public class Given_ApplicationDataCompositeValue
{
	[TestMethod]
	public void When_Create_Instance()
	{
		// Arrange & Act
		var result = new ApplicationDataCompositeValue();

		// Assert
		result.Count.Should().Be(0);
	}

#if HAS_UNO
	[TestMethod]
	public void When_Create_Instance_With_Dictionary()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue(dictionary);

		// Assert
		result.Count.Should().Be(2);
		result["key1"].Should().Be("value1");
		result["key2"].Should().Be("value2");
	}
#endif

	[TestMethod]
	public void When_Add_Items()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Count.Should().Be(2);
		result["key1"].Should().Be("value1");
		result["key2"].Should().Be("value2");
	}

	[TestMethod]
	public void When_Contains_Key()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.ContainsKey("key1").Should().BeTrue();
		result.ContainsKey("key3").Should().BeFalse();
	}

	[TestMethod]
	public void When_Remove_Key()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Remove("key1").Should().BeTrue();
		result.ContainsKey("key1").Should().BeFalse();
		result.Count.Should().Be(1);
	}

	[TestMethod]
	public void When_Try_Get_Value()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.TryGetValue("key1", out var value).Should().BeTrue();
		value.Should().Be("value1");
		result.TryGetValue("key3", out _).Should().BeFalse();
	}

	[TestMethod]
	public void When_Get_Set_Value()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result["key1"].Should().Be("value1");
		result["key1"] = "value3";
		result["key1"].Should().Be("value3");
	}

	[TestMethod]
	public void When_MapChanged_Event()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		int count = 0;
		var result = new ApplicationDataCompositeValue();
		result.MapChanged += (s, e) => count++;

		// Act
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		count.Should().Be(2);
	}

	[TestMethod]
	public void When_Is_ReadOnly()
	{
		// Arrange & Act
		var result = new ApplicationDataCompositeValue();

		// Assert
		result.IsReadOnly.Should().BeFalse();
	}

	[TestMethod]
	public void When_Get_Count()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Count.Should().Be(2);
	}

	[TestMethod]
	public void When_Clear()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}
		result.Clear();

		// Assert
		result.Count.Should().Be(0);
	}

	[TestMethod]
#if RUNTIME_NATIVE_AOT
	[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
	public void When_Values()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Values.Order().ToArray().Should().BeEquivalentTo(new[] { "value1", "value2" });
	}

	[TestMethod]
#if RUNTIME_NATIVE_AOT
	[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
	public void When_Keys()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Keys.Order().ToArray().Should().BeEquivalentTo(new[] { "key1", "key2" });
	}

	[TestMethod]
	public void When_Set_Null()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		// Assert
		result.Count.Should().Be(2);

		result["key2"] = null;

		result.Count.Should().Be(1);
	}

	[TestMethod]
	public void When_Add_Null()
	{
		// Arrange
		var dictionary = new Dictionary<string, object>
			{
				{ "key1", "value1" },
				{ "key2", "value2" }
			};

		// Act
		var result = new ApplicationDataCompositeValue();
		foreach (var item in dictionary)
		{
			result.Add(item.Key, item.Value);
		}

		result.Add("key3", null);

		// Assert
		result.Count.Should().Be(2);
	}
}
