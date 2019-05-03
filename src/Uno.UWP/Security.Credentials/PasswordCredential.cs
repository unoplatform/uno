using System;

namespace Windows.Security.Credentials
{
	public sealed partial class PasswordCredential 
	{
		private string _userName;
		private string _resource;
		private string _password;

		public PasswordCredential()
			: this(string.Empty, string.Empty, string.Empty)
		{
		}

		public PasswordCredential(string resource,  string userName,  string password)
		{
			Resource = resource;
			UserName = userName;
			Password = password;
		}

		public string Resource
		{
			get => _resource;
			set => _resource = value ?? throw new ArgumentNullException(nameof(Resource));
		}

		public string UserName
		{
			get => _userName;
			set => _userName = value ?? throw new ArgumentNullException(nameof(UserName));
		}

		public string Password
		{
			get => _password;
			set => _password = value ?? throw new ArgumentNullException(nameof(Password));
		}

		public void RetrievePassword()
		{
			// Nothing to do, we never hide the password
		}
	}
}
