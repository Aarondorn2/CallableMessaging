using System;
using System.Text.Json;

namespace Noogadev.CallableMessaging
{
    public class Serialization
    {
        public const string Delimiter = "::";
        public static readonly JsonSerializerOptions SerializerOptions = new ()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializes a Callable Message to a string that includes the messages type.
        /// </summary>
        /// <param name="callable">The Callable Message to serialize.</param>
        /// <returns>string - the serialized message.</returns>
        internal static string SerializeCallable(ICallableMessagingBase callable)
        {
            var type = callable.GetType();
            var serialized = JsonSerializer.Serialize(callable, type, SerializerOptions);

            var assembly = type.Assembly.GetName().Name;
            return $"{type}, {assembly}{Delimiter}{serialized}";
        }

        /// <summary>
        /// Deserializes a string into a Callable Message type. The serialized string must include an appropriately
        /// formatted type and Callable object.
        /// </summary>
        /// <param name="serializedCallable">The serialized message to deserialize into a Callable.</param>
        /// <returns>ICallableMessagingBase - the deserialized Callable Message.</returns>
        internal static ICallableMessagingBase? DeserializeCallable(string serializedCallable)
        {
            try
            {
                var parts = serializedCallable.Split(Delimiter, 2);
                if (parts.Length != 2) return null;

                var type = Type.GetType(parts[0]);
                if (type == null) return null;

                var deserialized = JsonSerializer.Deserialize(parts[1], type, SerializerOptions);

                return deserialized is ICallableMessagingBase callable
                    ? callable
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// This is a custom Exception that is thrown by the CallableMessaging library when a message cannot
    /// be deserialized. It can be expicitly caught to log and take action on messages that cannot be
    /// processed by a consumer.
    /// </summary>
    public class SerializationException : Exception
    {
        public SerializationException() { }
        public SerializationException(string? message) : base(message) { }
    }
}
