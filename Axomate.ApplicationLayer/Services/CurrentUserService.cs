using Axomate.ApplicationLayer.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axomate.ApplicationLayer.Services
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        public string? UserName => Environment.UserName; // or read from your app’s login
    }
}
