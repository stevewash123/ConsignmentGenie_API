using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ConsignmentGenie.API.Attributes;

public class RequiresTierAttribute : Attribute, IAuthorizationFilter
{
    private readonly SubscriptionTier _requiredTier;

    public RequiresTierAttribute(SubscriptionTier requiredTier)
    {
        _requiredTier = requiredTier;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip authorization if not authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        // Get organization ID from claims
        var organizationIdClaim = context.HttpContext.User.FindFirst("OrganizationId")?.Value;
        if (!Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get database context from DI
        var dbContext = context.HttpContext.RequestServices.GetService(typeof(ConsignmentGenieContext)) as ConsignmentGenieContext;
        if (dbContext == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check organization subscription tier
        var organization = dbContext.Organizations.FirstOrDefault(o => o.Id == organizationId);
        if (organization == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if subscription is active (allow trial)
        if (organization.SubscriptionStatus != SubscriptionStatus.Active &&
            organization.SubscriptionStatus != SubscriptionStatus.Trial)
        {
            context.Result = new ObjectResult(new { error = "Active subscription required", upgradeRequired = true })
            {
                StatusCode = 402  // Payment Required
            };
            return;
        }

        // Check tier requirement
        if (!HasRequiredTier(organization.SubscriptionTier, _requiredTier))
        {
            context.Result = new ObjectResult(new
            {
                error = $"This feature requires {_requiredTier} tier or higher",
                currentTier = organization.SubscriptionTier.ToString(),
                requiredTier = _requiredTier.ToString(),
                upgradeRequired = true
            })
            {
                StatusCode = 402  // Payment Required
            };
            return;
        }
    }

    private bool HasRequiredTier(SubscriptionTier currentTier, SubscriptionTier requiredTier)
    {
        var tierHierarchy = new Dictionary<SubscriptionTier, int>
        {
            { SubscriptionTier.Basic, 1 },
            { SubscriptionTier.Pro, 2 },
            { SubscriptionTier.Enterprise, 3 }
        };

        return tierHierarchy.GetValueOrDefault(currentTier) >= tierHierarchy.GetValueOrDefault(requiredTier);
    }
}