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
		private static tryGetValue(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_TryGetValueParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_TryGetValueReturn();

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			if (localStorage.hasOwnProperty(storageKey)) {
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
			const params = ApplicationDataContainer_SetValueParams.unmarshal(pParams);

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			localStorage.setItem(storageKey, params.Value);

			return true;
		}

		/**
		 * Determines if a key is contained in localStorage
		 * */
		private static containsKey(pParams: number, pReturn: number): boolean {
			const params = ApplicationDataContainer_ContainsKeyParams.unmarshal(pParams);
			const ret = new ApplicationDataContainer_ContainsKeyReturn();

			const storageKey = ApplicationDataContainer.buildStorageKey(params.Locality, params.Key);

			ret.ContainsKey = localStorage.hasOwnProperty(storageKey);

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

			for (let i = 0; i < localStorage.length; i++) {
				const storageKey = localStorage.key(i);

				if (storageKey.startsWith(prefix)) {

					if (localityIndex === params.Index) {
						returnKey = storageKey.substr(prefix.length);
					}

					localityIndex++;
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

			for (let i = 0; i < localStorage.length; i++) {
				const storageKey = localStorage.key(i);

				if (storageKey.startsWith(prefix)) {
					ret.Count++;
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

			for (let i = 0; i < localStorage.length; i++) {
				const storageKey = localStorage.key(i);

				if (storageKey.startsWith(prefix)) {
					itemsToRemove.push(storageKey);
				}
			}

			for (const item in itemsToRemove) {
				localStorage.removeItem(itemsToRemove[item]);
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

			ret.Removed = localStorage.hasOwnProperty(storageKey);

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

			for (let i = 0; i < localStorage.length; i++) {
				const storageKey = localStorage.key(i);

				if (storageKey.startsWith(prefix)) {

					if (localityIndex === params.Index) {
						returnKey = localStorage.getItem(storageKey);
					}

					localityIndex++;
				}
			}

			ret.Value = returnKey;

			ret.marshal(pReturn);

			return true;
		}

	}
}
