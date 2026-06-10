using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.App.Xaml;
using Uno.UI.Tests.Helpers;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_Style
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Sealed_Style_Add_Setter()
		{
			var SUT = new Style(typeof(Control));

			SUT.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			SUT.Seal();

			Assert.IsTrue(SUT.IsSealed);
			Assert.IsTrue(SUT.Setters.IsSealed);
			Assert.IsTrue(SUT.Setters[0].IsSealed);

			Assert.ThrowsExactly<InvalidOperationException>(() => SUT.Setters.Add(new Setter(Control.IsHitTestVisibleProperty, true)));
		}

		[TestMethod]
		public void When_Sealed_Style_Remove()
		{
			var SUT = new Style(typeof(Control));

			SUT.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			SUT.Seal();

			Assert.IsTrue(SUT.IsSealed);
			Assert.IsTrue(SUT.Setters.IsSealed);
			Assert.IsTrue(SUT.Setters[0].IsSealed);

			SUT.Setters.Clear();
		}

		[TestMethod]
		public void When_Sealed_Style_Setter_Update()
		{
			var SUT = new Style(typeof(Control));

			Setter s;
			SUT.Setters.Add(s = new Setter(Control.IsEnabledProperty, true));

			SUT.Seal();

			Assert.IsTrue(SUT.IsSealed);
			Assert.IsTrue(SUT.Setters.IsSealed);
			Assert.IsTrue(SUT.Setters[0].IsSealed);

			Assert.ThrowsExactly<InvalidOperationException>(() => s.Value = null);
		}

		[TestMethod]
		public void When_Sealed_Style_BasedOn_Sealed()
		{
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			var SUT2 = new Style(typeof(Control)) { BasedOn = SUT };
			SUT2.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			SUT2.Seal();

			Assert.IsTrue(SUT.IsSealed);
			Assert.IsTrue(SUT.Setters.IsSealed);
			Assert.IsTrue(SUT.Setters[0].IsSealed);

			Assert.IsTrue(SUT2.IsSealed);
			Assert.IsTrue(SUT2.Setters.IsSealed);
			Assert.IsTrue(SUT2.Setters[0].IsSealed);
		}

		[TestMethod]
		public void When_Sealed_Style_On_Apply()
		{
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			var SUT2 = new Style(typeof(Control)) { BasedOn = SUT };
			SUT2.Setters.Add(new Setter(Control.IsEnabledProperty, true));

			SUT2.Seal();

			Control control = new();
			control.Style = SUT2;

			Assert.IsTrue(SUT.IsSealed);
			Assert.IsTrue(SUT.Setters.IsSealed);
			Assert.IsTrue(SUT.Setters[0].IsSealed);

			Assert.IsTrue(SUT2.IsSealed);
			Assert.IsTrue(SUT2.Setters.IsSealed);
			Assert.IsTrue(SUT2.Setters[0].IsSealed);
		}
	}
}
