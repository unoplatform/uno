// eslint-disable-next-line @typescript-eslint/no-namespace
namespace Windows.Storage {

	export class ApplicationDataContainer {

		private static buildStorageKey(locality: string, key: string): string {
			return `UnoApplicationDataContainer_${locality}_${key}`;
		}
		private static buildStoragePrefix(locality: string): string {
			return `UnoApplicationDataContainer_${locality}_`;
		}

		// https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API/Using_the_Web_Storage_API#feature-detecting_localstorage
		private static isLocalStorageAvailable() {
			let storage: Storage;
			try {
				storage = window['localStorage'];
				const x = "__storage_test__";
				storage.setItem(x, x);
				storage.removeItem(x);
				return true;
			} catch (e) {
				return (
					e instanceof DOMException &&
					// everything except Firefox
					(e.code === 22 ||
						// Firefox
						e.code === 1014 ||
						// test name field too, because code might not be present
						// everything except Firefox
						e.name === "QuotaExceededError" ||
						// Firefox
						e.name === "NS_ERROR_DOM_QUOTA_REACHED") &&
					// acknowledge QuotaExceededError only if there's something already stored
					storage &&
					storage.length !== 0
				);
			}
		}

		/**
		 * Try to get a value from localStorage
		 * */
		private static tryGetValue(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_TryGetValueParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_TryGetValueReturn();

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			if (ApplicationDataContainer.isLocalStorageAvailable() && localStorage.hasOwnProperty(storageKey)) {
				ret.HasValue = true;
				ret.Value = localStorage.getItem(storageKey);
			} else {
				ret.Value = "";
				ret.HasValue = false;
			}

			ret.marshal(pReturn);

			return true;
		}

		/**
		 * Set a value to localStorage
		 * */
		private static setValue(pParams: number): boolean {
			if (ApplicationDataContainer.isLocalStorageAvailable()) {
				const params = ApplicationDataContainer_SetValueParams.unmarshal(pParams);

				const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

				localStorage.setItem(storageKey, params.Value);
			}

			return true;
		}

		/**
		 * Determines if a key is contained in localStorage
		 * */
		private static containsKey(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_ContainsKeyParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_ContainsKeyReturn();

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			ret.ContainsKey = ApplicationDataContainer.isLocalStorageAvailable() && localStorage.hasOwnProperty(storageKey);

			ret.marshal(pReturn);

			return true;
		}

		/**
		 * Gets a key by index in localStorage
		 * */
		private static getKeyByIndex(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_GetKeyByIndexParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_GetKeyByIndexReturn();

			let localityIndex = 0;
			let returnKey = "";
			const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);

			if (ApplicationDataContainer.isLocalStorageAvailable()) {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {

						if (localityIndex === params.Index) {
							returnKey = storageKey.substr(prefix.length);
						}

						localityIndex++;
					}
				}
			}

			ret.Value = returnKey;

			ret.marshal(pReturn);

			return true;
		}

		/**
		 * Determines the number of items contained in localStorage
		 * */
		private static getCount(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_GetCountParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_GetCountReturn();

			ret.Count = 0;
			const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);

			if (ApplicationDataContainer.isLocalStorageAvailable()) {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {
						ret.Count++;
					}
				}
			}

			ret.marshal(pReturn);

			return true;
		}

		/**
		 * Clears items contained in localStorage
		 * */
		private static clear(pParams: number): boolean {
			const params = ApplicationDataContainer_ClearParams.unmarshal(pParams);

			const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);

			const itemsToRemove: string[] = [];

			if (ApplicationDataContainer.isLocalStorageAvailable()) {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {
						itemsToRemove.push(storageKey);
					}
				}

				for (const item in itemsToRemove) {
					localStorage.removeItem(itemsToRemove[item]);
				}
			}

			return true;
		}

		/**
		 * Removes an item contained in localStorage
		 * */
		private static remove(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_RemoveParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_RemoveReturn();

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			ret.Removed = ApplicationDataContainer.isLocalStorageAvailable() && localStorage.hasOwnProperty(storageKey);

			if (ret.Removed) {
				localStorage.removeItem(storageKey);
			}

			ret.marshal(pReturn);

			return true;
		}

		/**
		 * Gets a key by index in localStorage
		 * */
		private static getValueByIndex(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_GetValueByIndexParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_GetKeyByIndexReturn();

			let localityIndex = 0;
			let returnKey = "";
			const prefix = ApplicationDataContainer.buildStoragePrefix(params.Locality);

			if (ApplicationDataContainer.isLocalStorageAvailable()) {
				for (let i = 0; i < localStorage.length; i++) {
					const storageKey = localStorage.key(i);

					if (storageKey.startsWith(prefix)) {

						if (localityIndex === params.Index) {
							returnKey = localStorage.getItem(storageKey);
						}

						localityIndex++;
					}
				}
			}

			ret.Value = returnKey;

			ret.marshal(pReturn);

			return true;
		}

	}
}
