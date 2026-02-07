using System;
using System.Threading.Tasks;

namespace ConsignmentGenie.Core.Interfaces
{
    public interface IOrganizationService
    {
        Task<Guid?> GetIdBySlugAsync(string slug);
    }
}