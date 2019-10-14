using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	public partial class AppBarButton : Button, ICommandBarElement, ICommandBarElement2, ICommandBarElement3
	{
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			RegisterPropertyChangedCallback(IsCompactProperty, (s, e) => UpdateApplicationViewState());
			RegisterPropertyChangedCallback(IsInOverflowProperty, (s, e) => UpdateApplicationViewState());
			UpdateApplicationViewState();

			SetupContentUpdate();
		}

		public AppBarButtonTemplateSettings TemplateSettings { get; } = new AppBarButtonTemplateSettings();

		#region Label

		public string Label
		{
			get => (string)GetValue(LabelProperty);
			set => SetValue(LabelProperty, value);
		}

		public static DependencyProperty LabelProperty { get; } =
			DependencyProperty.Register(
				"Label", typeof(string),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(string))
			);

		#endregion

		#region Icon

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static DependencyProperty IconProperty { get; } =
			DependencyProperty.Register(
				"Icon",
				typeof(IconElement),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(IconElement))
			);

		#endregion

		#region IsInOverflow

		public bool IsInOverflow
		{
			get => (bool) this.GetValue(IsInOverflowProperty);
			internal set => this.SetValue(IsInOverflowProperty, value);
		}

		bool ICommandBarElement3.IsInOverflow
		{
			get => IsInOverflow;
			set => IsInOverflow = value;
		}

		public static DependencyProperty IsInOverflowProperty { get; } =
			DependencyProperty.Register(
				"IsInOverflow",
				typeof(bool),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(false));

		#endregion

		#region LabelPosition

		public CommandBarLabelPosition LabelPosition
		{
			get => (CommandBarLabelPosition)this.GetValue(LabelPositionProperty);
			set => this.SetValue(LabelPositionProperty, value);
		}

		public static DependencyProperty LabelPositionProperty { get; } =
			DependencyProperty.Register(
				"LabelPosition",
				typeof(CommandBarLabelPosition),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(CommandBarLabelPosition))
			);

		#endregion

		#region IsCompat

		public bool IsCompact
		{
			get => (bool)this.GetValue(IsCompactProperty);
			set => this.SetValue(IsCompactProperty, value);
		}

		public static DependencyProperty IsCompactProperty { get; } =
			DependencyProperty.Register(
				"IsCompact",
				typeof(bool),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(bool))
			);

		#endregion

		#region DynamicOverflowOrder

		public int DynamicOverflowOrder
		{
			get => (int)this.GetValue(DynamicOverflowOrderProperty);
			set => this.SetValue(DynamicOverflowOrderProperty, value);
		}

		public static DependencyProperty DynamicOverflowOrderProperty { get; } =
			DependencyProperty.Register(
				"DynamicOverflowOrder",
				typeof(int),
				typeof(AppBarButton),
				new FrameworkPropertyMetadata(default(int))
			);

		#endregion

		private void UpdateApplicationViewState()
		{
			string applicationViewState;

			if (IsInOverflow)
			{
				applicationViewState = "Overflow";
			}
			else if (IsCompact)
			{
				applicationViewState = "Compact";
			}
			else
			{
				applicationViewState = "FullSize";
			}

			VisualStateManager.GoToState(this, applicationViewState, true);
		}

		// TODO: Remove this when ContentPresenter's implicit ContentProperty TemplateBinding is supported.
		private void SetupContentUpdate()
		{
			var contentPresenter = GetTemplateChild("Content") as ContentPresenter;

			void UpdateContent()
			{
				if (contentPresenter != null)
				{
					contentPresenter.Content = Icon ?? Content;
				}
			}

			RegisterPropertyChangedCallback(ContentProperty, (s, e) => UpdateContent());
			RegisterPropertyChangedCallback(IconProperty, (s, e) => UpdateContent());
			UpdateContent();
		}
	}
}
