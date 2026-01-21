using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno;

namespace SampleControl.Entities
{
	[Microsoft.UI.Xaml.Data.Bindable]
	public partial class SampleChooserCategory : IComparer<SampleChooserContent>, IComparable<SampleChooserCategory>
	{
		public SampleChooserCategory()
		{
			SamplesContent = new SortedSet<SampleChooserContent>(comparer: this);
		}

		public SampleChooserCategory(IGrouping<string, SampleChooserContent> contents) : this(contents.Key, contents)
		{
		}

		public SampleChooserCategory(string category, IEnumerable<SampleChooserContent> contents)
		{
			Category = category;
			SamplesContent = new SortedSet<SampleChooserContent>(contents, comparer: this);
		}

		public SortedSet<SampleChooserContent> SamplesContent { get; }

		public string Category { get; set; }

		public int Count => SamplesContent.Count;

		public override bool Equals(object obj) =>
			obj switch
			{
				SampleChooserCategory other => Equals(Category, other.Category),
				_ => false
			};

		public override int GetHashCode() => Category?.GetHashCode() ?? 0;

		/// <inheritdoc />
		public override string ToString()
			=> Category ?? "";

		public int Compare(SampleChooserContent x, SampleChooserContent y)
		{
			var comparisonResult = string.Compare(x.ControlName, y.ControlName, StringComparison.InvariantCultureIgnoreCase);
			if (comparisonResult != 0)
			{
				return comparisonResult;
			}

			return string.Compare(x.ControlType.FullName, y.ControlType.FullName, StringComparison.InvariantCultureIgnoreCase);
		}

		public int CompareTo(SampleChooserCategory other)
		{
			return ReferenceEquals(this, other)
				? 0
				: ReferenceEquals(null, other)
					? 1
					: string.Compare(Category, other.Category, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool operator ==(SampleChooserCategory left, SampleChooserCategory right) => Equals(left, right);

		public static bool operator !=(SampleChooserCategory left, SampleChooserCategory right) => !Equals(left, right);

		public static bool operator <(SampleChooserCategory left, SampleChooserCategory right) => left.CompareTo(right) < 0;

		public static bool operator >(SampleChooserCategory left, SampleChooserCategory right) => left.CompareTo(right) > 0;

		public static bool operator <=(SampleChooserCategory left, SampleChooserCategory right) => left.CompareTo(right) <= 0;

		public static bool operator >=(SampleChooserCategory left, SampleChooserCategory right) => left.CompareTo(right) >= 0;
	}
}
