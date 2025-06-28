using Models;
using System;
using System.Runtime.Caching;
using System.Runtime.Intrinsics.X86;
using System.Security;

namespace Cache
{
    public class CacheEx
    {
        private static readonly MemoryCache TokenCache = new MemoryCache("TokenCache");
        public static DateTime TimeEx;
        public static User DataUser { get; set; }
        public static string SetTokenEx(User mod)
        {
            //CleanUpTokens(mod);
            string token = Guid.NewGuid().ToString();
            DateTime expiry = DateTime.Now.AddMinutes(60);
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = new DateTimeOffset(expiry)
            };
            if (mod is User)
            {
                TokenCache.Set(token, mod, policy);
                Console.WriteLine("Add Token Succ");
            }

            return token;
        }

        public static bool CheckTokenEx(string token)
        {

            var storedValue = (User)TokenCache.Get(token);
            DataUser = (User)storedValue;
            return storedValue is User;
        }

        public static bool CleanUpTokens(string token)
        {
            object removedItem = TokenCache.Remove(token);
            return removedItem != null;

        }

        public static User GetUserFromToken(string token)
        {
            foreach (var item in TokenCache)
            {
                    Console.WriteLine($"Key: {item.Key}, Value Type: {item.Value.GetType().Name}");
            }
            var storedValue = TokenCache.Get(token);
            return storedValue as User;
        }
    }

}
