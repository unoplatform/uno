using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Navigation;

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
	[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
	public Type SourcePageType
	{
		[UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Relying on the declaring property to track this.")]
		get => (Type)GetValue(SourcePageTypeProperty);
		set => SetValue(SourcePageTypeProperty, value);
	}

	/// <summary>
	/// Identifies the SourcePageType dependency property.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "@jonpryor has no idea what the trimmer is talking about.")]
	public static DependencyProperty SourcePageTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(SourcePageType),
			typeof(Type),
			typeof(PageStackEntry),
			new FrameworkPropertyMetadata(null));
}
