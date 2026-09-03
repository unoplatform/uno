#nullable enable

using System;
using Microsoft.UI.Xaml.Media;

using View = Microsoft.UI.Xaml.UIElement;
using FrameworkTemplateBuilder = Uno.UI.FrameworkTemplateBuilder;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ControlTemplate : FrameworkTemplate
	{
		public ControlTemplate() : this(null, (FrameworkTemplateBuilder?)null) { }

		/// <summary>
		/// Build a ControlTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ControlTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		internal ControlTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public Type? TargetType { get; set; }

		internal View? LoadContentCached(Control templatedParent)
		{
			var root = base.LoadContentCachedCore(templatedParent);

			return root;
		}
	}
}
