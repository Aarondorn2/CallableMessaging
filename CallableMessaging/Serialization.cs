using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Noogadev.CallableMessaging
{
    public class Serialization
    {
        public const string Delimiter = "::";
        public static readonly JsonSerializerOptions SerializerOptions = new ()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new LoggerConverter() }
        };

        /// <summary>
        /// Serializes a Callable Message to a string that includes the messages type.
        /// </summary>
        /// <param name="callable">The Callable Message to serialize.</param>
        /// <returns>string - the serialized message.</returns>
        internal static string SerializeCallable(ICallable callable)
        {
            var type = callable.GetType();
            var serialized = JsonSerializer.Serialize(callable, type, SerializerOptions);

            return $"{GetFullSerializedType(type)}{Delimiter}{serialized}";
        }

        /// <summary>
        /// Deserializes a string into a Callable Message type. The serialized string must include an appropriately
        /// formatted type and Callable object.
        /// </summary>
        /// <param name="serializedCallable">The serialized message to deserialize into a Callable.</param>
        /// <returns>ICallableMessagingBase - the deserialized Callable Message.</returns>
        internal static ICallable? DeserializeCallable(string serializedCallable)
        {
            try
            {
                var parts = serializedCallable.Split(Delimiter, 2);
                if (parts.Length != 2) return null;

                var type = Type.GetType(parts[0]);
                if (type == null) return null;

                var deserialized = JsonSerializer.Deserialize(parts[1], type, SerializerOptions);

                return deserialized is ICallable callable
                    ? callable
                    : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes a Type into a string including assembly information.
        /// When using <see cref="Type.GetType"/>, the assembly is needed if it is
        /// an assembly other than the one invoking <see cref="Type.GetType"/>.
        /// </summary>
        /// <param name="type">The Type to serialize.</param>
        /// <returns>string - The serialized Type.</returns>
        internal static string GetFullSerializedType(Type type)
            => $"{type}, {type.Assembly.GetName().Name}";
    }

    /// <summary>
    /// ILoggingCallable utilizes an ILogger? that is set by the Consume method. This Converter
    /// ensures that object does not get inadvertently serialized when messages are re-queued.
    /// </summary>
    public class LoggerConverter : JsonConverter<ILogger?>
    {
        public override ILogger? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return default;
        }

        public override void Write(Utf8JsonWriter writer, ILogger? value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }

    /// <summary>
    /// This is a custom Exception that is thrown by the CallableMessaging library when a message cannot
    /// be deserialized. It can be explicitly caught to log and take action on messages that cannot be
    /// processed by a consumer.
    /// </summary>
    public class SerializationException : Exception
    {
        public SerializationException() { }
        public SerializationException(string? message) : base(message) { }
    }
}
