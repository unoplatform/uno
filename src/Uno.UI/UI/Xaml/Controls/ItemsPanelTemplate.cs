#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;

using View = Microsoft.UI.Xaml.UIElement;
// The template factory is exposed as a plain Func so no Uno-specific delegate type leaks into the public API.
using Builder = System.Func<object?, Microsoft.UI.Xaml.TemplateMaterializationSettings, Microsoft.UI.Xaml.UIElement?>;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsPanelTemplate : FrameworkTemplate
	{
		public ItemsPanelTemplate() : this(null, (Builder?)null) { }

		/// <summary>
		/// Build an ItemsPanelTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ItemsPanelTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public ItemsPanelTemplate(object? owner, Builder? factory)
			: base(owner, factory)
		{
		}

	}
}

