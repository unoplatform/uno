#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard : IAdditionalChildrenProvider
{
	/// <summary>
	/// Gets the collection of child Timeline objects.
	/// </summary>
	public TimelineCollection Children { get; }

	/// <summary>
	/// Gets the value of the Storyboard.TargetName XAML attached property from a target element.
	/// </summary>
	public static string GetTargetName(Timeline element) => (string)element.GetValue(TargetNameProperty);

	/// <summary>
	/// Sets the value of the Storyboard.TargetName XAML attached property for a target element.
	/// </summary>
	public static void SetTargetName(Timeline element, string name) => element.SetValue(TargetNameProperty, name);

	/// <summary>
	/// Identifies the Storyboard.TargetName XAML attached property.
	/// </summary>
	public static DependencyProperty TargetNameProperty
	{
		[DynamicDependency(nameof(GetTargetName))]
		[DynamicDependency(nameof(SetTargetName))]
		get;
	} = DependencyProperty.RegisterAttached(
			"TargetName",
			typeof(string),
			typeof(Storyboard),
			new FrameworkPropertyMetadata(string.Empty, OnTargetingPropertyChanged));

	/// <summary>
	/// Gets the value of the Storyboard.TargetProperty XAML attached property from a target element.
	/// </summary>
	public static string GetTargetProperty(Timeline element) => (string)element.GetValue(TargetPropertyProperty);

	/// <summary>
	/// Sets the value of the Storyboard.TargetProperty XAML attached property for a target element.
	/// </summary>
	public static void SetTargetProperty(Timeline element, string path) => element.SetValue(TargetPropertyProperty, path);

	/// <summary>
	/// Identifies the Storyboard.TargetProperty XAML attached property.
	/// </summary>
	public static DependencyProperty TargetPropertyProperty
	{
		[DynamicDependency(nameof(GetTargetProperty))]
		[DynamicDependency(nameof(SetTargetProperty))]
		get;
	} = DependencyProperty.RegisterAttached(
			"TargetProperty",
			typeof(string),
			typeof(Storyboard),
			new FrameworkPropertyMetadata(string.Empty, OnTargetingPropertyChanged));

	// CTimeline::SetValue: these properties can't be modified unless the root Storyboard is stopped.
	private static void OnTargetingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is Timeline timeline)
		{
			timeline.CheckCanBeModified();
		}
	}

	/// <summary>
	/// Sets the target of the timeline through an ElementNameSubject, for a target element materialized lazily.
	/// </summary>
	public static void SetTarget(Timeline timeline, ElementNameSubject target) => timeline.SetElementNameTarget(target);

	IEnumerable<DependencyObject> IAdditionalChildrenProvider.GetAdditionalChildObjects() => Children;
}
