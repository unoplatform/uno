using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Lottie
{
	[Sample("Lottie", Name = "AnimatedVisualPlayer Visibility (#23189)", IgnoreInSnapshotTests = true)]
	public sealed partial class AnimatedVisualPlayerVisibility : Page
	{
		private UIElement _playerSlot;

		public AnimatedVisualPlayerVisibility()
		{
			this.InitializeComponent();

			_playerSlot = Player;

			PlayerVisibleToggle.Checked += (_, _) => Player.Visibility = Visibility.Visible;
			PlayerVisibleToggle.Unchecked += (_, _) => Player.Visibility = Visibility.Collapsed;

			ParentVisibleToggle.Checked += (_, _) => ParentContainer.Visibility = Visibility.Visible;
			ParentVisibleToggle.Unchecked += (_, _) => ParentContainer.Visibility = Visibility.Collapsed;

			InTreeToggle.Checked += (_, _) =>
			{
				if (ParentContainer.Child is null)
				{
					ParentContainer.Child = _playerSlot;
				}
				UpdateEffectiveVisibilityText();
			};
			InTreeToggle.Unchecked += (_, _) =>
			{
				if (ParentContainer.Child is not null)
				{
					ParentContainer.Child = null;
				}
				UpdateEffectiveVisibilityText();
			};

			PlayButton.Click += (_, _) => _ = Player.PlayAsync(0.0, 1.0, looped: true);
			StopButton.Click += (_, _) => Player.Stop();
			PauseButton.Click += (_, _) => Player.Pause();
			ResumeButton.Click += (_, _) => Player.Resume();

			Player.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateEffectiveVisibilityText());
			ParentContainer.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateEffectiveVisibilityText());
			Loaded += (_, _) => UpdateEffectiveVisibilityText();
		}

		private void UpdateEffectiveVisibilityText()
		{
			var effectivelyVisible =
				Player.Visibility == Visibility.Visible &&
				ParentContainer.Visibility == Visibility.Visible &&
				ParentContainer.Child is not null;

			EffectiveVisibilityText.Text = effectivelyVisible ? "Visible" : "Collapsed";
		}
	}
}
