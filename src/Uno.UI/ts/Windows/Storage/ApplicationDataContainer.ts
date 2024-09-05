// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Windows.Storage {

	export class ApplicationDataContainer {

		private static buildStorageKey(locality: string, key: string): string {
			return `UnoApplicationDataContainer_${locality}_${key}`;
		}
		private static buildStoragePrefix(locality: string): string {
			return `UnoApplicationDataContainer_${locality}_`;
		}

		/**
		 * Try to get a value from localStorage
		 * */
		private static getValue(locality: string, key: string): string {
			const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);

			if (localStorage.hasOwnProperty(storageKey)) {
				return localStorage.getItem(storageKey);
			} else {
				throw `ApplicationDataContainer.getValue failed for ${storageKey}`;
			}
		}

		/**
		 * Set a value to localStorage
		 * */
		private static setValue(locality: string, key: string, value: string): boolean {
			try {
				const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);

				localStorage.setItem(storageKey, value);
			} catch (e) {
				console.debug(`ApplicationDataContainer.setValue failed: ${e}`);
			}

			return true;
		}

		/**
		 * Determines if a key is contained in localStorage
		 * */
		private static containsKey(locality: string, key: string): boolean {
			const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);

			try {
				return localStorage.hasOwnProperty(storageKey);
			} catch (e) {
				throw `ApplicationDataContainer.containsKey failed: ${e}`;
			}
		}

		/**
		 * Gets a key by index in localStorage
		 * */
		private static getKeyByIndex(locality: string, index: number): string {
			let localityIndex = 0;
			let returnKey = "";
			const prefix = ApplicationDataContainer.buildStoragePrefix(locality);

			try {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {

						if (localityIndex === index) {
							return storageKey.substr(prefix.length);
						}

						localityIndex++;
					}
				}
			} catch (e) {
				throw `ApplicationDataContainer.getKeyByIndex failed: ${e}`;
			}
		}

		/**
		 * Determines the number of items contained in localStorage
		 * */
		private static getCount(locality: string): number {
			let count = 0;
			const prefix = ApplicationDataContainer.buildStoragePrefix(locality);

			try {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {
						count++;
					}
				}
			} catch (e) {
				console.debug(`ApplicationDataContainer.getCount failed: ${e}`);
			}

			return count;
		}

		/**
		 * Clears items contained in localStorage
		 * */
		private static clear(locality: string) {
			const prefix = ApplicationDataContainer.buildStoragePrefix(locality);

			const itemsToRemove: string[] = [];

			try {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {
						itemsToRemove.push(storageKey);
					}
				}

				for (const item in itemsToRemove) {
					localStorage.removeItem(itemsToRemove[item]);
				}
			} catch (e) {
				throw `ApplicationDataContainer.clear failed: ${e}`;
			}
		}

		/**
		 * Removes an item contained in localStorage
		 * */
		private static remove(locality: string, key: string): boolean {
			const storageKey = ApplicationDataContainer.buildStorageKey(locality, key);

			let remove = false;

			try {
				remove = localStorage.hasOwnProperty(storageKey);
			} catch (e) {
				remove = false;
				console.debug(`ApplicationDataContainer.remove failed: ${e}`);
			}

			if (remove) {
				localStorage.removeItem(storageKey);
			}

			return remove;
		}

		/**
		 * Gets a key by index in localStorage
		 * */
		private static getValueByIndex(locality: string, index: number): string {
			let localityIndex = 0;
			let returnKey = "";
			const prefix = ApplicationDataContainer.buildStoragePrefix(locality);

			try {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {

						if (localityIndex === index) {
							return localStorage.getItem(storageKey);
						}

						localityIndex++;
					}
				}
			} catch (e) {
				throw `ApplicationDataContainer.getValueByIndex failed: ${e}`;
			}
		}
	}
}
