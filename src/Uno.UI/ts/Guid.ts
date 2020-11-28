namespace Uno.Utils {
	export class Guid {
		public static NewGuid(): string {
						if (!Guid.newGuidMethod) {
				Guid.newGuidMethod = (<any>Module).mono_bind_static_method("[mscorlib] System.Guid:NewGuid");
			}

      return Guid.newGuidMethod();
		}
	}
}
