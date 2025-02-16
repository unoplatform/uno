using System;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests;

[TestClass]
public class DependencyProperty_Type
{
	[TestMethod]
	public void Check_All_DP_Data_Types()
	{
		var getOwnerType = GetOwnerGetter();

		var asm = typeof(FrameworkElement).Assembly;

		foreach (var dependencyObject in asm.DefinedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(DependencyObject))))
		{
			// If there is a static DependencyProperty property with name "XProperty" and also a instance property with the name "X"
			// check whether the type of X == the "PropertyType" of the DependencyProperty property
			var dependencyProperties = dependencyObject.DeclaredProperties
				.Where(p => p.PropertyType == typeof(DependencyProperty) &&
					p.Name.EndsWith("Property") &&
					p.GetCustomAttributes(typeof(NotImplementedAttribute), true).Length == 0 &&
					(p.GetGetMethod()?.IsStatic ?? false)
				);

			foreach (var dpInfo in dependencyProperties)
			{
				var associatedInstanceProperty = dependencyObject.DeclaredProperties.FirstOrDefault(
					p => p.Name == dpInfo.Name.Substring(0, dpInfo.Name.Length - "Property".Length)
				);

				if (associatedInstanceProperty is null)
				{
					continue;
				}

				var type = associatedInstanceProperty?.PropertyType;
				var dp = (DependencyProperty)dpInfo.GetValue(null);

				CheckType(dp, type);
			}
		}
	}

	[TestMethod]
	public void Check_All_DP_Default_Value_Types()
	{
		var getOwnerType = GetOwnerGetter();
		var asm = typeof(FrameworkElement).Assembly;
		foreach (var dependencyObject in asm.DefinedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(DependencyObject))))
		{
			var dependencyProperties = dependencyObject.DeclaredProperties
				.Where(p => p.PropertyType == typeof(DependencyProperty) &&
					p.GetCustomAttributes(typeof(NotImplementedAttribute), true).Length == 0 &&
					(p.GetGetMethod()?.IsStatic ?? false)
				);

			foreach (var dpInfo in dependencyProperties)
			{
				var dp = (DependencyProperty)dpInfo.GetValue(null);
				CheckDefaultValue(dp);
			}
		}
	}

	private void CheckDefaultValue(DependencyProperty property)
	{
		var dpSpecifiedType = property.Type;
		var dpDefaultValue = property.GetDefaultValue(null, property.OwnerType);
		if (dpDefaultValue is null)
		{
			// type needs to be a reference type
			Assert.IsFalse(dpSpecifiedType.IsValueType);
		}
		else
		{
			// type needs to be same type or derived from it
			Assert.IsTrue(dpSpecifiedType.IsAssignableFrom(dpDefaultValue.GetType()));
		}
	}

	private static void CheckType(DependencyProperty dp, Type expectedType)
	{
		var dpSpecifiedType = dp.Type;

		if (expectedType != typeof(PasswordRevealMode))
		{
			Assert.AreEqual(expectedType, dpSpecifiedType, $"Type mismatch for {dp.Name} on {dp.OwnerType}");
		}
	}

	private static Func<DependencyProperty, Type> GetOwnerGetter()
	{
		var all = typeof(DependencyProperty).GetProperties();
		var all2 = typeof(DependencyProperty).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
		var p = typeof(DependencyProperty).GetProperty("OwnerType", BindingFlags.NonPublic | BindingFlags.Instance);

		return o => (Type)p.GetValue(o);
	}
}
