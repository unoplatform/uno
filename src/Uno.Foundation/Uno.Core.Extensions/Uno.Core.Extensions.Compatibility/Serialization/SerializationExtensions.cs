// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Uno.Serialization;
#if !SILVERLIGHT && !WINDOWS_UWP && !XAMARIN
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace Uno.Extensions
{
    public static class SerializationExtensions
    {
        public static SerializationExtensionPoint<T> Serialization<T>(this T value)
        {
            return new SerializationExtensionPoint<T>(value);
        }

        public static SerializationExtensionPoint<T> Serialization<T>(this Type type)
        {
            return new SerializationExtensionPoint<T>(type);
        }

        public static T DataContract<T>(this SerializationExtensionPoint<T> extensionPoint)
        {
            var serializer = new DataContractSerializer(extensionPoint.ExtendedType);

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, extensionPoint.ExtendedValue);

                stream.Position = 0;

                var newInstance = (T) serializer.ReadObject(stream);

                return newInstance;
            }
        }

#if !SILVERLIGHT && !WINDOWS_UWP && !XAMARIN
        public static T Binary<T>(this SerializationExtensionPoint<T> extensionPoint)
        {
            var serializer = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, extensionPoint.ExtendedValue);

                stream.Position = 0;

                var newInstance = (T) serializer.Deserialize(stream);

                return newInstance;
            }
        }

        public static void Binary<T>(this SerializationExtensionPoint<T> extensionPoint, Stream output)
        {
            if (extensionPoint.ExtendedValue != null)
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(output, extensionPoint.ExtendedValue);
                output.Flush();
            }
        }

        public static T Binary<T>(this SerializationExtensionPoint<T> extensionPoint, byte[] data)
        {
            var serializer = new BinaryFormatter();
            using (var stream = new MemoryStream(data))
            {
                return (T)serializer.Deserialize(stream);
            }
        }
#endif
        public static T Xml<T>(this SerializationExtensionPoint<T> extensionPoint)
        {
            using (var stream = new MemoryStream())
            {
                Xml(extensionPoint, stream, extensionPoint.ExtendedValue);

                stream.Position = 0;

                return Xml(extensionPoint, stream);
            }
        }

#if !WINDOWS_UWP && !XAMARIN
        public static T Xml<T>(this SerializationExtensionPoint<T> extensionPoint, string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return Xml(extensionPoint, stream);
            }
        }

        public static void Xml<T>(this SerializationExtensionPoint<T> extensionPoint, string path, T value)
        {
            using (var stream = File.OpenWrite(path))
            {
                Xml(extensionPoint, stream, value);
            }
        }
#endif

        public static T Xml<T>(this SerializationExtensionPoint<T> extensionPoint, Stream stream)
        {
            var serializer = new XmlSerializer(extensionPoint.ExtendedType);

            return (T) serializer.Deserialize(stream);
        }

        public static void Xml<T>(this SerializationExtensionPoint<T> extensionPoint, Stream stream, T value)
        {
            var serializer = new XmlSerializer(extensionPoint.ExtendedType);

            serializer.Serialize(stream, value);
        }
    }
}