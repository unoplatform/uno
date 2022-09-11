using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	internal partial class CounterGrid : Grid
	{
		/// <summary>
		/// How many instances of this type have been created?
		/// </summary>
		public static int CreationCount { get; private set; }

		/// <summary>
		/// How many times has an instance of this type been data-bound?
		/// </summary>
		public static int BindCount { get; private set; }

		public static int GlobalMeasureCount { get; private set; }
		public static int GlobalArrangeCount { get; private set; }

		/// <summary>
		/// How many times has this instance been data-bound?
		/// </summary>
		public int LocalBindCount { get; private set; }

		public int LocalMeasureCount { get; private set; }
		public int LocalArrangeCount { get; private set; }

		/// <summary>
		/// Raised whenever an instance of this type is created, data-bound, measured or arranged.
		/// </summary>
		public static event Action WasUpdated;

		public CounterGrid()
		{
			CreationCount++;
			WasUpdated?.Invoke();

			DataContextChanged += On_DataContextChanged;
		}

		private void On_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			if (args.NewValue != null)
			{
				BindCount++;
				LocalBindCount++;
				WasUpdated?.Invoke();
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			GlobalMeasureCount++;
			LocalMeasureCount++;
			WasUpdated?.Invoke();
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			GlobalArrangeCount++;
			LocalArrangeCount++;
			WasUpdated?.Invoke();
			return base.ArrangeOverride(finalSize);
		}

		/// <summary>
		/// Reset static counters.
		/// </summary>
		public static void Reset()
		{
			CreationCount = 0;
			BindCount = 0;
			GlobalMeasureCount = 0;
			GlobalArrangeCount = 0;
		}
	}

	internal partial class CounterGrid2 : Grid
	{
		public static int CreationCount { get; private set; }

		public static int BindCount { get; private set; }

		public static event Action WasUpdated;

		public CounterGrid2()
		{
			CreationCount++;
			WasUpdated?.Invoke();

			DataContextChanged += On_DataContextChanged;
		}

		private void On_DataContextChanged(DependencyObject sender, DataContextChangedEventArgs args)
		{
			if (args.NewValue != null)
			{
				BindCount++;
				WasUpdated?.Invoke();
			}
		}

		public static void Reset()
		{
			CreationCount = 0;
			BindCount = 0;
		}
	}
}
