#nullable enable
#if WINUI || HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Uno.Diagnostics.UI;

/// <summary>
/// An overlay layer used to inject analytics and diagnostics indicators into the UI.
/// </summary>
[TemplatePart(Name = ToolbarPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ElementsPanelPartName, Type = typeof(Panel))]
[TemplatePart(Name = AnchorPartName, Type = typeof(UIElement))]
[TemplatePart(Name = NotificationPartName, Type = typeof(ContentPresenter))]
[TemplateVisualState(GroupName = "DisplayMode", Name = DisplayModeCompactStateName)]
[TemplateVisualState(GroupName = "DisplayMode", Name = DisplayModeExpandedStateName)]
[TemplateVisualState(GroupName = "Notification", Name = NotificationCollapsedStateName)]
[TemplateVisualState(GroupName = "Notification", Name = NotificationVisibleStateName)]
[TemplateVisualState(GroupName = "HorizontalDirection", Name = HorizontalDirectionLeftVisualState)]
[TemplateVisualState(GroupName = "HorizontalDirection", Name = HorizontalDirectionRightVisualState)]
public sealed partial class DiagnosticsOverlay : Control
{
	private const string ToolbarPartName = "PART_Toolbar";
	private const string ElementsPanelPartName = "PART_Elements";
	private const string AnchorPartName = "PART_Anchor";
	private const string NotificationPartName = "PART_Notification";

	private const string DisplayModeCompactStateName = "Compact";
	private const string DisplayModeExpandedStateName = "Expanded";

	private const string NotificationCollapsedStateName = "Collapsed";
	private const string NotificationVisibleStateName = "Visible";

	private const string HorizontalDirectionLeftVisualState = "Left";
	private const string HorizontalDirectionRightVisualState = "Right";

	private static readonly ConditionalWeakTable<XamlRoot, DiagnosticsOverlay> _overlays = new();

	/// <summary>
	/// Gets the <see cref="DiagnosticsOverlay"/> for the specified <see cref="XamlRoot"/>.
	/// </summary>
	/// <param name="root">The root onto which the overlay is being rendered.</param>
	/// <returns></returns>
	public static DiagnosticsOverlay Get(XamlRoot root)
		=> _overlays.GetValue(root, static r => new DiagnosticsOverlay(r));

	private readonly XamlRoot _root;
	private readonly object _updateGate = new();
	private readonly List<IDiagnosticView> _localRegistrations = new();
	private readonly Dictionary<IDiagnosticView, DiagnosticElement> _elements = new();
	private readonly Dictionary<string, bool> _configuredElementVisibilities = new();

	private ViewContext? _context;
	private Popup? _overlayHost;
	private bool _isVisible;
	private bool _isExpanded = true;
	private int _updateEnqueued;
	private FrameworkElement? _toolbar;
	private Panel? _elementsPanel;
	private UIElement? _anchor;
	private ContentPresenter? _notificationPresenter;

	static DiagnosticsOverlay()
	{
		DiagnosticViewRegistry.Added += static (snd, e) =>
		{
			foreach (var overlay in _overlays)
			{
				overlay.Value.EnqueueUpdate();
			}
		};
	}

	private DiagnosticsOverlay(XamlRoot root)
	{
		_root = root;
		_context = ViewContext.TryCreate(this);

		root.Changed += static (snd, e) =>
		{
			var overlay = Get(snd);

			if (ViewContext.TryCreate(overlay) is { } context) // I.e. dispatcher changed ... is this even possible ???
			{
				lock (overlay._updateGate)
				{
					overlay._context = context;

					// Clean all dispatcher bound states
					overlay._overlayHost = null;
					overlay._elementsPanel = null;
					foreach (var element in overlay._elements.Values)
					{
						element.Dispose();
					}

					overlay._elements.Clear();
				}
			}

			overlay.UpdatePlacement();
			overlay.EnqueueUpdate();
		};

		Style = (Style)XamlReader.Load("""
			<Style
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:local="using:Uno.Diagnostics.UI"
					xmlns:diag="using:Uno.Diagnostics.UI"
					xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
					xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
					xmlns:mux="using:Microsoft.UI.Xaml.Controls"
					TargetType="local:DiagnosticsOverlay">
				<Setter Property="BorderThickness" Value="1" />
				<Setter Property="CornerRadius" Value="4" />
				<Setter Property="Height" Value="32" />
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="local:DiagnosticsOverlay">
							<Canvas Height="{TemplateBinding Height}">
								<Canvas.Resources>
									<ResourceDictionary>
										<x:String x:Key="AnchorIcon">M0 16.3626V1.36258C0 1.22716 0.0494792 1.10998 0.148438 1.01102C0.247396 0.912059 0.364583 0.862579 0.5 0.862579C0.635417 0.862579 0.752604 0.912059 0.851562 1.01102C0.950521 1.10998 1 1.22716 1 1.36258V16.3626C1 16.498 0.950521 16.6152 0.851562 16.7141C0.752604 16.8131 0.635417 16.8626 0.5 16.8626C0.364583 16.8626 0.247396 16.8131 0.148438 16.7141C0.0494792 16.6152 0 16.498 0 16.3626ZM3 16.3626V1.36258C3 1.22716 3.04948 1.10998 3.14844 1.01102C3.2474 0.912059 3.36458 0.862579 3.5 0.862579C3.63542 0.862579 3.7526 0.912059 3.85156 1.01102C3.95052 1.10998 4 1.22716 4 1.36258V16.3626C4 16.498 3.95052 16.6152 3.85156 16.7141C3.7526 16.8131 3.63542 16.8626 3.5 16.8626C3.36458 16.8626 3.2474 16.8131 3.14844 16.7141C3.04948 16.6152 3 16.498 3 16.3626Z</x:String>
										<Style TargetType="Button">
											<Setter Property="Background" Value="Transparent" />
											<Setter Property="BorderBrush" Value="Transparent" />
											<Setter Property="Padding" Value="0" />
											<Setter Property="BorderThickness" Value="0" />
										</Style>
										<ResourceDictionary.ThemeDictionaries>
											<ResourceDictionary x:Name="Light">
												<SolidColorBrush x:Key="DiagnosticsOverlayBackgroundBrush">#F9F9F9</SolidColorBrush>
												<SolidColorBrush x:Key="DiagnosticsOverlayBorderBrush">#EBEBEB</SolidColorBrush>
												<SolidColorBrush x:Key="DiagnosticsAnchorFillBrush" Opacity="0.6">#000000</SolidColorBrush>
											</ResourceDictionary>
											<ResourceDictionary x:Name="Dark">
												<SolidColorBrush x:Key="DiagnosticsOverlayBackgroundBrush">#282828</SolidColorBrush>
												<SolidColorBrush x:Key="DiagnosticsOverlayBorderBrush">#1C1C1C</SolidColorBrush>
												<SolidColorBrush x:Key="DiagnosticsAnchorFillBrush" Opacity="0.8">#FFFFFF</SolidColorBrush>
											</ResourceDictionary>
										</ResourceDictionary.ThemeDictionaries>
									</ResourceDictionary>
								</Canvas.Resources>
								<VisualStateManager.VisualStateGroups>
									<VisualStateGroup x:Name="DisplayMode">
										<VisualState x:Name="Compact">
											<VisualState.Setters>
												<Setter Target="PART_Elements.MaxWidth" Value="0" />
											</VisualState.Setters>
										</VisualState>
										<VisualState x:Name="Expanded">
											<VisualState.Setters>
												<Setter Target="PART_Elements.MaxWidth" Value="512" />
											</VisualState.Setters>
										</VisualState>
									</VisualStateGroup>
									<VisualStateGroup x:Name="Notification">
										<VisualStateGroup.Transitions>
											<VisualTransition To="Collapsed">
												<Storyboard>
													<DoubleAnimation Storyboard.TargetName="PART_Notification"
																	 Storyboard.TargetProperty="Opacity"
																	 To="0"
																	 Duration="0:0:1" />
												</Storyboard>
											</VisualTransition>
											<VisualTransition To="Visible">
												<Storyboard>
													<DoubleAnimation Storyboard.TargetName="PART_Notification"
																	 Storyboard.TargetProperty="Opacity"
																	 From="0.5"
																	 To="1"
																	 Duration="0:0:0.2" />
												</Storyboard>
											</VisualTransition>
										</VisualStateGroup.Transitions>
										<VisualState x:Name="Collapsed">
											<VisualState.Setters>
												<Setter Target="PART_Notification.MaxWidth" Value="0" />
											</VisualState.Setters>
										</VisualState>
										<VisualState x:Name="Visible">
											<VisualState.Setters>
												<Setter Target="PART_Notification.MaxWidth" Value="512" />
												<Setter Target="PART_Notification.Opacity" Value="1" />
											</VisualState.Setters>
										</VisualState>
									</VisualStateGroup>
									<VisualStateGroup x:Name="HorizontalDirection">
										<VisualState x:Name="Right" />
										<VisualState x:Name="Left">
											<VisualState.Setters>
												<Setter Target="PART_Anchor.(Grid.Column)" Value="1" />
												<Setter Target="PART_Elements.(Grid.Column)" Value="0" />
												<Setter Target="PART_Notification.(local:RelativePlacement.Mode)" Value="Left" />
											</VisualState.Setters>
										</VisualState>
									</VisualStateGroup>
								</VisualStateManager.VisualStateGroups>

								<Grid
									x:Name="PART_Toolbar"
									BorderThickness="{TemplateBinding BorderThickness}"
									BorderBrush="{ThemeResource DiagnosticsOverlayBorderBrush}"
									CornerRadius="{TemplateBinding CornerRadius}"
									Height="{TemplateBinding Height}"
									Background="{ThemeResource DiagnosticsOverlayBackgroundBrush}">
									<Grid.ColumnDefinitions>
										<ColumnDefinition Width="Auto" />
										<ColumnDefinition Width="Auto" />
									</Grid.ColumnDefinitions>

									<Border
										x:Name="PART_Anchor"
										Grid.Column="0"
										BorderThickness="0,0,1,0"
										BorderBrush="{ThemeResource DiagnosticsOverlayBorderBrush}"
										Background="{ThemeResource DiagnosticsOverlayBackgroundBrush}">
										<Path
											Stretch="Uniform"
											Width="12"
											Height="16"
											VerticalAlignment="Center"
											HorizontalAlignment="Center"
											Data="{StaticResource AnchorIcon}"
											Fill="{ThemeResource DiagnosticsAnchorFillBrush}" />
									</Border>

									<StackPanel
										x:Name="PART_Elements"
										Grid.Column="1"
										MaxWidth="0"
										Spacing="4"
										Padding="4,0"
										Orientation="Horizontal"
										VerticalAlignment="Center"/>
								</Grid>

								<ContentPresenter
									x:Name="PART_Notification"
									local:RelativePlacement.Mode="Right"
									Opacity="0.5"
									VerticalAlignment="Center" />
							</Canvas>
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");
	}

	/// <summary>
	/// Make the overlay visible.
	/// </summary>
	/// <remarks>This can be invoked from any thread.</remarks>>
	public void Show(bool? isExpanded = null)
	{
		_isVisible = true;
		if (isExpanded is not null)
		{
			_isExpanded = isExpanded.Value;
		}
		EnqueueUpdate();
	}

	/// <summary>
	/// Hide the overlay.
	/// </summary>
	/// <remarks>This can be invoked from any thread.</remarks>>
	public void Hide()
	{
		_isVisible = false;
		EnqueueUpdate(forceUpdate: true);
	}

	/// <summary>
	/// Hide the given view from the overlay.
	/// </summary>
	/// <param name="viewId"><see cref="IDiagnosticView.Id"/> of the <see cref="IDiagnosticView"/> to hide.</param>
	public void Hide(string viewId)
	{
		lock (_updateGate)
		{
			_configuredElementVisibilities[viewId] = false;
		}
		EnqueueUpdate();
	}

	/// <summary>
	/// Hide the given view from the overlay.
	/// </summary>
	/// <param name="viewId"><see cref="IDiagnosticView.Id"/> of the <see cref="IDiagnosticView"/> to hide.</param>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	public void Show(string viewId)
	{
		lock (_updateGate)
		{
			_configuredElementVisibilities[viewId] = true;
		}
		Show();
	}

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	public void Add(string id, string name, UIElement preview, Func<UIElement>? details = null, DiagnosticViewRegistrationPosition position = default)
		=> Add(new DiagnosticView(id, name, _ => preview, (_, ct) => new(details?.Invoke()), position));

	/// <summary>
	/// Add a UI diagnostic element to this overlay.
	/// </summary>
	/// <remarks>This will also make this overlay visible (cf. <see cref="Show(bool?)"/>).</remarks>
	/// <param name="provider">The provider to add.</param>
	public void Add(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			_localRegistrations.Add(provider);
		}

		EnqueueUpdate(); // Making IsVisible = true wil (try) to re-enqueue the update, but safer to keep it here anyway.
		Show();
	}

	public UIElement? Find(string viewId)
		=> _elements.Values.FirstOrDefault(elt => elt.View.Id == viewId)?.Value;

#if HAS_UNO
	/// <inheritdoc />
	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		base.OnVisibilityChanged(oldValue, newValue);

		EnqueueUpdate(forceUpdate: newValue is not Visibility.Visible); // Force update when hiding.
	}
#endif

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		if (_toolbar is not null)
		{
			//_anchor.Tapped -= OnAnchorTapped;
			_toolbar.ManipulationDelta -= OnAnchorManipulated;
			_toolbar.ManipulationCompleted -= OnAnchorManipulatedCompleted;
		}
		if (_notificationPresenter is not null)
		{
			_notificationPresenter.Tapped -= OnNotificationTapped;
		}

#if __ANDROID__ || __IOS__
		if (_toolbar is not null)
		{
			_toolbar.SizeChanged += OnToolBarSizeChanged;
		}
#endif

		base.OnApplyTemplate();

		_toolbar = GetTemplateChild(ToolbarPartName) as FrameworkElement;
		_elementsPanel = GetTemplateChild(ElementsPanelPartName) as Panel;
		_anchor = GetTemplateChild(AnchorPartName) as UIElement;
		_notificationPresenter = GetTemplateChild(NotificationPartName) as ContentPresenter;

		if (_toolbar is not null)
		{
			//_anchor.Tapped += OnAnchorTapped;
			_toolbar.ManipulationDelta += OnAnchorManipulated;
			_toolbar.ManipulationCompleted += OnAnchorManipulatedCompleted;
			_toolbar.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
			RenderTransform = new TranslateTransform();
		}
		if (_notificationPresenter is not null)
		{
			RelativePlacement.SetAnchor(_notificationPresenter, _toolbar);
			_notificationPresenter.Tapped += OnNotificationTapped;
		}
#if __ANDROID__ || __IOS__
		if (_toolbar is not null)
		{
			_toolbar.SizeChanged += OnToolBarSizeChanged;
		}

		static void OnToolBarSizeChanged(object sender, SizeChangedEventArgs args)
		{
			// Patches pointer event dispatch on a 0x0 Canvas
			if (sender is UIElement uie && uie.GetTemplatedParent() is DiagnosticsOverlay { TemplatedRoot: Canvas canvas } overlay)
			{
				canvas.Width = args.NewSize.Width;

#if __ANDROID__
				// Required for Android to effectively update the ActualSize allowing the RenderTransformAdapter to accept to apply the transform.
				overlay.DispatcherQueue.TryEnqueue(() => canvas.InvalidateMeasure());
#endif
			}
		}
#endif

		VisualStateManager.GoToState(this, _isExpanded ? DisplayModeExpandedStateName : DisplayModeCompactStateName, false);
		VisualStateManager.GoToState(this, NotificationCollapsedStateName, false);
		EnqueueUpdate();
	}

	//private void OnAnchorTapped(object sender, TappedRoutedEventArgs args)
	//{
	//	_isExpanded = !_isExpanded;
	//	VisualStateManager.GoToState(this, _isExpanded ? DisplayModeExpandedStateName : DisplayModeCompactStateName, true);
	//	args.Handled = true;
	//}

	private void EnqueueUpdate(bool forceUpdate = false)
	{
		var context = _context;
		var isHidden = !_isVisible;
		if ((isHidden && !forceUpdate)
			|| context is null
			|| Interlocked.CompareExchange(ref _updateEnqueued, 1, 0) is not 0)
		{
			return;
		}

		context.Schedule(() =>
		{
			_updateEnqueued = 0;

			if (isHidden || Visibility is not Visibility.Visible)
			{
				if (_overlayHost is { } h)
				{
					ShowHost(h, false);
				}

				return;
			}

			// If the _elementsPanel is null, we need to let layout pass to get it.
			var host = _overlayHost ??= CreateHost(_root, this);
			if (_elementsPanel is null)
			{
				// Once injected in the visual tree (cf. CreateHost), we try to force the template to be applied.
				ApplyTemplate();
				if (_elementsPanel is null)
				{
					// Template still not applied, we'll try again later (OnApplyTemplate will invoke back EnqueueUpdate).
					ShowHost(host, true);
					return;
				}
			}

			var visibleViews = 0;
			lock (_updateGate)
			{
				var viewsThatShouldBeMaterialized = DiagnosticViewRegistry
					.Registrations
					.Where(ShouldMaterialize)
					.Select(reg => reg.View)
					.Concat(_localRegistrations)
					.OrderBy(r => (int)r.Position)
					.Distinct();

				foreach (var view in viewsThatShouldBeMaterialized)
				{
					ref var element = ref CollectionsMarshal.GetValueRefOrAddDefault(_elements, view, out var hasElement);
					if (!hasElement)
					{
						element = new DiagnosticElement(this, view, _context!);
						_elementsPanel.Children.Add(element.Value);
					}
				}

				foreach (var element in _elements.Values)
				{
					if (_configuredElementVisibilities.GetValueOrDefault(element.View.Id, true))
					{
						var currentIndex = _elementsPanel.Children.IndexOf(element.Value);
						if (currentIndex is -1)
						{
							_elementsPanel.Children.Insert(visibleViews, element.Value);
						}
						else if (currentIndex != visibleViews)
						{
							global::System.Diagnostics.Debug.Fail("Invalid index, patching");
							_elementsPanel.Children.Move((uint)currentIndex, (uint)visibleViews);
						}

						visibleViews++;
					}
					else
					{
						_elementsPanel.Children.Remove(element.Value);
					}
				}
			}

			ShowHost(host, isVisible: visibleViews is not 0);
			UpdatePlacement();
		});
	}

	private static Popup CreateHost(XamlRoot root, DiagnosticsOverlay overlay)
	{
		var host = new Popup
		{
			XamlRoot = root,
			Child = overlay,
			IsLightDismissEnabled = false,
			LightDismissOverlayMode = LightDismissOverlayMode.Off
		};

		host.Opened += static (snd, e) => ((snd as Popup)?.Child as DiagnosticsOverlay)?.InitPlacement();
		host.Closed += static (snd, e) => ((snd as Popup)?.Child as DiagnosticsOverlay)?.CleanPlacement();

		return host;
	}

	private static void ShowHost(Popup host, bool isVisible)
		=> host.IsOpen = isVisible;

	private bool ShouldMaterialize(DiagnosticViewRegistration registration)
	{
		if (_configuredElementVisibilities.TryGetValue(registration.View.Id, out var isVisible) && isVisible)
		{
			// We explicitly requested to show that view, so yes we have to materialize it!
			return true;
		}

		return registration.Mode switch
		{
			DiagnosticViewRegistrationMode.All => true,
			DiagnosticViewRegistrationMode.OnDemand => false,
			_ => !_overlays.Any(overlay => overlay.Value.IsMaterialized(registration.View))
		};
	}

	private bool IsMaterialized(IDiagnosticView provider)
	{
		lock (_updateGate)
		{
			return _elements.ContainsKey(provider);
		}
	}
}
#endif
