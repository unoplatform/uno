using Uno.Extensions;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Data;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml
{
	[ContentProperty(Name = "Storyboard")]
	public partial class VisualTransition : DependencyObject
	{
		internal Action LazyBuilder { get; set; }

		public VisualTransition()
		{
			IsAutoPropertyInheritanceEnabled = false;
			InitializeBinder();
		}

		public string From { get; set; }

		public string To { get; set; }


		#region Storyboard DependencyProperty

		public Storyboard Storyboard
		{
			get
			{
				EnsureMaterialized();
				return (Storyboard)this.GetValue(StoryboardProperty);
			}
			set { this.SetValue(StoryboardProperty, value); }
		}

		private void EnsureMaterialized()
		{
			if (LazyBuilder != null)
			{
				var builder = LazyBuilder;
				LazyBuilder = null;
				builder.Invoke();

				if (Storyboard is IDependencyObjectStoreProvider storyboardProvider)
				{
					// Set the theme changed flag on so the update processes
					// the children.
					storyboardProvider.Store.UpdateResourceBindings(ResourceUpdateReason.ThemeResource);
				}
			}
		}

		public static DependencyProperty StoryboardProperty { get; } =
			DependencyProperty.Register(
				"Storyboard",
				typeof(Storyboard),
				typeof(VisualTransition),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild
				)
			);

		#endregion

		private Duration _generatedDuration;

		/// <summary>
		/// This property is not yet implemented in Uno Platform.
		/// </summary>
		/// <remarks>
		/// The code was moved here to override the LogLevel.
		/// </remarks>
		[Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Duration GeneratedDuration
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.VisualTransition", "Duration VisualTransition.GeneratedDuration", LogLevel.Debug);
				return _generatedDuration;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.VisualTransition", "Duration VisualTransition.GeneratedDuration", LogLevel.Debug);
				_generatedDuration = value;
			}
		}
	}
}
