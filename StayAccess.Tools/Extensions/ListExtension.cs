using System.Collections.Generic;
using System.Linq;

namespace StayAccess.Tools.Extensions
{
    public static class ListExtension
    {
        public static bool HasAny<T>(this IEnumerable<T> list)
        {
            return list != null && list.Any();
        }
    }
}
