var Uno;
(function (Uno) {
    var Storage;
    (function (Storage) {
        class NativeStorageFile {
            static async getBasicPropertiesAsync(guid) {
                const file = await Storage.NativeStorageItem.getFile(guid);
                var propertyString = "";
                propertyString += file.size;
                propertyString += "|";
                propertyString += file.lastModified;
                return propertyString;
            }
        }
        Storage.NativeStorageFile = NativeStorageFile;
    })(Storage = Uno.Storage || (Uno.Storage = {}));
})(Uno || (Uno = {}));
