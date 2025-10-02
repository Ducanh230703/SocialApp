using Models;
using System;
using System.Runtime.Caching;
using System.Security;

namespace Cache
{
    public class CacheEx
    {
        private static readonly MemoryCache TokenCache = new MemoryCache("TokenCache");
        private static readonly MemoryCache OtpCache = new MemoryCache("OtpCache");

        public static DateTime TimeEx;
        public static User DataUser { get; set; }

        public static string SetTokenEx(User mod)
        {
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

        // PHƯƠNG THỨC MỚI CHO OTP
        public static bool SetOtp(string email, string otpCode, TimeSpan duration)
        {
            DateTime expiry = DateTime.Now.Add(duration);
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = new DateTimeOffset(expiry)
            };

            // SỬA LỖI: Dùng Set thay vì AddOrGetExisting để luôn ghi đè OTP cũ
            try
            {
                OtpCache.Set(email, otpCode, policy);
                return true; // Trả về true nếu Set thành công
            }
            catch
            {
                return false; // Trả về false nếu có lỗi khi Set
            }
        }

        // PHƯƠNG THỨC MỚI CHO OTP
        public static string GetOtp(string email)
        {
            var storedOtp = OtpCache.Get(email);
            if (storedOtp is string otp)
            {
                OtpCache.Remove(email);
                return otp;
            }
            return null;
        }
        public static bool CleanUpOtp(string email)
        {
            object removedItem = OtpCache.Remove(email);
            return removedItem != null;
        }
    }
}