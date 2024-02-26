using System.Collections.Generic;

namespace Windows.ApplicationModel.Appointments;

/// <summary>
/// Represents a set of options that modifies a query for appointments.
/// </summary>
public partial class FindAppointmentsOptions
{
	/// <summary>
	/// Creates a new instance of the FindAppointmentsOptions class.
	/// </summary>
	public FindAppointmentsOptions()
	{
	}

	/// <summary>
	/// Gets or sets the maximum number of appointments that should
	/// be included in the find appointments query result.
	/// </summary>
	public uint MaxCount { get; set; } = uint.MaxValue;

	/// <summary>
	/// Gets or sets a value indicating whether appointments belonging
	/// to hidden calendars will be included in the find appointments query result.
	/// </summary>
	public bool IncludeHidden { get; set; }

	/// <summary>
	/// Gets the list of appointment property names that will be
	/// populated with data in the find appointment query results.
	/// </summary>
	public IList<string> FetchProperties { get; } = new List<string>();
}
