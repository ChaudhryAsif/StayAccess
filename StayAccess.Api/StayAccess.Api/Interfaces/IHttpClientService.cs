using System.Net.Http;
using System.Threading.Tasks;

namespace StayAccess.Api.Interfaces
{
    public interface IHttpClientService
    {
        /// <summary>
        /// Sends a POST request to the API as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="path"></param>
        /// <param name="requireToken"></param>
        /// <param name="baseUrl"></param>
        /// <returns><seealso cref="HttpResponseMessage"/></returns>
        Task<HttpResponseMessage> PostAsync<T>(T model, string path, bool requireToken = true, string baseUrl = "") where T : class;

        /// <summary>
        /// Deserializes the JSON to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedString"></param>
        /// <returns>The deserialized object from the serialized json string</returns>
        T Deserialize<T>(string serializedString);

        /// <summary>
        /// Serializes the specified object to a JSON string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns>A JSON string representation of the object</returns>
        string Serialize<T>(T model);
    }
}
