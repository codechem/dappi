using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CCApi.Extensions.DependencyInjection.Models;
using Microsoft.AspNetCore.Http;

namespace CCApi.Extensions.DependencyInjection.Interfaces
{
    public interface IDbContextAccessor
    {
        DbContext DbContext { get; }
    }
}