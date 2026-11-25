#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using static System.Reflection.BindingFlags;

namespace Uno.UI.Extensions;

public static partial class VisualTreeExtensions
{
	/// <summary>
	/// Gets the value of the dependency property named <paramref name="property"/> on <paramref name="owner"/>,
	/// found by reflection through its static <c>{property}Property</c> member.
	/// </summary>
	/// <typeparam name="T">The expected type of the value.</typeparam>
	/// <param name="owner">The object to read the property from.</param>
	/// <param name="property">The name of the property, without the <c>Property</c> suffix.</param>
	/// <param name="value">The value, when found.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="owner"/> is a <see cref="DependencyObject"/> with such a property and its value is a non-null <typeparamref name="T"/>;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <remarks>Meant for diagnostics: in a trimmed app, the <c>{property}Property</c> member may have been removed, in which case this returns <see langword="false"/>.</remarks>
	public static bool TryGetDpValue<T>(object owner, string property, [NotNullWhen(true)] out T? value)
	{
		if (owner is DependencyObject @do &&
				GetProperty(owner.GetType(), $"{property}Property")
				?.GetValue(null, null) is DependencyProperty dp &&
				@do.GetValue(dp) is T t)
		{
			value = t;
			return true;
		}

		value = default;
		return false;

		[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the property exists.")]
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the property exists.")]
		static PropertyInfo? GetProperty(Type type, string propertyName)
			=> type.GetProperty(propertyName, Public | Static | FlattenHierarchy);
	}

	/// <summary>
	/// Returns the first ancestor of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	public static T? FindFirstAncestor<T>(this DependencyObject? reference) => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first ancestor of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	/// <exception cref="InvalidOperationException">If no such ancestor exists.</exception>
	public static T FindFirstAncestorOrThrow<T>(this DependencyObject? reference) where T : class => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}");

	/// <summary>
	/// Returns the first ancestor of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	public static T? FindFirstAncestor<T>(this DependencyObject? reference, Func<T, bool> predicate) => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, Func<T, bool> predicate) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="InvalidOperationException">If no such descendant exists.</exception>
	public static T FindFirstDescendantOrThrow<T>(this DependencyObject? reference) where T : class => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}");

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="InvalidOperationException">If no such descendant exists.</exception>
	public static T FindFirstDescendantOrThrow<T>(this DependencyObject? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name) ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}#{name}");

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/> whose ancestors (up to <paramref name="reference"/>) and itself satisfy the <paramref name="hierarchyPredicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each ancestor for a condition.</param>
	/// <param name="predicate">A function to test each descendant for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, Func<DependencyObject, bool> hierarchyPredicate, Func<T, bool> predicate) => EnumerateDescendants(reference, hierarchyPredicate)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns all the visual descendants of a node, in depth-first order.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <seealso cref="EnumerateRawDescendants(UIElement, int?)"/>
	public static IEnumerable<DependencyObject> EnumerateDescendants(this DependencyObject? reference) => EnumerateDescendants(reference, x => true);

	/// <summary>
	/// Returns the visual descendants of a node that satisfy the <paramref name="hierarchyPredicate"/>, in depth-first order.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each descendant; one that fails it is skipped together with its own descendants.</param>
	/// <seealso cref="EnumerateRawDescendants(UIElement, int?)"/>
	public static IEnumerable<DependencyObject> EnumerateDescendants(this DependencyObject? reference, Func<DependencyObject, bool> hierarchyPredicate)
	{
		foreach (var child in reference.EnumerateChildren().Where(hierarchyPredicate))
		{
			yield return child;
			foreach (var grandchild in child.EnumerateDescendants(hierarchyPredicate))
			{
				yield return grandchild;
			}
		}
	}

	/// <summary>
	/// Returns the descendants of an element in depth-first order.
	/// </summary>
	/// <param name="view">The element to start from; it is not returned.</param>
	/// <param name="maxDepth">How deep to go: 0 returns the direct children only, n returns n + 1 levels; <see langword="null"/> for no limit.</param>
	/// <remarks>
	/// Unlike <see cref="EnumerateDescendants(DependencyObject)"/>, this walks the element's raw child list,
	/// so it also returns the placeholders that <c>x:Load</c> and <c>x:DeferLoadStrategy</c> leave for elements not loaded yet (<c>ElementStub</c>).
	/// This only makes a difference on Uno Platform targets: on WinAppSDK both methods walk <see cref="VisualTreeHelper"/> and, without <paramref name="maxDepth"/>, return the same elements in the same order.
	/// </remarks>
	public static IEnumerable<UIElement> EnumerateRawDescendants(this UIElement view, int? maxDepth = null)
	{
#if HAS_UNO
		return VisualTreeHelper.EnumerateRawDescendants(view, maxDepth);
#else
		return Walk(view, maxDepth);

		static IEnumerable<UIElement> Walk(UIElement view, int? maxDepth)
		{
			foreach (var child in view.EnumerateChildren().OfType<UIElement>())
			{
				yield return child;

				if (maxDepth is null or > 0)
				{
					foreach (var descendant in Walk(child, maxDepth - 1))
					{
						yield return descendant;
					}
				}
			}
		}
#endif
	}

	/// <summary>
	/// Returns the ancestors of a node, from its parent up to the root, following <see cref="VisualTreeHelper.GetParent"/>.
	/// </summary>
	/// <param name="reference">Any node of the visual tree; it is not returned.</param>
	public static IEnumerable<DependencyObject> EnumerateVisualTreeAncestors(this DependencyObject? reference)
	{
		if (reference is null) yield break;
		while (VisualTreeHelper.GetParent(reference) is { } parent)
		{
			yield return reference = parent;
		}
	}

	internal static IEnumerable<DependencyObject> EnumerateChildren(this DependencyObject? reference)
	{
		if (reference is null)
		{
			yield break;
		}

		var count = VisualTreeHelper.GetChildrenCount(reference);
		for (var i = 0; i < count; i++)
		{
			yield return VisualTreeHelper.GetChild(reference, i);
		}
	}
}
