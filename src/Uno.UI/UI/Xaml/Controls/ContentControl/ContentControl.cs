#nullable enable

using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Content))]
	public partial class ContentControl : Control
	{
		private bool _canCreateTemplateWithoutParent;

		protected override bool CanCreateTemplateWithoutParent { get { return _canCreateTemplateWithoutParent; } }

#nullable disable // Public members should stay nullable-oblivious for now to stay consistent with WinUI
		public ContentControl()
		{
			DefaultStyleKey = typeof(ContentControl);

			InitializePartial();
		}

		partial void InitializePartial();

		#region Content DependencyProperty

		public object Content
		{
			get
			{
				if (this.IsDependencyPropertySet(ContentProperty))
				{
					return GetValue(ContentProperty);
				}
				else if (ContentTemplate != null)
				{
					return DataContext;
				}
				else
				{
					// Return null to be sure that the Content will be empty and prevent the type to be dispayed.
					return null;
				}
			}
			set { SetValue(ContentProperty, value); }
		}

		public static DependencyProperty ContentProperty { get; } =
			DependencyProperty.Register(
				nameof(Content),
				typeof(object),
				typeof(ContentControl),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					// Don't propagate DataContext to Content qua Content, only propagate it via the visual tree. Prevents spurious
					// propagation in case that default style and template is only applied once the control enters the visual tree
					// (ie if created in code by new SomeControl())
					// NOTE: There's a case we currently don't support: if the Content is a DependencyObject but *not* a FrameworkElement, then
					// the DataContext won't get propagated and any bindings won't get updated.
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((ContentControl)s)?.OnContentChanged(e.OldValue, e.NewValue)
				)
			);

		#endregion

		#region ContentTemplate DependencyProperty

		public DataTemplate ContentTemplate
		{
			get { return (DataTemplate)GetValue(ContentTemplateProperty); }
			set { SetValue(ContentTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ContentTemplate.  This enables animation, styling, binding, etc...
		public static DependencyProperty ContentTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(ContentTemplate),
				typeof(DataTemplate),
				typeof(ContentControl),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((ContentControl)s)?.OnContentTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate)
				)
			);
		#endregion

		#region ContentTemplateSelector DependencyProperty

		public DataTemplateSelector ContentTemplateSelector
		{
			get { return (DataTemplateSelector)GetValue(ContentTemplateSelectorProperty); }
			set { SetValue(ContentTemplateSelectorProperty, value); }
		}

		public static DependencyProperty ContentTemplateSelectorProperty { get; } =
			DependencyProperty.Register(
				"ContentTemplateSelector",
				typeof(DataTemplateSelector),
				typeof(ContentControl),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((ContentControl)s)?.OnContentTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector)
				)
			);
		#endregion

		protected virtual void OnContentChanged(object oldContent, object newContent)
		{
		}

		protected virtual void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
		{
			if (CanCreateTemplateWithoutParent)
			{
				ApplyTemplate();
			}
		}

		protected virtual void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
		{
		}

		/// <summary>
		/// The root of the materialized <see cref="ContentTemplate"/>, reported by the
		/// <see cref="ContentPresenter"/> of the applied <see cref="Control.Template"/>.
		/// </summary>
		public UIElement ContentTemplateRoot { get; internal set; }

		#region Transitions Dependency Property

		public TransitionCollection ContentTransitions
		{
			get { return (TransitionCollection)this.GetValue(ContentTransitionsProperty); }
			set { this.SetValue(ContentTransitionsProperty, value); }
		}

		// The transitions are applied by the ContentPresenter of the template, which template-binds this property.
		public static DependencyProperty ContentTransitionsProperty { get; } =
			DependencyProperty.Register("ContentTransitions", typeof(TransitionCollection), typeof(ContentControl), new FrameworkPropertyMetadata(null));

		#endregion
#nullable enable

		/// <summary>
		/// Creates a ContentControl which can be measured without being added to the visual tree (eg as container in virtualized lists).
		/// </summary>
		internal static ContentControl CreateItemContainer()
		{
			return new ContentControl
			{
				_canCreateTemplateWithoutParent = true,
				IsGeneratedContainer = true
			};
		}

#nullable disable // Public members should stay nullable-oblivious for now to stay consistent with WinUI

		public override string GetAccessibilityInnerText()
		{
			switch (Content)
			{
				case string str:
					return str;
				case IFrameworkElement frameworkElement:
					return frameworkElement.GetAccessibilityInnerText();
				case object content:
					return content.ToString();
				default:
					return null;
			}
		}
#nullable enable
	}
}
