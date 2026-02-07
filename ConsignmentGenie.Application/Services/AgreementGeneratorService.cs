using System.Text.RegularExpressions;
using ConsignmentGenie.Core.Entities;

namespace ConsignmentGenie.Application.Services;

public interface IAgreementGeneratorService
{
    string GetSampleTemplate();
    string GenerateAgreement(string template, Organization organization, Consignor consignor);
    List<string> ValidateTemplate(string template);
    bool IsCustomTemplate(string template);
}

public class AgreementGeneratorService : IAgreementGeneratorService
{
    private readonly HashSet<string> _validTags = new()
    {
        "SHOP_NAME", "SHOP_ADDRESS", "SHOP_PHONE", "SHOP_EMAIL",
        "CONSIGNOR_NAME", "CONSIGNOR_EMAIL", "CONSIGNOR_PHONE", "CONSIGNOR_ADDRESS",
        "COMMISSION_PERCENT", "CONSIGNOR_PERCENT", "SHOP_PERCENT",
        "CONSIGNMENT_PERIOD", "RETRIEVAL_PERIOD", "UNSOLD_POLICY",
        "AGREEMENT_DATE", "AGREEMENT_ID"
    };

    public string GetSampleTemplate()
    {
        return @"═══════════════════════════════════════════════════════════════════
  ⚠️  SAMPLE TEMPLATE - YOU MUST CUSTOMIZE THIS BEFORE USE  ⚠️

  This sample is provided for illustration only. ConsignmentGenie
  does not provide legal advice. Have an attorney review your
  agreement before use.

  [DELETE THIS BOX AFTER CUSTOMIZING]
═══════════════════════════════════════════════════════════════════

CONSIGNMENT AGREEMENT

This agreement is entered into on {{GeneratedDate}} between:

SHOP:       {{ShopName}}
            {{ShopAddress}}
            {{ShopPhone}} | {{ShopEmail}}

CONSIGNOR:  {{ConsignorName}}
            {{ConsignorAddress}}
            {{ConsignorPhone}} | {{ConsignorEmail}}

---

COMMISSION STRUCTURE

The Shop shall retain {{ShopPercent}}% of the sale price of each item sold.
The Consignor shall receive {{ConsignorPercent}}% of the sale price.

---

CONSIGNMENT PERIOD

Items will be displayed for sale for {{ConsignmentPeriodDays}} days from
the date they are accepted by the Shop.

---

UNSOLD ITEMS

Items not sold within the consignment period must be retrieved by the
Consignor within {{RetrievalPeriodDays}} days of notification.

Items not retrieved within this period will {{UnsoldItemPolicy}}.

---

[YOUR ADDITIONAL TERMS HERE - Consult with your attorney about:]
- Liability and insurance
- Pricing authority
- Item condition requirements
- Termination terms
- Dispute resolution
- [Other terms specific to your jurisdiction]

---

SIGNATURES

Shop Representative: ________________________  Date: __________

Consignor: ________________________  Date: __________";
    }

    public string GenerateAgreement(string template, Organization organization, Consignor consignor)
    {
        var agreementId = $"AGR-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999):D5}";

        var values = new Dictionary<string, string>
        {
            ["SHOP_NAME"] = organization.ShopName ?? organization.Name,
            ["SHOP_ADDRESS"] = FormatAddress(organization),
            ["SHOP_PHONE"] = organization.ShopPhone ?? "Not provided",
            ["SHOP_EMAIL"] = organization.ShopEmail ?? "Not provided",
            ["CONSIGNOR_NAME"] = $"{consignor.FirstName} {consignor.LastName}".Trim(),
            ["CONSIGNOR_EMAIL"] = consignor.Email ?? "Not provided",
            ["CONSIGNOR_PHONE"] = consignor.Phone ?? "Not provided",
            ["CONSIGNOR_ADDRESS"] = FormatConsignorAddress(consignor),
            ["CONSIGNOR_PERCENT"] = ((int)(consignor.CommissionRate * 100)).ToString(),
            ["SHOP_PERCENT"] = ((int)((1 - consignor.CommissionRate) * 100)).ToString(),
            ["COMMISSION_PERCENT"] = ((int)((1 - consignor.CommissionRate) * 100)).ToString(), // Shop's commission percentage
            ["CONSIGNMENT_PERIOD"] = "90", // TODO: Get from organization settings
            ["RETRIEVAL_PERIOD"] = "14", // TODO: Get from organization settings
            ["UNSOLD_POLICY"] = "become property of the shop", // TODO: Get from organization settings
            ["AGREEMENT_DATE"] = DateTime.Now.ToString("MMMM d, yyyy"),
            ["AGREEMENT_ID"] = agreementId
        };

        return Regex.Replace(template, @"\{\{(\w+)\}\}", match =>
        {
            var tag = match.Groups[1].Value;
            return values.TryGetValue(tag, out var value) ? value : match.Value;
        });
    }

    public List<string> ValidateTemplate(string template)
    {
        var unknownTags = new List<string>();
        var matches = Regex.Matches(template, @"\{\{(\w+)\}\}");

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value;
            if (!_validTags.Contains(tag) && !unknownTags.Contains(tag))
            {
                unknownTags.Add(tag);
            }
        }

        return unknownTags;
    }

    public bool IsCustomTemplate(string template)
    {
        return !template.Contains("SAMPLE TEMPLATE - YOU MUST CUSTOMIZE THIS BEFORE USE");
    }

    private string FormatAddress(Organization organization)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress1))
            parts.Add(organization.ShopAddress1);

        if (!string.IsNullOrWhiteSpace(organization.ShopAddress2))
            parts.Add(organization.ShopAddress2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(organization.ShopCity))
            cityStateZip.Add(organization.ShopCity);

        if (!string.IsNullOrWhiteSpace(organization.ShopState))
            cityStateZip.Add(organization.ShopState);

        if (!string.IsNullOrWhiteSpace(organization.ShopZip))
            cityStateZip.Add(organization.ShopZip);

        if (cityStateZip.Any())
            parts.Add(string.Join(", ", cityStateZip));

        return parts.Any() ? string.Join("\n            ", parts) : "Address not provided";
    }

    private string FormatConsignorAddress(Consignor consignor)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(consignor.AddressLine1))
            parts.Add(consignor.AddressLine1);

        if (!string.IsNullOrWhiteSpace(consignor.AddressLine2))
            parts.Add(consignor.AddressLine2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrWhiteSpace(consignor.City))
            cityStateZip.Add(consignor.City);

        if (!string.IsNullOrWhiteSpace(consignor.State))
            cityStateZip.Add(consignor.State);

        if (!string.IsNullOrWhiteSpace(consignor.PostalCode))
            cityStateZip.Add(consignor.PostalCode);

        if (cityStateZip.Any())
            parts.Add(string.Join(", ", cityStateZip));

        return parts.Any() ? string.Join("\n            ", parts) : "Address not provided";
    }
}