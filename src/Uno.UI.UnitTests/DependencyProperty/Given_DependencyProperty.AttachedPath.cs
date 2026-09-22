#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.BinderTests;

/// <summary>
/// Owner of an attached property that is only ever named through a string binding path, so that its static
/// constructor has not run when the binding is created.
/// </summary>
public static class AttachedPathTestOwner
{
	public static DependencyProperty MarkerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"Marker",
			typeof(string),
			typeof(AttachedPathTestOwner),
			new FrameworkPropertyMetadata(default(string)));

	public static string GetMarker(DependencyObject owner) => (string)owner.GetValue(MarkerProperty);

	public static void SetMarker(DependencyObject owner, string value) => owner.SetValue(MarkerProperty, value);
}

[TestClass]
public partial class Given_DependencyProperty_AttachedPath
{
	[TestMethod]
	public void When_Binding_To_Attached_Path_Of_Uninitialized_Owner()
	{
		// DependencyPropertyDescriptor.Parse redirects the lookup to the attached property's owner, whose
		// registration only happens in its static constructor. Without forcing that constructor the lookup
		// missed and the null was negatively cached, so the binding read the initial value once and never
		// subscribed to changes.
		var source = new Border();
		var target = new Border();

		target.SetBinding(
			Border.TagProperty,
			new Binding
			{
				Path = new PropertyPath("(Uno.UI.Tests.BinderTests:AttachedPathTestOwner.Marker)"),
				Source = source,
			});

		AttachedPathTestOwner.SetMarker(source, "set-after-binding");

		Assert.AreEqual("set-after-binding", target.Tag);
	}

	[TestMethod]
	public void When_Binding_To_Attached_Path_Then_Notifies()
	{
		var source = new Border();
		var target = new Border();

		target.SetBinding(
			Border.TagProperty,
			new Binding
			{
				Path = new PropertyPath("(Microsoft.UI.Xaml.Controls:Canvas.Left)"),
				Source = source,
			});

		Canvas.SetLeft(source, 42d);
		Assert.AreEqual(42d, target.Tag);

		Canvas.SetLeft(source, 43d);
		Assert.AreEqual(43d, target.Tag);
	}
}
