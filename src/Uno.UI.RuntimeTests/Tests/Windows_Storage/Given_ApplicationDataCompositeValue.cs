using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		Assert.AreEqual(0, result.Count);
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
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
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
		Assert.AreEqual(2, result.Count);
		Assert.AreEqual("value1", result["key1"]);
		Assert.AreEqual("value2", result["key2"]);
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
		Assert.IsTrue(result.ContainsKey("key1"));
		Assert.IsFalse(result.ContainsKey("key3"));
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
		Assert.IsTrue(result.Remove("key1"));
		Assert.IsFalse(result.ContainsKey("key1"));
		Assert.AreEqual(1, result.Count);
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
		Assert.IsTrue(result.TryGetValue("key1", out var value));
		Assert.AreEqual("value1", value);
		Assert.IsFalse(result.TryGetValue("key3", out _));
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
		Assert.AreEqual("value1", result["key1"]);
		result["key1"] = "value3";
		Assert.AreEqual("value3", result["key1"]);
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
		Assert.AreEqual(2, count);
	}

	[TestMethod]
	public void When_Is_ReadOnly()
	{
		// Arrange & Act
		var result = new ApplicationDataCompositeValue();

		// Assert
		Assert.IsFalse(result.IsReadOnly);
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
		Assert.AreEqual(2, result.Count);
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
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
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
		CollectionAssert.AreEqual(new[] { "value1", "value2" }, result.Values.Order().ToArray());
	}

	[TestMethod]
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
		CollectionAssert.AreEqual(new[] { "key1", "key2" }, result.Keys.Order().ToArray());
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
		Assert.AreEqual(2, result.Count);

		result["key2"] = null;

		Assert.AreEqual(1, result.Count);		
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
		Assert.AreEqual(2, result.Count);		
	}
}
