using ConsignmentGenie.Core.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services.QuickBooks;

public class QuickBooksApiService : IQuickBooksApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<QuickBooksApiService> _logger;
    private readonly QBApiConfig _config;

    public QuickBooksApiService(HttpClient httpClient, ILogger<QuickBooksApiService> logger, QBApiConfig config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    public async Task<QBSyncResult> CreateVendor(QBVendor vendor, string companyId)
    {
        try
        {
            var requestBody = new
            {
                Name = vendor.DisplayName,
                CompanyName = vendor.DisplayName,
                BillAddr = vendor.BillAddr != null ? new
                {
                    Line1 = vendor.BillAddr.Line1,
                    Line2 = vendor.BillAddr.Line2,
                    City = vendor.BillAddr.City,
                    CountrySubDivisionCode = vendor.BillAddr.CountrySubDivisionCode,
                    PostalCode = vendor.BillAddr.PostalCode,
                    Country = vendor.BillAddr.Country
                } : null,
                PrimaryEmailAddr = !string.IsNullOrEmpty(vendor.Email) ? new { Address = vendor.Email } : null,
                PrimaryPhone = !string.IsNullOrEmpty(vendor.Phone) ? new { FreeFormNumber = vendor.Phone } : null
            };

            var response = await PostToQB($"v3/company/{companyId}/vendor", requestBody);

            if (response.Success && response.Data != null)
            {
                var vendorData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                var qbId = vendorData.GetProperty("QueryResponse")
                    .GetProperty("Vendor")[0]
                    .GetProperty("Id").GetString();

                return new QBSyncResult { Success = true, QBId = qbId };
            }

            return new QBSyncResult { Success = false, ErrorMessage = response.ErrorMessage ?? "Unknown error creating vendor" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor in QuickBooks");
            return new QBSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<QBSyncResult> CreateBill(QBBill bill, string companyId)
    {
        try
        {
            var requestBody = new
            {
                VendorRef = new { value = bill.VendorRef },
                TxnDate = bill.TxnDate.ToString("yyyy-MM-dd"),
                TotalAmt = bill.TotalAmt,
                DocNumber = bill.DocNumber,
                PrivateNote = bill.PrivateNote,
                Line = bill.Lines.Select((line, index) => new
                {
                    Id = (index + 1).ToString(),
                    Amount = line.Amount,
                    DetailType = "AccountBasedExpenseLineDetail",
                    AccountBasedExpenseLineDetail = new
                    {
                        AccountRef = new { value = "66" }, // Default expense account
                        BillableStatus = "NotBillable"
                    },
                    Description = line.Description
                }).ToArray()
            };

            var response = await PostToQB($"v3/company/{companyId}/bill", requestBody);

            if (response.Success && response.Data != null)
            {
                var billData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                var qbId = billData.GetProperty("QueryResponse")
                    .GetProperty("Bill")[0]
                    .GetProperty("Id").GetString();

                return new QBSyncResult { Success = true, QBId = qbId };
            }

            return new QBSyncResult { Success = false, ErrorMessage = response.ErrorMessage ?? "Unknown error creating bill" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bill in QuickBooks");
            return new QBSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<QBSyncResult> CreateBillPayment(QBBillPayment payment, string companyId)
    {
        try
        {
            var requestBody = new
            {
                VendorRef = new { value = payment.VendorRef },
                TxnDate = payment.TxnDate.ToString("yyyy-MM-dd"),
                TotalAmt = payment.TotalAmt,
                CheckNum = payment.CheckNum,
                PayType = "Check",
                Line = payment.LinkedTxns.Select(txn => new
                {
                    Amount = payment.TotalAmt,
                    LinkedTxn = new[]
                    {
                        new
                        {
                            TxnId = txn.TxnId,
                            TxnType = txn.TxnType,
                            TxnLineId = txn.TxnLineId
                        }
                    }
                }).ToArray()
            };

            var response = await PostToQB($"v3/company/{companyId}/billpayment", requestBody);

            if (response.Success && response.Data != null)
            {
                var paymentData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                var qbId = paymentData.GetProperty("QueryResponse")
                    .GetProperty("BillPayment")[0]
                    .GetProperty("Id").GetString();

                return new QBSyncResult { Success = true, QBId = qbId };
            }

            return new QBSyncResult { Success = false, ErrorMessage = response.ErrorMessage ?? "Unknown error creating bill payment" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bill payment in QuickBooks");
            return new QBSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<QBSyncResult> CreateExpense(QBExpense expense, string companyId)
    {
        try
        {
            var requestBody = new
            {
                EntityRef = new { value = expense.VendorRef, type = "Vendor" },
                TxnDate = expense.TxnDate.ToString("yyyy-MM-dd"),
                TotalAmt = expense.TotalAmt,
                DocNumber = expense.DocNumber,
                PrivateNote = expense.PrivateNote,
                PaymentType = expense.PaymentType,
                Line = expense.Lines.Select((line, index) => new
                {
                    Id = (index + 1).ToString(),
                    Amount = line.Amount,
                    DetailType = "AccountBasedExpenseLineDetail",
                    AccountBasedExpenseLineDetail = new
                    {
                        AccountRef = new { value = "66" }, // Default expense account
                        BillableStatus = "NotBillable"
                    },
                    Description = line.Description
                }).ToArray()
            };

            var response = await PostToQB($"v3/company/{companyId}/purchase", requestBody);

            if (response.Success && response.Data != null)
            {
                var expenseData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                var qbId = expenseData.GetProperty("QueryResponse")
                    .GetProperty("Purchase")[0]
                    .GetProperty("Id").GetString();

                return new QBSyncResult { Success = true, QBId = qbId };
            }

            return new QBSyncResult { Success = false, ErrorMessage = response.ErrorMessage ?? "Unknown error creating expense" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create expense in QuickBooks");
            return new QBSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<QBSyncResult> CreateSalesReceipt(QBSalesReceipt receipt, string companyId)
    {
        try
        {
            var requestBody = new
            {
                TxnDate = receipt.TxnDate.ToString("yyyy-MM-dd"),
                TotalAmt = receipt.TotalAmt,
                DocNumber = receipt.DocNumber,
                PrivateNote = receipt.PrivateNote,
                PaymentMethodRef = !string.IsNullOrEmpty(receipt.PaymentMethodRef)
                    ? new { value = GetPaymentMethodId(receipt.PaymentMethodRef) }
                    : null,
                Line = receipt.Lines.Select((line, index) => new
                {
                    Id = (index + 1).ToString(),
                    Amount = line.Amount,
                    DetailType = "SalesItemLineDetail",
                    SalesItemLineDetail = new
                    {
                        ItemRef = new { value = "1" }, // Default service item
                        Qty = line.Qty
                    },
                    Description = line.Description
                }).ToArray()
            };

            var response = await PostToQB($"v3/company/{companyId}/salesreceipt", requestBody);

            if (response.Success && response.Data != null)
            {
                var receiptData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                var qbId = receiptData.GetProperty("QueryResponse")
                    .GetProperty("SalesReceipt")[0]
                    .GetProperty("Id").GetString();

                return new QBSyncResult { Success = true, QBId = qbId };
            }

            return new QBSyncResult { Success = false, ErrorMessage = response.ErrorMessage ?? "Unknown error creating sales receipt" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sales receipt in QuickBooks");
            return new QBSyncResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<QBVendor?> GetVendorByName(string name, string companyId)
    {
        try
        {
            var encodedName = Uri.EscapeDataString(name);
            var response = await GetFromQB($"v3/company/{companyId}/query?query=SELECT * FROM Vendor WHERE Name = '{encodedName}'");

            if (response.Success && response.Data != null)
            {
                var vendorData = JsonSerializer.Deserialize<JsonElement>(response.Data);
                if (vendorData.TryGetProperty("QueryResponse", out var queryResponse) &&
                    queryResponse.TryGetProperty("Vendor", out var vendors) &&
                    vendors.GetArrayLength() > 0)
                {
                    var vendor = vendors[0];
                    return new QBVendor
                    {
                        Id = vendor.GetProperty("Id").GetString() ?? "",
                        DisplayName = vendor.GetProperty("DisplayName").GetString() ?? ""
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor by name from QuickBooks");
            return null;
        }
    }

    public async Task<bool> ValidateConnection(string companyId)
    {
        try
        {
            var response = await GetFromQB($"v3/company/{companyId}/companyinfo/{companyId}");
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(bool Success, string? Data, string? ErrorMessage)> PostToQB(string endpoint, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_config.BaseUrl}/{endpoint}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, responseContent, null);
            }

            return (false, null, $"HTTP {response.StatusCode}: {responseContent}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? Data, string? ErrorMessage)> GetFromQB(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_config.BaseUrl}/{endpoint}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, responseContent, null);
            }

            return (false, null, $"HTTP {response.StatusCode}: {responseContent}");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private string GetPaymentMethodId(string paymentMethod) => paymentMethod.ToLowerInvariant() switch
    {
        "cash" => "1",
        "check" => "2",
        "credit card" => "3",
        _ => "1"
    };
}