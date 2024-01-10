using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno;
using Uno.Extensions;
using Uno.UI.Samples.Controls;

namespace SampleControl.Entities
{
	[Microsoft.UI.Xaml.Data.Bindable]
	[DebuggerDisplay("{" + nameof(ControlName) + "}")]
	public partial class SampleChooserContent : INotifyPropertyChanged
	{
		public string ControlName { get; set; }
		public Type ViewModelType { get; set; }
		public Type ControlType { get; set; }
		public string[] Categories { get; set; }
		public string CategoriesString => Categories?.JoinBy(", ");
		public string Description { get; set; }
		public string QueryString => $"?sample={Categories.FirstOrDefault() ?? ""}/{ControlName}";
		public bool IgnoreInSnapshotTests { get; internal set; }
		public bool IsManualTest { get; internal set; }
		public bool UsesFrame { get; internal set; }

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
			obj switch
			{
				SampleChooserContent other => Equals(ControlName, other.ControlName),
				_ => false
			};

		public override int GetHashCode() => ControlName.GetHashCode();
	}
}
