// Copyright (c) LibSharp. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace LibSharp.Common;

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

        using StringReader stringReader = new StringReader(xmlString);
        using XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings ?? s_xmlReaderSettings);
        return (T)GetSerializer(typeof(T)).Deserialize(xmlReader);
    }

    /// <summary>
    /// Serializes an object to XML.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="objectToSerialize">Object to serialize.</param>
    /// <returns>XML string.</returns>
    public static string SerializeToXml<T>(this T objectToSerialize)
    {
        Argument.NotNull(objectToSerialize, nameof(objectToSerialize));

        using StringWriter stringWriter = new StringWriter();
        GetSerializer(typeof(T)).Serialize(stringWriter, objectToSerialize);
        return stringWriter.ToString();
    }

    private static XmlSerializer GetSerializer(Type type)
    {
        return s_serializerCache.GetOrAdd(type, static t => new XmlSerializer(t));
    }

    private static readonly XmlReaderSettings s_xmlReaderSettings = new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit
    };

    private static readonly ConcurrentDictionary<Type, XmlSerializer> s_serializerCache = new ConcurrentDictionary<Type, XmlSerializer>();
}
