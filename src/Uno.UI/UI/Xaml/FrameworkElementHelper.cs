using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI
{
	/// <summary>
	/// A set of uno.ui-specific helpers for Xaml
	/// </summary>
	public static class FrameworkElementHelper
	{
		/// <summary>
		/// Conditional table used in the context of Control and DataTemplates
		/// to link the lifetime generated XAML classes that hold x:Name backing
		/// fields to the top level UIElement of the template. This allows for
		/// ElementNameSubject and TargetProperty path to only keep weak references
		/// to their targets.
		/// </summary>
		private static readonly ConditionalWeakTable<DependencyObject, object> _contextAssociation = new ConditionalWeakTable<DependencyObject, object>();

		/// <summary>
		/// Set the rendering phase, defined via x:Phase.
		/// </summary>
		/// <param name="target">The target <see cref="FrameworkElement"/></param>
		/// <param name="phase">The render phase ID</param>
		public static void SetRenderPhase(FrameworkElement target, int phase)
			=> target.RenderPhase = phase;

		/// <summary>
		/// Sets the x:Phases defined by all the children controls. The control must be the root element of a DataTemplate.
		/// </summary>
		/// <param name="target">The target <see cref="FrameworkElement"/></param>
		/// <param name="declaredPhases">A set of phases used by the children controls.</param>
		public static void SetDataTemplateRenderPhases(FrameworkElement target, int[] declaredPhases)
			=> target.DataTemplateRenderPhases = declaredPhases;

		/// <summary>
		/// When true (normally because the IsUiAutomationMappingEnabled build property is set), setting the <see cref="Name"/> property
		/// programmatically will also set the appropriate test identifier for Android/iOS. Disabled by default because it may interfere
		/// with accessibility features in non-testing scenarios.
		/// On WebAssembly, settings this property also enables the ability for <see cref="Microsoft.UI.Xaml.Automation.AutomationProperties.AutomationIdProperty"/>
		/// to be applied to the visual tree elements.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsUiAutomationMappingEnabled { get; set; } = false;

		/// <summary>
		/// Sets the BaseUri property on FrameworkElement. This is a XAML generator API, do not use directly.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetBaseUri(FrameworkElement target, string uri)
			=> SetBaseUri(target, uri, null, -1, -1);

		/// <summary>
		/// Sets the BaseUri property and debugging details on FrameworkElement. This is a XAML generator API, do not use directly.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void SetBaseUri(FrameworkElement target, string uri, string localFileUri, int lineNumber, int linePosition)
		{
			if (target is { } fe)
			{
				fe.SetBaseUri(uri, localFileUri, lineNumber, linePosition);
			}
		}

		/// <summary>
		/// Associates an arbitrary object to the life time of the <paramref name="target"/> instance.
		/// </summary>
		/// <remarks>
		/// This method is used by the XAML generator to keep x:Name generated
		/// code alive alonside the top-level control of thete.
		/// </remarks>
		public static void AddObjectReference(DependencyObject target, object context)
			=> _contextAssociation.Add(target, context);

		/// <summary>
		/// Removes entries whose key (DependencyObject) is a type from a non-default ALC.
		/// Called during ALC teardown.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup")]
		internal static void ClearNonDefaultAlcEntries()
		{
			var defaultAlc = System.Runtime.Loader.AssemblyLoadContext.Default;
			var keysToRemove = new System.Collections.Generic.List<DependencyObject>();
			foreach (var kvp in _contextAssociation)
			{
				if (HasNonDefaultAlcReference(kvp.Key, defaultAlc) || (kvp.Value is not null && HasNonDefaultAlcReference(kvp.Value, defaultAlc)))
				{
					keysToRemove.Add(kvp.Key);
				}
			}

			foreach (var key in keysToRemove)
			{
				_contextAssociation.Remove(key);
			}

			if (keysToRemove.Count > 0 && typeof(FrameworkElementHelper).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FrameworkElementHelper).Log().Debug($"[ALC-CLEANUP] FrameworkElementHelper: removed {keysToRemove.Count} ALC context entries");
			}
		}

		/// <summary>
		/// Checks if an object or any of its instance fields (depth 2) references a type
		/// from a non-default ALC.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup")]
		private static bool HasNonDefaultAlcReference(object obj, System.Runtime.Loader.AssemblyLoadContext defaultAlc)
		{
			if (obj is null)
			{
				return false;
			}

			// Check the object's own type
			var objAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(obj.GetType().Assembly);
			if (objAlc is not null && objAlc != defaultAlc)
			{
				return true;
			}

			// Check instance fields (depth 1) for ALC-type values
			try
			{
				foreach (var field in obj.GetType().GetFields(
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.Public))
				{
					try
					{
						var fieldVal = field.GetValue(obj);
						if (fieldVal is null)
						{
							continue;
						}

						var fieldAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(fieldVal.GetType().Assembly);
						if (fieldAlc is not null && fieldAlc != defaultAlc)
						{
							return true;
						}
					}
					catch { }
				}
			}
			catch { }

			return false;
		}

		/// <summary>
		/// This is the equivalent of <see cref="FeatureConfiguration.UIElement.UseInvalidateMeasurePath"/>
		/// but just for a specific element (and its descendants) in the visual tree.
		/// </summary>
		/// <remarks>
		/// This will have no effect if <see cref="FeatureConfiguration.UIElement.UseInvalidateMeasurePath"/>
		/// is set to false.
		/// </remarks>
		public static void SetUseMeasurePathDisabled(UIElement element, bool state = true, bool eager = true, bool invalidate = true)
		{
			element.IsMeasureDirtyPathDisabled = state;

			if (eager)
			{
				using var children = element.GetChildren().GetEnumerator();
				while (children.MoveNext())
				{
					if (children.Current is FrameworkElement child)
					{
						SetUseMeasurePathDisabled(child, state, eager: true, invalidate);
					}
				}
			}

			if (invalidate)
			{
				element.InvalidateMeasure();
			}
		}

		public static bool GetUseMeasurePathDisabled(FrameworkElement element)
			=> element.IsMeasureDirtyPathDisabled;

		/// <summary>
		/// This is the equivalent of <see cref="FeatureConfiguration.UIElement.UseInvalidateArrangePath"/>
		/// but just for a specific element (and its descendants) in the visual tree.
		/// </summary>
		/// <remarks>
		/// This will have no effect if <see cref="FeatureConfiguration.UIElement.UseInvalidateArrangePath"/>
		/// is set to false.
		/// </remarks>
		public static void SetUseArrangePathDisabled(UIElement element, bool state = true, bool eager = true, bool invalidate = true)
		{
			element.IsArrangeDirtyPathDisabled = state;

			if (eager)
			{
				using var children = element.GetChildren().GetEnumerator();
				while (children.MoveNext())
				{
					if (children.Current is FrameworkElement child)
					{
						SetUseArrangePathDisabled(child, state, eager: true, invalidate);
					}
				}
			}

			if (invalidate)
			{
				element.InvalidateArrange();
			}
		}

		public static bool GetUseArrangePathDisabled(FrameworkElement element)
			=> element.IsArrangeDirtyPathDisabled;
	}
}
