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

namespace Uno.UI.Tests.BinderTests.DependencyPropertyPath
{
	[TestClass]
	public partial class Given_Binder_DependencyPropertyPath
	{
		[TestMethod]
		public void When_AttachedDependencyProperty_And_SimplePath()
		{
			Control1.OtherControlProperty.ToString();

			var source = new Control1();
			var attachedSource = new Control2();

			// The property needs to be set at least once (so that it is actually attached)
			Attachable.SetMyValue(attachedSource, 21);

			var target = new Control1();

			target.SetBinding(
				Control1.OtherControlProperty,
				new Binding($"{nameof(source.OtherControl)}.({typeof(Attachable).Namespace}:{nameof(Attachable)}.MyValue)")
				{
					Mode = BindingMode.TwoWay,
					Source = source
				}
			);

			Assert.AreEqual(null, target.OtherControl);

			source.OtherControl = attachedSource;

			Assert.AreEqual(21, target.OtherControl);

			Attachable.SetMyValue(attachedSource, 42);
			Assert.AreEqual(42, target.OtherControl);
		}

		public partial class BaseTarget : DependencyObject
		{
			public BaseTarget(BaseTarget parent = null)
			{
				this.SetParent(parent);
			}
		}

		public class Control1 : BaseTarget
		{
			public object OtherControl
			{
				get { return (object)DependencyObjectExtensions.GetValue(this, OtherControlProperty); }
				set { DependencyObjectExtensions.SetValue(this, OtherControlProperty, value); }
			}

			public static readonly DependencyProperty OtherControlProperty =
				DependencyProperty.Register("OtherControl", typeof(object), typeof(Control1), new FrameworkPropertyMetadata(null));
		}

		public class Control2 : BaseTarget
		{
		}
	}

	public class Attachable
	{
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.RegisterAttached(
				"MyValue",
				typeof(int),
				typeof(Attachable),
				new FrameworkPropertyMetadata(0)
			);

		public static int GetMyValue(object view)
		{
			return (int)DependencyObjectExtensions.GetValue(view, MyValueProperty);
		}

		public static void SetMyValue(object view, int row)
		{
			DependencyObjectExtensions.SetValue(view, MyValueProperty, row);
		}
	}
}
