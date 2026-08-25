#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;

using View = Microsoft.UI.Xaml.UIElement;
using FrameworkTemplateBuilder = Uno.UI.FrameworkTemplateBuilder;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsPanelTemplate : FrameworkTemplate
	{
		public ItemsPanelTemplate() : this(null, (FrameworkTemplateBuilder?)null) { }

		/// <summary>
		/// Build an ItemsPanelTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ItemsPanelTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		internal ItemsPanelTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

	}
}

