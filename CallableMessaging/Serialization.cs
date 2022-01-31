using System;
using System.Text.Json;

namespace Noogadev.CallableMessaging
{
    internal class Serialization
    {
        private const string Delimiter = "::";
        private static readonly JsonSerializerOptions SerializerOptions = new ()
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        internal static string SerializeCallable(ICallable callable)
        {
            var type = callable.GetType();
            var serialized = JsonSerializer.Serialize(callable, type, SerializerOptions);

            return $"{type}{Delimiter}{serialized}";
        }

        internal static CallableMessagingBase? DeserializeCallable(string serializedCallable)
        {
            try
            {
                var parts = serializedCallable.Split(Delimiter, 2);
                if (parts.Length != 2) return null;

                var type = Type.GetType(parts[0]);
                if (type == null) return null;

                var deserialized = JsonSerializer.Deserialize(parts[1], type, SerializerOptions);

                return deserialized is CallableMessagingBase callable
                    ? callable
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class SerializationException : Exception
    {
        public SerializationException() { }
        public SerializationException(string? message) : base(message) { }
    }
}
