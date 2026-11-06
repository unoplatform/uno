#nullable enable

using System;
using System.ComponentModel;

using View = Microsoft.UI.Xaml.UIElement;
using FrameworkTemplateBuilder = Uno.UI.FrameworkTemplateBuilder;

namespace Microsoft.UI.Xaml
{
	public partial class DataTemplate : FrameworkTemplate
	{
		public DataTemplate() : base(null, (FrameworkTemplateBuilder?)null) { }

		/// <summary>
		/// Build a DataTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the DataTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		internal DataTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public DependencyObject? LoadContent()
		{
			if (_queriedContent is { } queriedContent)
			{
				_queriedContent = null;
				return queriedContent;
			}

			return ((IFrameworkTemplateInternal)this).LoadContent(templatedParent: null);
		}

		private DependencyObject? _queriedContent;

		// CDataTemplate::QueryContentNoRef in WinUI: expands once for inspection and the next LoadContent reuses that instance.
		internal DependencyObject? QueryContentNoRef()
			=> _queriedContent ??= ((IFrameworkTemplateInternal)this).LoadContent(templatedParent: null);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public DependencyObject? LoadContent(DependencyObject templatedParent) => ((IFrameworkTemplateInternal)this).LoadContent(templatedParent);

		internal View? LoadContentCached(DependencyObject? templatedParent = null) => base.LoadContentCachedCore(templatedParent);
	}
}

