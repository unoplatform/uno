using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;

namespace Uno.UI.Tests
{
	[TestClass]
	public class DependencyProperty_Owner
	{

		[TestMethod]
		public void Check_All_DP_Owners()
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

				var dependencyPropertyFields = dependencyObject.DeclaredFields
					.Where(f => f.FieldType == typeof(DependencyProperty) &&
						f.GetCustomAttributes(typeof(NotImplementedAttribute), true).Length == 0 &&
						f.IsStatic
					);

				foreach (var dpInfo in dependencyProperties)
				{
					var dp = (DependencyProperty)dpInfo.GetValue(null);
					var owner = getOwnerType(dp);
					Assert.AreEqual(dependencyObject.AsType(), owner);
				}

				foreach (var dpInfo in dependencyPropertyFields)
				{
					var dp = (DependencyProperty)dpInfo.GetValue(null);
					var owner = getOwnerType(dp);
					Assert.AreEqual(dependencyObject.AsType(), owner);
				}
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
}
