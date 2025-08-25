using Newtonsoft.Json;

namespace StayAccess.Tools.Extensions
{
    public static class JsonExtension
    {
        public static string ToJsonString<T>(this T model)
        {
            return JsonConvert.SerializeObject(model);
        }
    }
}
