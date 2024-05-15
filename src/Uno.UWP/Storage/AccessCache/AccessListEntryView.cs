using System.Collections;
using System.Collections.Generic;

namespace Windows.Storage.AccessCache;

public partial class AccessListEntryView : IReadOnlyList<AccessListEntry>, IEnumerable<AccessListEntry>
{
	internal AccessListEntryView()
	{
	}

	public uint Size
	{
		get
		{
			throw new global::System.NotImplementedException("The member uint AccessListEntryView.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented#m=uint%20AccessListEntryView.Size");
		}
	}
	public AccessListEntry this[int index]
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}

	public IEnumerator<AccessListEntry> GetEnumerator()
	{
		throw new global::System.NotSupportedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new global::System.NotSupportedException();
	}

	public int Count
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}
}
