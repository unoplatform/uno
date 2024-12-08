#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Binding used for resource resolution at load time. 
	/// </summary>
	internal class ResourceBinding : BindingBase
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
