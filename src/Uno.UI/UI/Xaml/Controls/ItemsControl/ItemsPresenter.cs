#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter : FrameworkElement, IScrollSnapPointsInfo
{
	// WinUI creates the header and footer in Initialize and inserts them in MeasureOverride.
	// Uno attaches them when the ItemsControl adopts this presenter (see CreateHeaderAndFooter).
	internal ContentControl? FooterContentControl { get; private set; }

	internal ContentControl? HeaderContentControl { get; private set; }

	private UIElement? _itemsPanel;

	internal UIElement? Panel => _itemsPanel;

	public object? Header
	{
		get => GetHeaderValue();
		set => SetHeaderValue(value);
	}

	private static object? GetHeaderDefaultValue() => null;

	[GeneratedDependencyProperty]
	public static DependencyProperty HeaderProperty { get; } = CreateHeaderProperty();

	public object? Footer
	{
		get => GetFooterValue();
		set => SetFooterValue(value);
	}

	private static object? GetFooterDefaultValue() => null;

	[GeneratedDependencyProperty]
	public static DependencyProperty FooterProperty { get; } = CreateFooterProperty();

	private static DataTemplate? GetHeaderTemplateDefaultValue() => null;

	public DataTemplate? HeaderTemplate
	{
		get => GetHeaderTemplateValue();
		set => SetHeaderTemplateValue(value);
	}

	[GeneratedDependencyProperty]
	public static DependencyProperty HeaderTemplateProperty { get; } = CreateHeaderTemplateProperty();

	public DataTemplate? FooterTemplate
	{
		get => GetFooterTemplateValue();
		set => SetFooterTemplateValue(value);
	}

	private static DataTemplate? GetFooterTemplateDefaultValue() => null;

	[GeneratedDependencyProperty]
	public static DependencyProperty FooterTemplateProperty { get; } = CreateFooterTemplateProperty();

	public TransitionCollection HeaderTransitions
	{
		get => GetHeaderTransitionsValue();
		set => SetHeaderTransitionsValue(value);
	}

	private static TransitionCollection GetHeaderTransitionsDefaultValue() => new TransitionCollection();

	[GeneratedDependencyProperty]
	public static DependencyProperty HeaderTransitionsProperty { get; } = CreateHeaderTransitionsProperty();

	public TransitionCollection FooterTransitions
	{
		get => GetFooterTransitionsValue();
		set => SetFooterTransitionsValue(value);
	}

	private static TransitionCollection GetFooterTransitionsDefaultValue() => new TransitionCollection();

	[GeneratedDependencyProperty]
	public static DependencyProperty FooterTransitionsProperty { get; } = CreateFooterTransitionsProperty();

	public Thickness Padding
	{
		get => (Thickness)GetValue(PaddingProperty);
		set => SetValue(PaddingProperty, value);
	}

	public static DependencyProperty PaddingProperty { get; } =
		DependencyProperty.Register(
			nameof(Padding),
			typeof(Thickness),
			typeof(ItemsPresenter),
			new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsMeasure));

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		if (IsLoaded)
		{
			var itemsControl =
				this.FindFirstParent<ItemsControl>() ??
				// The items-control may not exist in the direct visual-tree,
				// if it is defined within a popup/flyout, as in the case of ComboBox.
				this.FindFirstParent<PopupPanel>()?.Popup?.FindFirstParent<ItemsControl>();

			itemsControl?.SetItemsPresenter(this);
		}
	}

	internal void SetItemsPanel(UIElement? panel)
	{
		if (_itemsPanel == panel)
		{
			return;
		}

		// We should only reach here, after CreateHeaderAndFooter() was called. Let's sanity-check that.
		global::System.Diagnostics.Debug.Assert(HeaderContentControl is { });

		if (_itemsPanel is { } previousPanel)
		{
			VisualTreeHelper.RemoveView(this, previousPanel);
		}

		if ((_itemsPanel = panel) is { })
		{
			// There can be only two scenarios here, with header and footer created or not:
			// 1. if so, we should have only 2 children: Header + Footer. So, we insert the panel in-between(at index 1)
			// 2. if not, we should have zero child. So, we just add the panel.
			if (HeaderContentControl is { })
			{
				VisualTreeHelper.AddView(this, _itemsPanel, 1);
			}
			else
			{
				VisualTreeHelper.AddView(this, _itemsPanel);
			}
		}

		NotifySnapPointsInfoPanelChanged();

		InvalidateMeasure();
	}

	internal void CreateHeaderAndFooter()
	{
		if (HeaderContentControl is null)
		{
			HeaderContentControl = new ContentControl
			{
				Content = Header,
				ContentTemplate = HeaderTemplate,
				ContentTransitions = HeaderTransitions,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				IsTabStop = false
			};
			VisualTreeHelper.AddChild(this, HeaderContentControl);
		}

		if (FooterContentControl is null)
		{
			// The footer content is only loaded once it nears the viewport (see LoadFooter).
			FooterContentControl = new ContentControl
			{
				Content = m_bCanLoadFooter ? Footer : null,
				ContentTemplate = m_bCanLoadFooter ? FooterTemplate : null,
				ContentTransitions = FooterTransitions,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				IsTabStop = false
			};

			// make sure that footer has lower zIndex to property display drop animation.
			Canvas.SetZIndex(FooterContentControl, -1);

			VisualTreeHelper.AddChild(this, FooterContentControl);
		}
	}

	internal override bool CanHaveChildren() => true;
}
