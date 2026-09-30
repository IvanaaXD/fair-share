using System;
using System.Collections.Generic;
using System.Text;

namespace FairShare.Application.DTOs.Auth
{
    public class AuthResult
    {
        public int StatusCode { get; set; }
        public AuthResponse Data { get; set; }
    }
}
