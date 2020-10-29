using System;

namespace ChatCore.Models.OAuth
{
    public class OAuthCredentials
    {
        public string AccessToken = null!;
        public string RefreshToken = null!;
        public DateTime ExpiresAt;
    }
}
