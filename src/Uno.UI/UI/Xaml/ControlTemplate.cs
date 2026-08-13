#nullable enable

using System;
using Microsoft.UI.Xaml.Media;

using View = Microsoft.UI.Xaml.UIElement;
// The template factory is exposed as a plain Func so no Uno-specific delegate type leaks into the public API.
using Builder = System.Func<object?, Microsoft.UI.Xaml.TemplateMaterializationSettings, Microsoft.UI.Xaml.UIElement?>;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ControlTemplate : FrameworkTemplate
	{
		public ControlTemplate() : this(null, (Builder?)null) { }

		/// <summary>
		/// Build a ControlTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ControlTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public ControlTemplate(object? owner, Builder? factory)
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
