﻿using ChatCore.Models;
using ChatCore.Models.OAuth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Interfaces
{
    public interface IUserAuthProvider
    {
        event Action<LoginCredentials> OnCredentialsUpdated;
        LoginCredentials Credentials { get; }
        void Save(bool callback = true);
    }
}
