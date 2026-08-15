#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml
{
	public sealed partial class TargetPropertyPath
	{

		public object? Target
		{
			get;
			set;
		}

		public PropertyPath? Path
		{
			get;
			set;
		}

		public TargetPropertyPath(DependencyProperty targetProperty) { }

		public TargetPropertyPath() { }

		public TargetPropertyPath(object target, PropertyPath path)
		{
			Target = target;
			Path = path;
		}

		internal string? TargetName { get; }

		/// <summary>
		/// Constructor used by the XamlReader, for target late-binding
		/// </summary>
		internal TargetPropertyPath(string targetName, PropertyPath path)
		{
			TargetName = targetName;
			Path = path;
		}
	}
}
