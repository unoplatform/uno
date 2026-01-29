using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_BindingPropertyHelper_InterfaceProperties
	{
		[TestMethod]
		public void When_GetPropertyType_InterfaceProperty_Array()
		{
			// Arrange: IReadOnlyList<string> backed by an array (which has Length, not Count)
			var arrayType = typeof(string[]);
			
			// Act: Try to get Count property type (which is on IReadOnlyList<T>, not on array)
			var propertyType = BindingPropertyHelper.GetPropertyType(arrayType, "Count", false);
			
			// Assert: Should find Count from IReadOnlyList<T> interface
			Assert.IsNotNull(propertyType, "Count property should be found via IReadOnlyList<T> interface");
			Assert.AreEqual(typeof(int), propertyType);
		}

		[TestMethod]
		public void When_GetValueGetter_InterfaceProperty_Array()
		{
			// Arrange
			var arrayType = typeof(string[]);
			var testArray = new[] { "Item1", "Item2", "Item3" };
			
			// Act: Get a value getter for Count property
			var getter = BindingPropertyHelper.GetValueGetter(arrayType, "Count");
			var count = getter(testArray);
			
			// Assert
			Assert.IsNotNull(count, "Count getter should return a value");
			Assert.AreEqual(3, count);
		}

		[TestMethod]
		public void When_GetValueGetter_InterfaceIndexer_Array()
		{
			// Arrange
			var arrayType = typeof(string[]);
			var testArray = new[] { "Item1", "Item2", "Item3" };
			
			// Act: Get a value getter for indexer via IReadOnlyList<T>
			var getter = BindingPropertyHelper.GetValueGetter(arrayType, "[0]");
			var value = getter(testArray);
			
			// Assert
			Assert.IsNotNull(value, "Indexer getter should return a value");
			Assert.AreEqual("Item1", value);
		}

		[TestMethod]
		public void When_GetPropertyType_InterfaceProperty_List()
		{
			// Arrange: IReadOnlyList<string> backed by a List (which has Count directly)
			var listType = typeof(List<string>);
			
			// Act: Try to get Count property type
			var propertyType = BindingPropertyHelper.GetPropertyType(listType, "Count", false);
			
			// Assert: Should find Count property
			Assert.IsNotNull(propertyType, "Count property should be found");
			Assert.AreEqual(typeof(int), propertyType);
		}

		[TestMethod]
		public void When_GetPropertyType_NonExistentProperty()
		{
			// Arrange
			var arrayType = typeof(string[]);
			
			// Act: Try to get a property that doesn't exist
			var propertyType = BindingPropertyHelper.GetPropertyType(arrayType, "NonExistent", false);
			
			// Assert: Should return null
			Assert.IsNull(propertyType, "Non-existent property should return null");
		}

		[TestMethod]
		public void When_GetPropertyType_ConcreteProperty_Priority()
		{
			// Arrange: Array has Length property directly
			var arrayType = typeof(string[]);
			
			// Act: Try to get Length property type
			var propertyType = BindingPropertyHelper.GetPropertyType(arrayType, "Length", false);
			
			// Assert: Should find Length from array type (not interface)
			Assert.IsNotNull(propertyType, "Length property should be found");
			Assert.AreEqual(typeof(int), propertyType);
		}
	}
}
