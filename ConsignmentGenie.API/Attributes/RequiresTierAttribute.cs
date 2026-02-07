using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace ConsignmentGenie.API.Attributes;

public class RequiresTierAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _featureName;

    public RequiresTierAttribute(SubscriptionTier requiredTier)
    {
        // Map tier to feature name for compatibility
        _featureName = requiredTier switch
        {
            SubscriptionTier.Basic => "base_features",
            SubscriptionTier.Pro => "pro_features",
            SubscriptionTier.Enterprise => "enterprise_features",
            _ => "base_features"
        };
    }

    public RequiresTierAttribute(string featureName)
    {
        _featureName = featureName;
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

        // Get feature gating service from DI
        var featureGatingService = context.HttpContext.RequestServices.GetService<FeatureGatingService>();
        if (featureGatingService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Check feature access
        var hasAccess = featureGatingService.HasAccessAsync(organizationId, _featureName).Result;
        if (!hasAccess)
        {
            context.Result = new ObjectResult(new
            {
                error = $"This feature requires an active subscription with {_featureName}",
                featureName = _featureName,
                upgradeRequired = true
            })
            {
                StatusCode = 402  // Payment Required
            };
            return;
        }
    }
}