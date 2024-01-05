using System;
using System.IO;
using Foundation;
using Security;

namespace Windows.Security.Credentials
{
	sealed partial class PasswordVault
	{
		public PasswordVault()
			: this(new KeyChainPersister())
		{
		}

		private sealed class KeyChainPersister : IPersister
		{
			private const string _accountId = "uno_passwordvault";

			private readonly SecRecord _query = new SecRecord(SecKind.GenericPassword) { Account = _accountId };

			public bool TryOpenRead(out Stream inputStream)
			{
				var record = SecKeyChain.QueryAsRecord(_query, out var statusCode);
				if (statusCode == SecStatusCode.Success)
				{
					inputStream = record!.ValueData!.AsStream();
					return true;
				}
				else
				{
					CheckCommonStatusCodes(statusCode);

					inputStream = Stream.Null;
					return false;
				}
			}

			public WriteTransaction OpenWrite(out Stream outputStream)
			{
				var stream = new MemoryStream();
				outputStream = stream;

				return new WriteTransaction(onCommit: () => Commit());

				void Commit()
				{
					stream.Position = 0;
					var record = new SecRecord()
					{
						ValueData = NSData.FromStream(stream)
					};


					var result = SecKeyChain.Update(_query, record);
					if (result == SecStatusCode.ItemNotFound)
					{
						stream.Position = 0;
						record = new SecRecord(SecKind.GenericPassword)
						{
							Account = _accountId,
							ValueData = NSData.FromStream(stream)
						};

						result = SecKeyChain.Add(record);
					}

					if (result != SecStatusCode.Success)
					{
						CheckCommonStatusCodes(result);
						throw new InvalidOperationException("Failed to persist the vault");
					}
				}
			}

			private void CheckCommonStatusCodes(SecStatusCode code)
			{
				if (code == SecStatusCode.MissingEntitlement)
				{
					throw new InvalidOperationException("Your application is not allowed to use the keychain. Make sure that you have setup the KeyChain in the Entitlepements.plist of your application.");
				}
			}
		}
	}
}
