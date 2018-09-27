using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.ControlTests
{
	[TestClass]
	public partial class Given_Control
	{
		[TestMethod]
		public void When_ManuallyApplyTemplate()
		{
			var current = FeatureConfiguration.Control.UseLegacyLazyApplyTemplate;
			try
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = true;
				var templatedRoot = default(UIElement);
				var sut = new MyControl
				{
					Template = new ControlTemplate(() => templatedRoot = new Grid())
				};

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				new Grid().Children.Add(sut); // This kind-of simulate that the control is in the visual tree.

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				var applied = sut.ApplyTemplate();

				Assert.IsTrue(applied);
				Assert.IsNotNull(sut.TemplatedRoot);
				Assert.AreSame(templatedRoot, sut.TemplatedRoot);
			}
			finally
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = current;
			}
		}

		[TestMethod]
		public void When_ManuallyApplyTemplate_OutOfVisualTree()
		{
			var current = FeatureConfiguration.Control.UseLegacyLazyApplyTemplate;
			try
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = true;
				var templatedRoot = default(UIElement);
				var sut = new MyControl
				{
					Template = new ControlTemplate(() => templatedRoot = new Grid())
				};

				Assert.IsNull(sut.TemplatedRoot);
				Assert.IsNull(templatedRoot);

				var applied = sut.ApplyTemplate();

				Assert.IsTrue(applied);
				Assert.IsNotNull(sut.TemplatedRoot);
				Assert.AreSame(templatedRoot, sut.TemplatedRoot);
			}
			finally
			{
				FeatureConfiguration.Control.UseLegacyLazyApplyTemplate = current;
			}
		}

		public partial class MyControl : Control
		{
		}
	}
}
