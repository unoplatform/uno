using CommonServiceLocator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Uno.Disposables;
using System.ComponentModel;
using Uno.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using System.Reflection;

namespace Uno.UI.Tests.BinderTests
{
	[TestClass]
	public partial class Given_Binder_FastConvert
	{
		[DataTestMethod]
		[DataRow(typeof(Visibility))]
		[DataRow(typeof(VerticalAlignment))]
		[DataRow(typeof(HorizontalAlignment))]
		[DataRow(typeof(Orientation))]
		[DataRow(typeof(TextAlignment))]
		public void When_String_WithType(Type enumType)
		{
			ValidateEnumType(enumType);
		}

		[TestMethod]
		public void When_String_To_FontWeight()
		{
			var allWeights = typeof(FontWeights).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (var weight in allWeights)
			{
				object expected = weight.GetValue(null);

				object actual = BindingPropertyHelper.Convert(() => typeof(FontWeight), weight.Name);
				Assert.AreEqual(expected, actual);

				object actualLower = BindingPropertyHelper.Convert(() => typeof(FontWeight), weight.Name.ToLowerInvariant());
				Assert.AreEqual(expected, actualLower);
			}
		}

		[TestMethod]
		public void When_Double_To_GridLength()
		{

			Assert.AreEqual(new GridLength(42.0, GridUnitType.Pixel), Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(GridLength), 42.0));
		}

		private static void ValidateEnumType(Type enumType)
		{
			var names = Enum.GetNames(enumType);
			var values = Enum.GetValues(enumType);

			for (int i = 0; i < names.Length; i++)
			{
				object expected = values.GetValue(i);
				object actual = BindingPropertyHelper.Convert(() => enumType, names[i]);
				Assert.AreEqual(expected, actual);

				object actualLower = BindingPropertyHelper.Convert(() => enumType, names[i].ToLowerInvariant());
				Assert.AreEqual(expected, actual);
			}
		}
	}
}
