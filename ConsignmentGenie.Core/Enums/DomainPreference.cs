namespace ConsignmentGenie.Core.Enums;

public enum DomainPreference
{
    ConsignmentGenieSubdomain = 0,  // We host at *.consignmentgenie.com
    OwnDomain = 1,                  // They have a domain, configure later
    ExternalPlatform = 2            // Square/Shopify is their website
}