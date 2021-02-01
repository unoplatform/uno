using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno;
using Uno.UI.Samples.Controls;

namespace SampleControl.Entities
{
	[Windows.UI.Xaml.Data.Bindable]
	[DebuggerDisplay("{" + nameof(ControlName) + "}")]
	public partial class SampleChooserContent : INotifyPropertyChanged
	{
		public string ControlName { get; set; }
		public Type ViewModelType { get; set; }
		public Type ControlType { get; set; }
		public string[] Categories { get; set; }
		public string Description { get; set; }
		public bool IgnoreInSnapshotTests { get; internal set; }

		bool _isFavorite;
		public bool IsFavorite
		{
			get => _isFavorite;
			set
			{
				if (_isFavorite != value)
				{
					_isFavorite = value;
					RaisePropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public override bool Equals(object obj) =>
			obj switch {
				SampleChooserContent other => Equals(ControlName, other.ControlName),
				_ => false
			};

		public override int GetHashCode() => ControlName.GetHashCode();
	}
}
