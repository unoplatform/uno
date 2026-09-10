#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskRegistrationGroup
{
	private readonly string _id;
	private readonly string _name;

	public BackgroundTaskRegistrationGroup(string id)
		: this(id, id)
	{
	}

	public BackgroundTaskRegistrationGroup(string id, string name)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("A task group id is required.", nameof(id));
		}

		ArgumentNullException.ThrowIfNull(name);
		_id = id;
		_name = name;
	}

	public IReadOnlyDictionary<Guid, BackgroundTaskRegistration> AllTasks
	{
		get
		{
			var registrations = BackgroundTaskRegistrationStore
				.GetAll()
				.Where(record => string.Equals(record.GroupId, Id, StringComparison.Ordinal))
				.ToDictionary(
					record => record.TaskId,
					record => new BackgroundTaskRegistration(record));
			return new ReadOnlyDictionary<Guid, BackgroundTaskRegistration>(registrations);
		}
	}

	public string Id => _id;

	public string Name => _name;

	public event TypedEventHandler<
		BackgroundTaskRegistrationGroup,
		Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs>?
		BackgroundActivated;

	internal void RaiseBackgroundActivated(
		Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs args)
		=> BackgroundActivated?.Invoke(this, args);
}
