// Copyright (c) LibSharp. All rights reserved.

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace LibSharp.Common
{
    /// <summary>
    /// XML serialization extensions.
    /// </summary>
    public static class XmlSerializationExtensions
    {
        /// <summary>
        /// Deserializes an XML string to a typed object.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="xmlString">XML string.</param>
        /// <param name="xmlReaderSettings">XML reader settings.</param>
        /// <returns>Deserialized object.</returns>
        public static T DeserializeFromXml<T>(this string xmlString, XmlReaderSettings xmlReaderSettings = null)
        {
            Argument.NotNullOrWhiteSpace(xmlString, nameof(xmlString));

            StringReader stringReader = new StringReader(xmlString);
            using XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings ?? s_xmlReaderSettings);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(xmlReader);
        }

        /// <summary>
        /// Serializes an object to XML.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="objectToSerialize">Object to serialize.</param>
        /// <returns>XML string.</returns>
        public static string SerializeToXml<T>(T objectToSerialize)
        {
            Argument.NotNull(objectToSerialize, nameof(objectToSerialize));

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using StringWriter stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, objectToSerialize);
            return stringWriter.ToString();
        }

        private static readonly XmlReaderSettings s_xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit
        };
    }
}
