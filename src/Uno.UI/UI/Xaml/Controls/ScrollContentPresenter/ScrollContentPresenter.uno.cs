using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls
{
	public class ScrollContentPresenter
	{
		/// <summary>
		/// Backing property for the IsPointerWheelReversed attached property.
		/// </summary>
		[DynamicDependency(nameof(GetIsPointerWheelReversed))]
		[DynamicDependency(nameof(SetIsPointerWheelReversed))]
		public static readonly DependencyProperty IsPointerWheelReversedProperty = DependencyProperty.RegisterAttached(
			"IsPointerWheelReversed",
			typeof(bool),
			typeof(Microsoft.UI.Xaml.Controls.ScrollContentPresenter),
			new FrameworkPropertyMetadata((snd, e) => ((Microsoft.UI.Xaml.Controls.ScrollContentPresenter)snd).IsPointerWheelReversed = (bool)e.NewValue));

		/// <summary>
		/// Gets a boolean which indicates if the pointer wheel should be reversed or not for the <paramref name="scrollViewer"/>.
		/// </summary>
		/// <param name="scrollViewer"></param>
		/// <returns></returns>
		public static bool GetIsPointerWheelReversed(Microsoft.UI.Xaml.Controls.ScrollContentPresenter scrollViewer)
			=> (bool)scrollViewer.GetValue(IsPointerWheelReversedProperty);

		/// <summary>
		/// Sets if the pointer wheel should be reversed or not for the <paramref name="scrollViewer"/>.
		/// </summary>
		/// <param name="scrollViewer">The target ScrollViewer to configure</param>
		/// <param name="isReversed">A boolean which indicates if the wheel should be reversed of not.</param>
		public static void SetIsPointerWheelReversed(Microsoft.UI.Xaml.Controls.ScrollContentPresenter scrollViewer, bool isReversed)
			=> scrollViewer.SetValue(IsPointerWheelReversedProperty, isReversed);

	}
}

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
		private bool _isPointerWheelReversed;

		/// <summary>
		/// Cached value of <see cref="Uno.UI.Xaml.Controls.ScrollContentPresenter.IsPointerWheelReversedProperty"/>,
		/// in order to not access the DP on each scroll (perf considerations)
		/// </summary>
		internal bool IsPointerWheelReversed
		{
			get => _isPointerWheelReversed;
			set
			{
				if (_isPointerWheelReversed != value)
				{
					_isPointerWheelReversed = value;
					OnIsPointerWheelReversedChanged(value);
				}
			}
		}

		partial void OnIsPointerWheelReversedChanged(bool isReversed);
	}
}
