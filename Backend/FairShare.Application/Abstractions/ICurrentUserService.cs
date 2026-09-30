using System;
using System.Collections.Generic;
using System.Text;

namespace FairShare.Application.Abstractions
{
    public interface ICurrentUserService
    {
        string? Username { get; }
        Guid UserId { get; }
    }
}
