using System;

namespace ChatCore.Models.OAuth
{
    public class OAuthCredentials
    {
        public string AccessToken;
        public string RefreshToken;
        public DateTime ExpiresAt;
    }
}
