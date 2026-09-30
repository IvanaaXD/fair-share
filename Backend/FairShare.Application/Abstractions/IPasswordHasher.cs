using System;
using System.Collections.Generic;
using System.Text;

namespace FairShare.Application.Abstractions
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }  
}
