using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ConsignmentGenie.Application.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly ConsignmentGenieContext _context;

        public OrganizationService(ConsignmentGenieContext context)
        {
            _context = context;
        }

        public async Task<Guid?> GetIdBySlugAsync(string slug)
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == slug);

            return organization?.Id;
        }
    }
}