using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Uno.Disposables;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo(nameof(ComboBox), nameof(ComboBox_ObsCol_Reset))]
	public sealed partial class ComboBox_ObsCol_Reset : UserControl
	{
		private readonly BatchUpdateObservableCollection<string> _source = new BatchUpdateObservableCollection<string>();

		public ComboBox_ObsCol_Reset()
		{
			this.InitializeComponent();
			this.DataContext = _source;

			foreach (var button in ButtonPanel.Children.OfType<Button>())
			{
				button.Click += (s, e) =>
				{
					if (Regex.Match((s as Button).Content.ToString(), @"^(?<prefix>\w)(?<count>\d+)$") is { Success: true } match)
					{
						ResetSource(
							match.Groups["prefix"].Value,
							int.Parse(match.Groups["count"].Value)
						);
					}
				};
			}

			ResetSource("X", 7);
		}

		private void ResetSource(string prefix, int count)
		{
			using (_source.BatchUpdate())
			{
				_source.Clear();
				for (int i = 0; i < count; i++)
				{
					_source.Add(prefix + i);
				}
			}
		}
	}

	public class BatchUpdateObservableCollection<T> : ObservableCollection<T>
	{
		private int _batchUpdateCount;

		public IDisposable BatchUpdate()
		{
			++_batchUpdateCount;
			return Disposable.Create(Release);

			void Release()
			{
				if (--_batchUpdateCount <= 0)
				{
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				}
			}
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_batchUpdateCount > 0)
			{
				return;
			}

			base.OnCollectionChanged(e);
		}
	}

	public class ItemModel
	{
		public string Text { get; set; }

		public override string ToString() => Text;
	}
}
