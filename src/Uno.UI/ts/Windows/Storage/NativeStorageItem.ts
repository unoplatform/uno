namespace Uno.Storage {
	export class NativeStorageItem {
		private static generateGuidBinding: () => string;

		public static generateGuid(): string {
			if (!NativeStorageItem.generateGuidBinding) {
				NativeStorageItem.generateGuidBinding = (<any>Module).mono_bind_static_method("[Uno] Uno.Storage.NativeStorageItem:GenerateGuid");
			}

			return NativeStorageItem.generateGuidBinding();
		}
	}
}
