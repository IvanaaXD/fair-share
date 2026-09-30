using FairShare.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FairShare.Application.Abstractions
{
    public interface IIdentityService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
    }  
}
