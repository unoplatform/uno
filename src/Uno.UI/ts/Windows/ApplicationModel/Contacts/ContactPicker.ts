enum ContactProperty {
	Address = "address",
	Email = "email",
	Icon = "icon",
	Name = "name",
	Tel = "tel"
};

declare class ContactInfo {
	address: PaymentAddress[];
	email: string[];
	name: string;
	tel: string;
}

declare class ContactsManager {
	select(props: ContactProperty[], options: any): Promise<ContactInfo[]>;
}

interface Navigator {
	contacts: ContactsManager
}

namespace Windows.ApplicationModel.Contacts {

	export class ContactPicker {

		public static isSupported(): boolean {
			return 'contacts' in navigator && 'ContactsManager' in window;
		}

		public static async pickContacts(pickMultiple: boolean): Promise<string> {
			const props = [ContactProperty.Name, ContactProperty.Email, ContactProperty.Tel, ContactProperty.Address];
			const opts = {
				multiple: pickMultiple
			};

			try {
				const contacts = await navigator.contacts.select(props, opts);
				return JSON.stringify(contacts);
			} catch (ex) {
				console.log("Error occurred while picking contacts.");
				return null;
			}
		}

	}

}
