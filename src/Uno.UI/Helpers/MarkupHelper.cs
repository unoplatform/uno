#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Collections;
using System.ComponentModel;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Markup;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// A set of Uno specific markup helpers
	/// </summary>
	public static class MarkupHelper
	{
		private static WeakAttachedDictionary<object, string>? _weakProperties;

		private static WeakAttachedDictionary<object, string> WeakProperties
			=> _weakProperties ??= new();

		/// <summary>
		/// Sets the x:Uid member on a element implementing <see cref="IXUidProvider"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="uid">The new uid to set</param>
		public static void SetXUid(object target, string uid)
		{
			if (target is IXUidProvider provider)
			{
				provider.Uid = uid;
			}
		}

		/// <summary>
		/// Gets the Uid defined via <see cref="SetXUid(object, string)"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <returns>A the x:Uid value</returns>
		public static string GetXUid(object target)
			=> target is IXUidProvider provider ? provider.Uid : "";

		/// <summary>
		/// Gets a resource string for an x:Uid bound property.
		/// </summary>
		/// <remarks>
		/// Returns null when the resource is an empty string.
		/// </remarks>
		public static string? GetResourceStringForXUid(string viewName, string resourceName)
		{
			var loader = viewName is not null
				? ResourceLoader.GetForCurrentView(viewName)
				: ResourceLoader.GetForCurrentView();

			return loader.GetString(resourceName) is { Length: > 0 } value
						? value
						: null;
		}

		/// <summary>
		/// Sets a builder for markup-lazy properties in <see cref="VisualState"/>
		/// </summary>
		public static void SetVisualStateLazy(VisualState target, Action builder)
			=> target.LazyBuilder = builder;

		/// <summary>
		/// Sets a builder for markup-lazy properties in <see cref="VisualTransition"/>
		/// </summary>
		public static void SetVisualTransitionLazy(VisualTransition target, Action builder)
			=> target.LazyBuilder = builder;

		public static IXamlServiceProvider CreateParserContext(object? target, Type propertyDeclaringType, string propertyName, [DynamicallyAccessedMembers(ProvideValueTargetProperty.TypeRequirements)] Type propertyType)
			=> CreateParserContext(target, propertyDeclaringType, propertyName, propertyType, null);

		public static IXamlServiceProvider CreateParserContext(object? target, Type propertyDeclaringType, string propertyName, [DynamicallyAccessedMembers(ProvideValueTargetProperty.TypeRequirements)] Type propertyType, object? rootObject)
			=> new XamlServiceProviderContext
			{
				TargetObject = target,
				TargetProperty = new ProvideValueTargetProperty
				{
					DeclaringType = propertyDeclaringType,
					Name = propertyName,
					Type = propertyType,
				},
				RootObject = rootObject,
			};

		/// <summary>
		/// Name of the weak property carrying the XAML declaration site of an object that is not a
		/// <see cref="FrameworkElement"/> — a Style, a template, a VisualState, a ResourceDictionary.
		/// A <see cref="FrameworkElement"/> carries the same information as its DebugParseContext
		/// (set through <c>FrameworkElementHelper.SetBaseUri</c>) instead.
		/// </summary>
		/// <remarks>
		/// The value is emitted by the XAML generator when Hot Reload code generation is enabled, in the
		/// form <c>file:///&lt;path&gt;#L&lt;line&gt;:&lt;position&gt;</c> — an absolute build-machine path
		/// with '\' replaced by '/', then a fragment naming the 1-based line and position of the element.
		/// An already-rooted path yields four slashes (<c>file:////home/…</c>), as the scheme's three are
		/// followed by the path's own leading separator.
		/// <para>
		/// Read the fragment from the END of the value: a path is free to contain '#' — including the
		/// sequence "#L" (a "C#Lib" folder, say) — so a parser that splits on the first occurrence
		/// mis-reads such a path, and one that splits on every occurrence fails outright.
		/// </para>
		/// <para>
		/// The value is not URI-escaped beyond the separator replacement: spaces and non-ASCII
		/// characters appear verbatim, so it must not be handed to a URI parser as-is. Stripping the
		/// <c>file:///</c> prefix and the trailing fragment yields the key of the embedded XAML sources
		/// provider (compared with <see cref="StringComparison.OrdinalIgnoreCase"/>).
		/// </para>
		/// <para>
		/// Not a <c>const</c> deliberately: a consumer compiled against one Uno.UI would otherwise bake
		/// the literal in, and keep it if the name ever changed — the drift this single declaration
		/// exists to prevent. The name itself lives in <c>XamlFilePathHelper</c>, which is linked into
		/// the generator that emits it.
		/// </para>
		/// </remarks>
		internal static readonly string OriginalSourceLocationPropertyName = Uno.UI.Xaml.XamlFilePathHelper.OriginalSourceLocationPropertyName;

		/// <summary>
		/// Attaches a property to an object, using a weak reference.
		/// </summary>
		/// <remarks>This helper is mainly used for XAML Hot Reload</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetElementProperty<TInstance>(object target, string propertyName, TInstance value)
			=> WeakProperties.SetValue(target, propertyName, value);

		/// <summary>
		/// Gets a property to an object, using a weak reference.
		/// </summary>
		/// <remarks>This helper is mainly used for XAML Hot Reload</remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static TInstance? GetElementProperty<TInstance>(object target, string propertyName)
			=> WeakProperties.GetValue<TInstance>(target, propertyName);

		/// <summary>
		/// Applies the materialization settings to a member created from a <see cref="FrameworkTemplate"/>.
		/// </summary>
		/// <remarks>
		/// Null-safe entry point for generated XAML; the behavior itself lives on the settings. Called once
		/// per template member, so it is also the place to add materialization diagnostics.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void OnTemplateMemberCreated(DependencyObject target, TemplateMaterializationSettings? settings)
			=> settings?.OnMemberCreated(target);

		/// <summary>
		/// Creates a <see cref="DataTemplate"/> from a factory.
		/// </summary>
		/// <remarks>
		/// The builder constructors are internal (WinUI exposes none), so generated XAML -- which compiles into
		/// the consuming app's assembly -- reaches them through these helpers.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static DataTemplate CreateDataTemplate(object? owner, FrameworkTemplateBuilder? factory) => new(owner, factory);

		/// <summary>
		/// Creates a <see cref="ControlTemplate"/> from a factory. See <see cref="CreateDataTemplate"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static ControlTemplate CreateControlTemplate(object? owner, FrameworkTemplateBuilder? factory) => new(owner, factory);

		/// <summary>
		/// Creates an <see cref="ItemsPanelTemplate"/> from a factory. See <see cref="CreateDataTemplate"/>.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static ItemsPanelTemplate CreateItemsPanelTemplate(object? owner, FrameworkTemplateBuilder? factory) => new(owner, factory);

		/// <summary>
		/// Helper for XAML code generation. Not intended to be used in apps outside of XAML generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static ResourceDictionary AddMergedDictionaries(this ResourceDictionary dictionary, params ResourceDictionary[] mergedDictionaries)
		{
			foreach (var mergedDictionary in mergedDictionaries)
			{
				dictionary.MergedDictionaries.Add(mergedDictionary);
			}

			return dictionary;
		}
	}
}
