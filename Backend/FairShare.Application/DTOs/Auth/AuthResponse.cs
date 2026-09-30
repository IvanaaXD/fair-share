using System;
using System.Collections.Generic;
using System.Text;

namespace FairShare.Application.DTOs.Auth
{
    public class AuthResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string RefreshToken { get; set; }
        public string AccessToken { get; set; }
    }
}
