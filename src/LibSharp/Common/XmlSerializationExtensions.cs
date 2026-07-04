// Copyright (c) 2026 Danylo Fitel

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace LibSharp.Common;

/// <summary>
/// XML serialization extensions.
/// </summary>
/// <remarks>
/// These methods rely on <see cref="XmlSerializer"/>, which uses runtime reflection and dynamic
/// code generation. They are therefore incompatible with assembly trimming and Native AOT.
/// </remarks>
public static class XmlSerializationExtensions
{
    private const string XmlSerializerRequiresUnreferencedCodeMessage =
        "XmlSerializer uses reflection over the members of T, which may be removed by trimming.";

    private const string XmlSerializerRequiresDynamicCodeMessage =
        "XmlSerializer generates serialization code at runtime, which is not supported by Native AOT.";

    /// <summary>
    /// Deserializes an XML string to a typed object.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="xmlString">XML string.</param>
    /// <param name="xmlReaderSettings">XML reader settings.</param>
    /// <returns>Deserialized object.</returns>
    [RequiresUnreferencedCode(XmlSerializerRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(XmlSerializerRequiresDynamicCodeMessage)]
    public static T DeserializeFromXml<T>(this string xmlString, XmlReaderSettings? xmlReaderSettings = null)
    {
        Argument.NotNullOrWhiteSpace(xmlString);

        using StringReader stringReader = new StringReader(xmlString);
        using XmlReader xmlReader = XmlReader.Create(stringReader, xmlReaderSettings ?? s_xmlReaderSettings);

        // XmlSerializer.Deserialize is typed as object? but only yields null for content that cannot
        // represent T (e.g. an empty document); the cast then surfaces that as the caller's error.
        return (T)GetSerializer(typeof(T)).Deserialize(xmlReader)!;
    }

    /// <summary>
    /// Serializes an object to XML.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <param name="objectToSerialize">Object to serialize.</param>
    /// <returns>XML string.</returns>
    [RequiresUnreferencedCode(XmlSerializerRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(XmlSerializerRequiresDynamicCodeMessage)]
    public static string SerializeToXml<T>(this T objectToSerialize)
    {
        if (objectToSerialize is null)
        {
            throw new ArgumentNullException(nameof(objectToSerialize));
        }

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
