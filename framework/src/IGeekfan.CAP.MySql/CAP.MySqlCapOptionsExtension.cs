﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP
{
    internal class MySqlCapOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<MySqlOptions> _configure;

        public MySqlCapOptionsExtension(Action<MySqlOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapStorageMarkerService>();
            services.AddSingleton<IDataStorage, MySqlDataStorage>();
            
            services.TryAddSingleton<IStorageInitializer, MySqlStorageInitializer>();
            services.AddTransient<ICapTransaction, MySqlCapTransaction>();

            //Add MySqlOptions
            services.Configure(_configure);
        } 
    }
}