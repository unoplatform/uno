namespace Windows.ApplicationModel.Appointments
{
	public partial interface IAppointmentParticipant
	{
		string Address { get; set; }
		string DisplayName { get; set; }
	}
}
