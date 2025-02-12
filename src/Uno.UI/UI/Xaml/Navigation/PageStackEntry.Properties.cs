using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation;

partial class PageStackEntry
{
	/// <summary>
	/// Gets a value that indicates the animated transition associated with the navigation entry.
	/// </summary>
	public NavigationTransitionInfo NavigationTransitionInfo { get; internal set; }

	/// <summary>
	/// Gets the navigation parameter associated with this navigation entry.
	/// </summary>
	public object Parameter { get; private set; }

	/// <summary>
	/// Gets the type of page associated with this navigation entry.
	/// </summary>
	public Type SourcePageType
	{
		get => (Type)GetValue(SourcePageTypeProperty);
		set => SetValue(SourcePageTypeProperty, value);
	}

	/// <summary>
	/// Identifies the SourcePageType dependency property.
	/// </summary>
	public static DependencyProperty SourcePageTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(SourcePageType),
			typeof(Type),
			typeof(PageStackEntry),
			new FrameworkPropertyMetadata(null));
}
