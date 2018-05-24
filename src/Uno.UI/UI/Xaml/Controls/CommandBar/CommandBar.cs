using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(PrimaryCommands))]
	[TemplatePart(Name = MoreButton, Type = typeof(Button))]
	[TemplatePart(Name = OverflowPopup, Type = typeof(Popup))]
	[TemplatePart(Name = PrimaryItemsControl, Type = typeof(ItemsControl))]
	[TemplatePart(Name = SecondaryItemsControl, Type = typeof(ItemsControl))]
	public partial class CommandBar : AppBar
	{
		private const string MoreButton = "MoreButton";
		private const string OverflowPopup = "OverflowPopup";
		private const string PrimaryItemsControl = "PrimaryItemsControl";
		private const string SecondaryItemsControl = "SecondaryItemsControl";

		private Button _moreButton;
		private Popup _overflowPopup;
		private ItemsControl _primaryItemsControl;
		private ItemsControl _secondaryItemsControl;

		public CommandBar()
		{
			PrimaryCommands = new ObservableVector<ICommandBarElement>();
			SecondaryCommands = new ObservableVector<ICommandBarElement>();

			CommandBarTemplateSettings = new CommandBarTemplateSettings(this);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_moreButton = GetTemplateChild(MoreButton) as Button;
			_overflowPopup = GetTemplateChild(OverflowPopup) as Popup;
			_primaryItemsControl = GetTemplateChild(PrimaryItemsControl) as ItemsControl;
			_secondaryItemsControl = GetTemplateChild(SecondaryItemsControl) as ItemsControl;
			
			if (_moreButton != null)
			{
				_moreButton.Click += (s, e) =>
				{
					IsOpen = !IsOpen;
				};
			}

			if (_overflowPopup != null)
			{
				_overflowPopup.Closed += (s, e) =>
				{
					IsOpen = false;
				};
			}

			PrimaryCommands.VectorChanged += (s, e) => UpdateCommands();
			SecondaryCommands.VectorChanged += (s, e) => UpdateCommands();
			
			UpdateCommands();

			this.RegisterPropertyChangedCallback(IsEnabledProperty, (s, e) => UpdateCommonState());
			this.RegisterPropertyChangedCallback(ClosedDisplayModeProperty, (s, e) => UpdateDisplayModeState());
			this.RegisterPropertyChangedCallback(IsOpenProperty, (s, e) =>
			{
				// TODO: Consider the content of _secondaryItemsControl when IsDynamicOverflowEnabled is supported.
				var hasSecondaryItems = SecondaryCommands.Any(); 
				if (hasSecondaryItems)
				{
					if (_overflowPopup != null)
					{
						_overflowPopup.IsOpen = true;
					}
				}

				UpdateDisplayModeState();
				UpdateButtonsIsCompact();
			});

			UpdateCommonState();
			UpdateDisplayModeState();
			UpdateButtonsIsCompact();
		}

		private void UpdateButtonsIsCompact()
		{
			var allCommands = Enumerable.Concat(PrimaryCommands, SecondaryCommands).OfType<ICommandBarElement>();

			foreach (var command in allCommands)
			{
				command.IsCompact = !IsOpen;
			};
		}

		public event TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs> DynamicOverflowItemsChanging;

#region PrimaryCommands

		public IObservableVector<ICommandBarElement> PrimaryCommands
		{
			get => (IObservableVector<ICommandBarElement>)this.GetValue(PrimaryCommandsProperty);
			private set => this.SetValue(PrimaryCommandsProperty, value);
		}

		public static DependencyProperty PrimaryCommandsProperty { get; } =
			DependencyProperty.Register(
				"PrimaryCommands",
				typeof(IObservableVector<ICommandBarElement>),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(
					default(IObservableVector<ICommandBarElement>),
					FrameworkPropertyMetadataOptions.ValueInheritsDataContext
				)
			);

#endregion

#region SecondaryCommands

		public IObservableVector<ICommandBarElement> SecondaryCommands
		{
			get => (IObservableVector<ICommandBarElement>)this.GetValue(SecondaryCommandsProperty);
			private set => this.SetValue(SecondaryCommandsProperty, value);
		}

		public static DependencyProperty SecondaryCommandsProperty { get; } =
			DependencyProperty.Register(
				"SecondaryCommands",
				typeof(IObservableVector<ICommandBarElement>),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(
					default(IObservableVector<ICommandBarElement>),
					FrameworkPropertyMetadataOptions.ValueInheritsDataContext
				)			
			);

#endregion

#region CommandBarOverflowPresenterStyle

		public Style CommandBarOverflowPresenterStyle
		{
			get => (Style)GetValue(CommandBarOverflowPresenterStyleProperty);
			set => SetValue(CommandBarOverflowPresenterStyleProperty, value);
		}

		[Uno.NotImplemented]
		public static DependencyProperty CommandBarOverflowPresenterStyleProperty { get; } =
			DependencyProperty.Register(
				"CommandBarOverflowPresenterStyle",
				typeof(Style),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(default(Style))
			);

#endregion

#region OverflowButtonVisibility

		public CommandBarOverflowButtonVisibility OverflowButtonVisibility
		{
			get => (CommandBarOverflowButtonVisibility)this.GetValue(OverflowButtonVisibilityProperty);
			set => SetValue(OverflowButtonVisibilityProperty, value);
		}

		public static DependencyProperty OverflowButtonVisibilityProperty { get; } =
			DependencyProperty.Register(
				"OverflowButtonVisibility",
				typeof(CommandBarOverflowButtonVisibility),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(default(CommandBarOverflowButtonVisibility))
			);

#endregion

#region IsDynamicOverflowEnabled

		public bool IsDynamicOverflowEnabled
		{
			get => (bool)this.GetValue(IsDynamicOverflowEnabledProperty);
			set => SetValue(IsDynamicOverflowEnabledProperty, value);
		}

		public static DependencyProperty IsDynamicOverflowEnabledProperty { get; } =
			DependencyProperty.Register(
				"IsDynamicOverflowEnabled",
				typeof(bool),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(default(bool))
			);

#endregion

#region DefaultLabelPosition

		public CommandBarDefaultLabelPosition DefaultLabelPosition
		{
			get => (CommandBarDefaultLabelPosition)this.GetValue(DefaultLabelPositionProperty);
			set => SetValue(DefaultLabelPositionProperty, value);
		}

		public static DependencyProperty DefaultLabelPositionProperty { get; } =
			DependencyProperty.Register(
				"DefaultLabelPosition",
				typeof(CommandBarDefaultLabelPosition),
				typeof(CommandBar),
				new FrameworkPropertyMetadata(default(CommandBarDefaultLabelPosition))
			);

#endregion

		public CommandBarTemplateSettings CommandBarTemplateSettings { get; }

		private void UpdateCommands()
		{
			DynamicOverflowItemsChanging?.Invoke(this, null);

			// TODO: Remove/Add delta, to preserve state of existing buttons
			if (_primaryItemsControl != null)
			{
				_primaryItemsControl.ItemsSource = PrimaryCommands;
			}
			if (_secondaryItemsControl != null)
			{
				_secondaryItemsControl.ItemsSource = SecondaryCommands;
			}

			PrimaryCommands.OfType<ICommandBarElement3>().ForEach(command => command.IsInOverflow = false);
			SecondaryCommands.OfType<ICommandBarElement3>().ForEach(command => command.IsInOverflow = true);
			
			UpdateAvailableCommandsState();
		}

		private void UpdateCommonState()
		{
			var commonState = IsEnabled
				? "Normal"
				: "Disabled";

			VisualStateManager.GoToState(this, commonState, true);
		}

		private void UpdateDisplayModeState()
		{
			string GetDisplayMode()
			{
				switch (ClosedDisplayMode)
				{
					case AppBarClosedDisplayMode.Compact: return "Compact";
					case AppBarClosedDisplayMode.Minimal: return "Minimal";
					case AppBarClosedDisplayMode.Hidden: return "Hidden";
					default: return null;
				}
			}

			string GetState()
			{
				// TODO: Add support for "OpenDown" and "OpenUp" DisplayModeStates.
				this.Log().Warn("Only the Closed DisplayModeState is supported at the moment.");

				return "Closed";
			}

			var displayModeState = GetDisplayMode() + GetState();

			VisualStateManager.GoToState(this, displayModeState, true);
		}

		private void UpdateAvailableCommandsState()
		{
			string availableCommandsState;

			if (PrimaryCommands.Any() && SecondaryCommands.None())
			{
				availableCommandsState = "PrimaryCommandsOnly";
			}
			else if (PrimaryCommands.None() && SecondaryCommands.Any())
			{
				availableCommandsState = "SecondaryCommandsOnly";
			}
			else
			{
				availableCommandsState = "BothCommands";
			}

			VisualStateManager.GoToState(this, availableCommandsState, true);
		}
	}
}