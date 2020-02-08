using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;

namespace SampleControl.Entities
{
	[Windows.UI.Xaml.Data.Bindable]
	[GeneratedEquality]
	public partial class SampleChooserCategory : IComparer<SampleChooserContent>, IComparable<SampleChooserCategory>
	{
		public SampleChooserCategory()
		{
			SamplesContent = new SortedSet<SampleChooserContent>(comparer: this);
		}

		public SampleChooserCategory(IGrouping<string, SampleChooserContent> contents)
		{
			Category = contents.Key;
			SamplesContent = new SortedSet<SampleChooserContent>(contents, comparer: this);
		}

		public SortedSet<SampleChooserContent> SamplesContent { get; }
		[EqualityKey]
		public string Category { get; set; }
		[EqualityIgnore]
		public int Count => SamplesContent.Count;

		public int Compare(SampleChooserContent x, SampleChooserContent y)
			=> string.Compare(x.ControlName, y.ControlName, StringComparison.InvariantCultureIgnoreCase);

		public int CompareTo(SampleChooserCategory other)
		{
			return ReferenceEquals(this, other)
				? 0
				: ReferenceEquals(null, other)
					? 1
					: string.Compare(Category, other.Category, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
