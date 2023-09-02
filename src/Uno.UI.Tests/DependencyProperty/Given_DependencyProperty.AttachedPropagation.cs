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
using System.Threading;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.AttachedPropagation
{
	[TestClass]
	public partial class Given_DependencyProperty_AttachedPropagation
	{
		[TestMethod]
		public void When_SimpleInheritance_Late()
		{
			var root = new Grid();
			var child1 = new Grid();

			root.Children.Add(child1);

			MyAttachedPropType.SetMyProperty(root, 42);
			Assert.AreEqual(0, MyAttachedPropType.GetMyProperty(child1));

			MyAttachedPropType.SetMyProperty(root, 43);
			Assert.AreEqual(43, MyAttachedPropType.GetMyProperty(child1));
		}

		[TestMethod]
		public void When_SimpleInheritance_Early()
		{
			var root = new Grid();
			var child1 = new Grid();
			Assert.AreEqual(0, MyAttachedPropType.GetMyProperty(child1));

			root.Children.Add(child1);

			MyAttachedPropType.SetMyProperty(root, 42);

			Assert.AreEqual(42, MyAttachedPropType.GetMyProperty(child1));
		}

		[TestMethod]
		public void When_SimpleInheritance_Late_Event()
		{
			var root = new Grid();
			var child1 = new Grid();

			root.Children.Add(child1);

			int propagatedValue = -1;
			child1.RegisterPropertyChangedCallback(
				MyAttachedPropType.MyPropertyProperty,
				(s, e) =>
				{
					propagatedValue = MyAttachedPropType.GetMyProperty(child1);
				}
			);

			Assert.AreEqual(-1, propagatedValue);
			MyAttachedPropType.SetMyProperty(root, 42);

			Assert.AreEqual(42, MyAttachedPropType.GetMyProperty(child1));
			Assert.AreEqual(42, propagatedValue);
		}

		[TestMethod]
		public void When_SimpleInheritance_Early_Event()
		{
			var root = new Grid();
			var child1 = new Grid();

			int propagatedValue = -1;
			child1.RegisterPropertyChangedCallback(
				MyAttachedPropType.MyPropertyProperty,
				(s, e) =>
				{
					propagatedValue = MyAttachedPropType.GetMyProperty(child1);
				}
			);

			Assert.AreEqual(-1, propagatedValue);
			MyAttachedPropType.SetMyProperty(root, 42);

			root.Children.Add(child1);

			Assert.AreEqual(42, MyAttachedPropType.GetMyProperty(child1));
			Assert.AreEqual(42, propagatedValue);
		}
	}

	public class MyAttachedPropType
	{
		public static int GetMyProperty(DependencyObject obj)
		{
			return (int)obj.GetValue(MyPropertyProperty);
		}

		public static void SetMyProperty(DependencyObject obj, int value)
		{
			obj.SetValue(MyPropertyProperty, value);
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.RegisterAttached(
				name: "MyProperty",
				propertyType: typeof(int),
				ownerType: typeof(MyAttachedPropType),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: OnPropertyChanged
				)
			);

		private static void OnPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}
	}
}
