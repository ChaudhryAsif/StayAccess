using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StayAccess.Tools.Extensions
{
    public static class ObjectExtensions
    {
        public static T DeepClone<T>(this T obj)
        {
            byte[] objectByteArray = ObjectToByteArray(obj);
            return ByteArrayToObject<T>(objectByteArray);
        }

        /// <summary>
        /// Convert an object to a Byte Array.
        /// </summary>
        private static byte[] ObjectToByteArray(object objData)
        {
            if (objData == null)
                return default;

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(objData, GetJsonSerializerOptions()));
        }

        /// <summary>
        /// Convert a byte array to an Object of T.
        /// </summary>
        private static T ByteArrayToObject<T>(byte[] byteArray)
        {
            if (byteArray == null || !byteArray.Any())
                return default;

            return JsonSerializer.Deserialize<T>(byteArray, GetJsonSerializerOptions());
        }

        /// <summary>
        /// json serializer options setting
        /// </summary>
        /// <returns></returns>
        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null,
                WriteIndented = true,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = false,
                ReferenceHandler = ReferenceHandler.Preserve,
            };
        }
    }
}
