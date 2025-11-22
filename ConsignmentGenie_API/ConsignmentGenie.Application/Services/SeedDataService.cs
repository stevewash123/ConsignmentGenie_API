using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class SeedDataService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(ConsignmentGenieContext context, ILogger<SeedDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDemoDataAsync()
    {
        _logger.LogInformation("Starting seed data creation for Winnie's Attic...");

        // Get the demo organization (should already exist)
        var organization = await _context.Organizations.FirstOrDefaultAsync();
        if (organization == null)
        {
            _logger.LogError("No organization found! Create organization first.");
            return;
        }

        // Update organization name to Winnie's Attic
        organization.Name = "Winnie's Attic";
        await _context.SaveChangesAsync();

        var orgId = organization.Id;

        // Clear existing seed data if any
        await ClearExistingDemoData(orgId);

        // Create Providers
        var providers = await CreateProviders(orgId);

        // Create Items
        var items = await CreateItems(orgId, providers);

        // Create Transactions
        var transactions = await CreateTransactions(orgId, items);

        // Create some historical payouts
        await CreateHistoricalPayouts(orgId, providers, transactions);

        _logger.LogInformation("Seed data creation completed for Winnie's Attic!");
        _logger.LogInformation($"Created: {providers.Count} providers, {items.Count} items, {transactions.Count} transactions");
    }

    private async Task ClearExistingDemoData(Guid orgId)
    {
        // Remove in correct order to handle foreign key constraints
        var existingTransactions = await _context.Transactions.Where(t => t.OrganizationId == orgId).ToListAsync();
        _context.Transactions.RemoveRange(existingTransactions);

        var existingItems = await _context.Items.Where(i => i.OrganizationId == orgId).ToListAsync();
        _context.Items.RemoveRange(existingItems);

        var existingProviders = await _context.Providers.Where(p => p.OrganizationId == orgId).ToListAsync();
        _context.Providers.RemoveRange(existingProviders);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Cleared existing demo data");
    }

    private async Task<List<Provider>> CreateProviders(Guid orgId)
    {
        var providers = new List<Provider>
        {
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Jane Doe",
                Email = "jane.doe@email.com",
                Phone = "(555) 123-4567",
                DefaultSplitPercentage = 50.00m,
                CommissionRate = 50.00m,
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "{\"venmo\": \"@jane-doe\"}",
                Status = ProviderStatus.Active,
                BusinessName = "Jane's Vintage Collection",
                Address = "123 Main St",
                City = "Springfield",
                ZipCode = "12345",
                Notes = "Specializes in vintage clothing and accessories"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Bob Smith",
                Email = "bob.smith@email.com",
                Phone = "(555) 234-5678",
                DefaultSplitPercentage = 40.00m,
                CommissionRate = 60.00m,
                PreferredPaymentMethod = "Check",
                PaymentDetails = "{\"address\": \"456 Oak Ave, Springfield 12345\"}",
                Status = ProviderStatus.Active,
                Notes = "Brings furniture and home decor items"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Maria Garcia",
                Email = "maria.garcia@email.com",
                Phone = "(555) 345-6789",
                DefaultSplitPercentage = 55.00m,
                CommissionRate = 45.00m,
                PreferredPaymentMethod = "Zelle",
                PaymentDetails = "{\"zelle\": \"maria.garcia@email.com\"}",
                Status = ProviderStatus.Active,
                BusinessName = "Maria's Designer Finds",
                Notes = "High-end designer handbags and jewelry"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Tom Johnson",
                Email = "tom.johnson@email.com",
                Phone = "(555) 456-7890",
                DefaultSplitPercentage = 50.00m,
                CommissionRate = 50.00m,
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "{\"venmo\": \"@tom-johnson\"}",
                Status = ProviderStatus.Active,
                Notes = "Mid-century modern furniture specialist"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Sarah Williams",
                Email = "sarah.williams@email.com",
                Phone = "(555) 567-8901",
                DefaultSplitPercentage = 45.00m,
                CommissionRate = 55.00m,
                PreferredPaymentMethod = "PayPal",
                PaymentDetails = "{\"paypal\": \"sarah.williams@email.com\"}",
                Status = ProviderStatus.Active,
                BusinessName = "Sarah's Jewelry Box",
                Notes = "Handmade and vintage jewelry"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Mike Brown",
                Email = "mike.brown@email.com",
                Phone = "(555) 678-9012",
                DefaultSplitPercentage = 60.00m,
                CommissionRate = 40.00m,
                PreferredPaymentMethod = "Check",
                PaymentDetails = "{\"address\": \"789 Pine St, Springfield 12345\"}",
                Status = ProviderStatus.Active,
                Notes = "Books, electronics, and collectibles"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "Lisa Davis",
                Email = "lisa.davis@email.com",
                Phone = "(555) 789-0123",
                DefaultSplitPercentage = 50.00m,
                CommissionRate = 50.00m,
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "{\"venmo\": \"@lisa-davis\"}",
                Status = ProviderStatus.Inactive,
                Notes = "Currently inactive - seasonal provider"
            },
            new Provider
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                DisplayName = "John Wilson",
                Email = "john.wilson@email.com",
                Phone = "(555) 890-1234",
                DefaultSplitPercentage = 35.00m,
                CommissionRate = 65.00m,
                PreferredPaymentMethod = "Venmo",
                PaymentDetails = "{\"venmo\": \"@john-wilson\"}",
                Status = ProviderStatus.Active,
                BusinessName = "Wilson's Art Gallery",
                Notes = "Original artwork and prints"
            }
        };

        _context.Providers.AddRange(providers);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created {providers.Count} providers");
        return providers;
    }

    private async Task<List<Item>> CreateItems(Guid orgId, List<Provider> providers)
    {
        var items = new List<Item>();
        var random = new Random(42); // Fixed seed for consistent data

        var categories = new[] { "Clothing", "Accessories", "Furniture", "Art", "Collectibles", "Electronics", "Books" };

        var itemTemplates = new[]
        {
            ("Vintage Leather Jacket", "Classic brown leather jacket from the 80s", 120.00m),
            ("Antique Table Lamp", "Brass table lamp with stained glass shade", 85.00m),
            ("Designer Handbag", "Authentic Louis Vuitton handbag", 350.00m),
            ("Mid-Century Chair", "Eames-style lounge chair, excellent condition", 450.00m),
            ("Sterling Silver Necklace", "Handcrafted silver chain with pendant", 45.00m),
            ("Vintage Art Print", "Original 1960s abstract art print, framed", 75.00m),
            ("Antique Book Set", "Complete works of Shakespeare, leather bound", 125.00m),
            ("Ceramic Vase", "Hand-painted ceramic vase, 12 inches tall", 65.00m),
            ("Vintage Watch", "Men's Omega watch, needs minor repair", 280.00m),
            ("Wooden Jewelry Box", "Hand-carved jewelry box with mirror", 55.00m),
            ("Original Oil Painting", "Small landscape painting by local artist", 180.00m),
            ("Vintage Scarf", "Hermès silk scarf, excellent condition", 95.00m),
            ("Antique Candlesticks", "Pair of brass candlesticks, 8 inches", 40.00m),
            ("Designer Shoes", "Manolo Blahnik heels, size 8, like new", 225.00m),
            ("Collectible Figurine", "Royal Doulton figurine, limited edition", 85.00m),
            ("Vintage Camera", "Canon AE-1 35mm camera with lens", 150.00m),
            ("Art Deco Mirror", "Sunburst mirror from the 1930s", 175.00m),
            ("Cashmere Sweater", "Pure cashmere sweater, barely worn", 80.00m),
            ("Antique Clock", "Mantel clock, mechanical movement", 160.00m),
            ("Pearl Earrings", "Freshwater pearl drop earrings", 55.00m),
            ("Vintage Purse", "1950s beaded evening purse", 45.00m),
            ("Crystal Decanter", "Cut crystal whiskey decanter with stopper", 70.00m),
            ("Silk Kimono", "Vintage Japanese kimono, hand-embroidered", 190.00m),
            ("Wooden Sculpture", "Abstract wooden sculpture, 10 inches", 95.00m),
            ("Vintage Brooch", "Art Nouveau style brooch with gemstones", 65.00m),
            ("Leather Boots", "Vintage cowboy boots, size 9", 85.00m),
            ("Porcelain Teapot", "Fine china teapot with matching cups", 75.00m),
            ("Vintage Record", "Rare vinyl LP, mint condition", 35.00m),
            ("Antique Frame", "Ornate gold picture frame, 16x20", 45.00m),
            ("Designer Belt", "Gucci leather belt, authentic", 125.00m),
            ("Vintage Toy", "Tin toy robot from the 1960s", 55.00m),
            ("Crystal Chandelier", "Small crystal chandelier, 5 lights", 285.00m),
            ("Vintage Cookbook", "Julia Child's first edition cookbook", 75.00m),
            ("Amber Jewelry", "Baltic amber pendant on silver chain", 85.00m),
            ("Antique Compass", "Brass nautical compass in wooden box", 95.00m),
            ("Vintage Dress", "1960s mod dress, size medium", 65.00m),
            ("Pewter Mug", "Handcrafted pewter beer mug", 35.00m),
            ("Oil Lamp", "Victorian-era oil lamp, converted to electric", 110.00m)
        };

        for (int i = 0; i < Math.Min(38, itemTemplates.Length); i++)
        {
            var template = itemTemplates[i];
            var provider = providers[random.Next(providers.Count - 1)]; // Exclude inactive provider mostly

            // Determine status - most available, some sold, few returned
            ItemStatus status;
            if (i < 25) status = ItemStatus.Available;
            else if (i < 35) status = ItemStatus.Sold;
            else status = ItemStatus.Removed;

            var item = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                ProviderId = provider.Id,
                Sku = $"WA{1000 + i}",
                Title = template.Item1,
                Description = template.Item2,
                Price = template.Item3,
                Condition = Core.Enums.ItemCondition.Good,
                Category = categories[random.Next(categories.Length)],
                Status = status,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90))
            };

            items.Add(item);
        }

        _context.Items.AddRange(items);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created {items.Count} items");
        return items;
    }

    private async Task<List<Transaction>> CreateTransactions(Guid orgId, List<Item> items)
    {
        var transactions = new List<Transaction>();
        var random = new Random(42);

        var soldItems = items.Where(i => i.Status == ItemStatus.Sold).ToList();
        var paymentMethods = new[] { "Cash", "Credit Card", "Debit Card", "Check" };

        foreach (var item in soldItems)
        {
            var provider = await _context.Providers.FirstAsync(p => p.Id == item.ProviderId);

            // Random sale date in last 60 days (weighted toward last 30)
            var daysAgo = random.NextDouble() < 0.65 ? random.Next(1, 31) : random.Next(31, 61);
            var saleDate = DateTime.UtcNow.AddDays(-daysAgo);

            // Sale price - sometimes at listing price, sometimes negotiated
            var salePrice = random.NextDouble() < 0.7 ? item.Price : item.Price * (decimal)(0.85 + random.NextDouble() * 0.1);

            // Calculate commission split
            var providerAmount = salePrice * (provider.CommissionRate / 100);
            var shopAmount = salePrice - providerAmount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                ItemId = item.Id,
                ProviderId = provider.Id,
                SalePrice = salePrice,
                SaleDate = saleDate,
                PaymentMethod = paymentMethods[random.Next(paymentMethods.Length)],
                ProviderSplitPercentage = provider.CommissionRate,
                ProviderAmount = providerAmount,
                ShopAmount = shopAmount,
                SalesTaxAmount = salePrice * 0.0875m, // 8.75% sales tax
                Notes = random.NextDouble() < 0.3 ? "Customer negotiated price" : null,
                Source = "Manual", // MVP default
                ProviderPaidOut = false // Will be set to true for some historical transactions
            };

            transactions.Add(transaction);
        }

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Created {transactions.Count} transactions");
        return transactions;
    }

    private async Task CreateHistoricalPayouts(Guid orgId, List<Provider> providers, List<Transaction> transactions)
    {
        var random = new Random(42);

        // Mark some older transactions as paid out (create payout history)
        var oldTransactions = transactions
            .Where(t => t.SaleDate < DateTime.UtcNow.AddDays(-35))
            .GroupBy(t => t.ProviderId)
            .Take(3) // Just a few providers
            .ToList();

        foreach (var providerGroup in oldTransactions)
        {
            var provider = providers.First(p => p.Id == providerGroup.Key);
            var payoutDate = DateTime.UtcNow.AddDays(-random.Next(5, 15)); // Paid 5-15 days ago

            foreach (var transaction in providerGroup)
            {
                transaction.ProviderPaidOut = true;
                transaction.ProviderPaidOutDate = payoutDate;
                transaction.PayoutMethod = provider.PreferredPaymentMethod;
                transaction.PayoutNotes = $"Monthly payout - {payoutDate:MMM yyyy}";
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Created historical payouts for {oldTransactions.Count} providers");
    }
}