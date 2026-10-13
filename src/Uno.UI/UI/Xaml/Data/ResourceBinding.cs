#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Data
{
	/// <summary>
	/// Binding used for resource resolution at load time.
	/// </summary>
	/// <remarks>
	/// A plain object rather than a <see cref="BindingBase"/>: every {ThemeResource}/{StaticResource} assignment
	/// allocates one, and a <see cref="DependencyObject"/> would cost a full property store each.
	/// </remarks>
	internal sealed class ResourceBinding
	{
		/// <summary>
		/// The resource key.
		/// </summary>
		public SpecializedResourceDictionary.ResourceKey ResourceKey { get; }

		public ResourceUpdateReason UpdateReason { get; }

		public bool IsPersistent => UpdateReason != ResourceUpdateReason.StaticResourceLoading;

		public object? ParseContext { get; }

		public DependencyPropertyValuePrecedences Precedence { get; }

		/// <summary>
		/// The binding path that this resource binding is targeting, if it was created by a VisualState <see cref="Setter"/>.
		/// </summary>
		public BindingPath? SetterBindingPath { get; }

		public ResourceBinding(SpecializedResourceDictionary.ResourceKey resourceKey, ResourceUpdateReason updateReason, object? parseContext, DependencyPropertyValuePrecedences precedence, BindingPath? setterBindingPath)
		{
			ResourceKey = resourceKey;
			UpdateReason = updateReason;
			ParseContext = parseContext;
			Precedence = precedence;
			SetterBindingPath = setterBindingPath;
		}
	}
}
