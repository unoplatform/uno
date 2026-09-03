#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public partial class Given_FrameworkTemplate;

#if HAS_UNO
partial class Given_FrameworkTemplate // tests
{
	[TestMethod]
	public void When_TemplatesHaveSameFactoryTarget_Then_AreEqual()
	{
		// Arrange
		FrameworkTemplateBuilder factory = (_, _) => new Border();
		var template1 = new FrameworkTemplate(null, factory);
		var template2 = new FrameworkTemplate(null, factory);

		// Act & Assert
		Assert.AreEqual(template1, template2);
		Assert.AreEqual(template1.GetHashCode(), template2.GetHashCode());
	}

	[TestMethod]
	public void When_TemplatesHaveSameStaticFactoryTarget_Then_AreEqual()
	{
		// Arrange
		static UIElement StaticLocalMethodFactory(object? owner, TemplateMaterializationSettings settings) => new Border();
		var template1 = new FrameworkTemplate(null, StaticLocalMethodFactory);
		var template2 = new FrameworkTemplate(null, StaticLocalMethodFactory);

		// Act & Assert
		Assert.AreEqual(template1, template2);
		Assert.AreEqual(template1.GetHashCode(), template2.GetHashCode());
	}

	[TestMethod]
	public void When_TemplatesHaveDifferentFactories_Then_AreNotEqual()
	{
		// Arrange
		var template1 = new FrameworkTemplate(null, (_, _) => new Border());
		var template2 = new FrameworkTemplate(null, (_, _) => new TextBlock());

		// Act & Assert
		Assert.AreNotEqual(template1, template2);
	}

	[TestMethod]
	public async Task InstancedFactory_ShouldNotLeak()
	{
		// Arrange
		var setup = new XamlPageSetup();
		var template = setup.GetTemplate(nameof(setup.InstancedBuilder));
		var setupWR = new WeakReference(setup);

		// Act
		setup = null;
		await TestHelper.TryWaitUntilCollected(setupWR);

		// Assert
		Assert.IsNull(setupWR.Target);
	}

	[TestMethod]
	public async Task LambdaExpressionFactory1_ShouldNotBeCollected()
	{
		// Arrange
		var context = new object();
		FrameworkTemplateBuilder? builder = (_, _) => new ContentPresenter { Content = context };
		Assert.IsNotNull(builder.Target, "The delegate is expected to have a target here");

		var template = new DataTemplate(null, builder);
		var targetWR = new WeakReference(builder.Target);

		// Act
		builder = null;
		await TestHelper.TryWaitUntilCollected(targetWR);

		// Assert
		Assert.IsNotNull(targetWR.Target);

		// prevent the `template` from being collected before the assertion, which would otherwise brick this test.
		// since we would expect the `template` to keep `targetWR.Target` alive.
		GC.KeepAlive(template);
	}

	[TestMethod]
	public async Task LambdaExpressionFactory2_ShouldNotBeCollected()
	{
		// Arrange
		FrameworkTemplateBuilder? builder = (_, _) => new Border();
		Assert.IsNotNull(builder.Target, "The delegate is expected to have a target here");

		var template = new DataTemplate(null, builder);
		var targetWR = new WeakReference(builder.Target);

		// Act
		builder = null;
		await TestHelper.TryWaitUntilCollected(targetWR);

		// Assert
		Assert.IsNotNull(targetWR.Target);

		// completely unnecessary, but the `template` technically should not be collected before `targetWR.Target` does.
		GC.KeepAlive(template);
	}
}

partial class Given_FrameworkTemplate
{
	public partial class XamlPageSetup : Page
	{
		public DataTemplate GetTemplate(string name)
		{
			return name switch
			{
				"InstancedBuilder" => new DataTemplate(owner: null, factory: InstancedBuilder),
				"StaticBuilder" => new DataTemplate(owner: null, factory: StaticBuilder),

				_ => throw new KeyNotFoundException(nameof(name)),
			};
		}

		public View? InstancedBuilder(object? owner, TemplateMaterializationSettings settings) => new Border();
		public static View? StaticBuilder(object? owner, TemplateMaterializationSettings settings) => new Border();
	}
}
#endif
