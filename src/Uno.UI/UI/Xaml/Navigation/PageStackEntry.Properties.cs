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

[assembly: UnconditionalSuppressMessage("Trimming", "IL2111", Scope = "member",
	Target = "M:Microsoft.UI.Xaml.Navigation.PageStackEntry.#cctor()",
	Justification = "From the `SourcePageTypeProperty` assignment; `typeof(Type)` triggers IL2111 regarding `Type.TypeInitializer`, but Uno doesn't use `Type.TypeInitializer`!")]

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
	[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "`typeof(Type)` triggers IL2111 regarding `Type.TypeInitializer`, but Uno doesn't use `Type.TypeInitializer`!")]
	// warning IL2111: Method 'System.Type.TypeInitializer.get' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
	public static DependencyProperty SourcePageTypeProperty { get; } =
		DependencyProperty.Register(
			nameof(SourcePageType),
			typeof(Type),
			typeof(PageStackEntry),
			new FrameworkPropertyMetadata(null));
}
