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

		[TestMethod]
		public void When_Setter_Wins_Then_Value_Is_Materialized_Once()
		{
			var count = 0;
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromStyle", () => count++));

			Control control = new();
			control.Style = SUT;

			Assert.AreEqual("fromStyle", control.Tag);
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void When_Setter_Overridden_By_Local_Value_Then_Value_Is_Not_Materialized()
		{
			var count = 0;
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromStyle", () => count++));

			Control control = new();
			control.Tag = "local";
			control.Style = SUT;

			Assert.AreEqual("local", control.Tag);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void When_Overriding_Local_Value_Cleared_Then_Deferred_Setter_Is_Materialized()
		{
			var count = 0;
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromStyle", () => count++));

			Control control = new();
			control.Tag = "local";
			control.Style = SUT;

			Assert.AreEqual(0, count);

			control.ClearValue(FrameworkElement.TagProperty);

			Assert.AreEqual("fromStyle", control.Tag);
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void When_BuiltIn_Setter_Overridden_By_Explicit_Style_Then_Value_Is_Not_Materialized()
		{
			var builtInCount = 0;
			var explicitCount = 0;

			var builtInStyle = new Style(typeof(Control));
			builtInStyle.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "builtIn", () => builtInCount++));

			var explicitStyle = new Style(typeof(Control));
			explicitStyle.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "explicit", () => explicitCount++));

			Control control = new();
			control.Style = explicitStyle;
			builtInStyle.ApplyTo(control, DependencyPropertyValuePrecedences.ImplicitStyle);

			Assert.AreEqual("explicit", control.Tag);
			Assert.AreEqual(0, builtInCount);

			// The winning setter must be materialized exactly once, even though the built-in style
			// application used to re-apply the explicit setter on top of it.
			Assert.AreEqual(1, explicitCount);
		}

		[TestMethod]
		public void When_BuiltIn_Template_Overridden_By_Explicit_Style_Then_Template_Is_Not_Materialized()
		{
			var builtInCount = 0;
			var explicitCount = 0;

			var explicitTemplate = new ControlTemplate(() => null);

			var builtInStyle = new Style(typeof(Control));
			builtInStyle.Setters.Add(CreateCountingSetter(Control.TemplateProperty, new ControlTemplate(() => null), () => builtInCount++));

			var explicitStyle = new Style(typeof(Control));
			explicitStyle.Setters.Add(CreateCountingSetter(Control.TemplateProperty, explicitTemplate, () => explicitCount++));

			Control control = new();
			control.Style = explicitStyle;
			builtInStyle.ApplyTo(control, DependencyPropertyValuePrecedences.ImplicitStyle);

			Assert.AreSame(explicitTemplate, control.Template);
			Assert.AreEqual(0, builtInCount);
			Assert.AreEqual(1, explicitCount);
		}

		[TestMethod]
		public void When_BasedOn_Setter_Overridden_By_Local_Value_Then_Value_Is_Not_Materialized()
		{
			var count = 0;

			var baseStyle = new Style(typeof(Control));
			baseStyle.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromBase", () => count++));

			var SUT = new Style(typeof(Control)) { BasedOn = baseStyle };
			SUT.Setters.Add(new Setter(Control.IsTabStopProperty, false));

			Control control = new();
			control.Tag = "local";
			control.Style = SUT;

			Assert.AreEqual("local", control.Tag);
			Assert.IsFalse(control.IsTabStop);
			Assert.AreEqual(0, count);

			control.ClearValue(FrameworkElement.TagProperty);

			Assert.AreEqual("fromBase", control.Tag);
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void When_Style_Replaced_While_Overridden_Then_Deferred_Setter_Uses_New_Style()
		{
			var countA = 0;
			var countB = 0;

			var styleA = new Style(typeof(Control));
			styleA.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "A", () => countA++));

			var styleB = new Style(typeof(Control));
			styleB.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "B", () => countB++));

			Control control = new();
			control.Tag = "local";
			control.Style = styleA;
			control.Style = styleB;

			Assert.AreEqual("local", control.Tag);
			Assert.AreEqual(0, countA);
			Assert.AreEqual(0, countB);

			control.ClearValue(FrameworkElement.TagProperty);

			Assert.AreEqual("B", control.Tag);
			Assert.AreEqual(0, countA);
		}

		[TestMethod]
		public void When_Style_Cleared_While_Overridden_Then_Deferred_Setter_Is_Not_Materialized()
		{
			var count = 0;

			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromStyle", () => count++));

			Control control = new();
			control.Tag = "local";
			control.Style = SUT;
			control.Style = null;

			Assert.AreEqual("local", control.Tag);
			Assert.AreEqual(0, count);

			control.ClearValue(FrameworkElement.TagProperty);

			Assert.IsNull(control.Tag);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void When_Deferral_Disabled_Then_Overridden_Setter_Is_Materialized()
		{
			var count = 0;
			var SUT = new Style(typeof(Control));
			SUT.Setters.Add(CreateCountingSetter(FrameworkElement.TagProperty, "fromStyle", () => count++));

			try
			{
				FeatureConfiguration.Style.DeferOverriddenSetterValues = false;

				Control control = new();
				control.Tag = "local";
				control.Style = SUT;

				Assert.AreEqual("local", control.Tag);
				Assert.AreEqual(1, count);
			}
			finally
			{
				FeatureConfiguration.Style.DeferOverriddenSetterValues = true;
			}
		}

		private static Setter CreateCountingSetter(DependencyProperty property, object value, Action onMaterialized)
		{
			SetterValueProviderHandler provider = () =>
			{
				onMaterialized();
				return value;
			};

			return new Setter(property, provider);
		}
	}
}
