using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Constants;
using System.Text.Json;

namespace ConsignmentGenie.Application.Helpers;

public static class NotificationHelper
{
    public static CreateNotificationRequest CreateItemSoldNotification(
        Transaction transaction, Item item, Consignor consignor, TransactionItem transactionItem)
    {
        var lineTotal = transactionItem.UnitPrice * transactionItem.Quantity;

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = consignor.UserId!.Value,
            ToType = "consignor",
            OrganizationId = item.OrganizationId,
            Type = NotificationTypes.ITEM_SOLD,
            Title = "Item Sold!",
            Message = $"Your item \"{item.Title}\" sold for {lineTotal:C}",
            Payload = new
            {
                itemName = item.Title,
                salePrice = lineTotal,
                yourEarnings = transactionItem.ConsignorAmount,
                soldDate = transaction.CreatedAt
            },
            ActionUrl = $"/consignor/sales/{transaction.Id}",
            ItemId = item.Id,
            TransactionId = transaction.Id
        };
    }

    public static CreateNotificationRequest CreatePayoutProcessedNotification(
        Payout payout, Consignor consignor)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = consignor.UserId!.Value,
            ToType = "consignor",
            OrganizationId = payout.OrganizationId,
            Type = NotificationTypes.PAYOUT_PROCESSED,
            Title = "Payout Processed",
            Message = $"A payout of {payout.Amount:C} has been sent to your account",
            Payload = new
            {
                amount = payout.Amount,
                paymentMethod = payout.PaymentMethod,
                processedDate = payout.CreatedAt
            },
            ActionUrl = $"/consignor/payouts/{payout.Id}",
            PayoutId = payout.Id
        };
    }

    public static CreateNotificationRequest CreateManualPayoutProcessedNotification(
        Consignor consignor, decimal amount, string paymentMethod, string payoutReference)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = consignor.UserId!.Value,
            ToType = "consignor",
            OrganizationId = consignor.OrganizationId,
            Type = NotificationTypes.PAYOUT_PROCESSED,
            Title = "Payout Processed",
            Message = $"A payout of {amount:C} has been processed via {paymentMethod}",
            Payload = new
            {
                amount = amount,
                paymentMethod = paymentMethod,
                payoutReference = payoutReference,
                processedDate = DateTime.UtcNow
            },
            ActionUrl = $"/consignor/payouts",
            ReferenceType = "payout",
            ReferenceId = consignor.Id
        };
    }

    public static CreateNotificationRequest CreateNewConsignorRequestNotification(
        Consignor consignor, User owner, Guid organizationId)
    {
        return new CreateNotificationRequest
        {
            FromUserId = consignor.UserId,
            FromType = "consignor",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = organizationId,
            Type = NotificationTypes.NEW_CONSIGNOR_REQUEST,
            Title = "New Consignor Request",
            Message = $"{consignor.Name} wants to join your shop",
            Payload = new
            {
                consignorName = consignor.Name,
                consignorEmail = consignor.Email,
                requestedAt = DateTime.UtcNow
            },
            ActionUrl = $"/owner/consignors/{consignor.Id}",
            ReferenceType = "consignor",
            ReferenceId = consignor.Id
        };
    }

    public static CreateNotificationRequest CreateNewConsignorPendingRequestNotification(
        User pendingConsignorUser, User owner, Guid organizationId)
    {
        var nameParts = pendingConsignorUser.Name?.Split(' ', 2) ?? new[] { pendingConsignorUser.Email, "" };
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = organizationId,
            Type = NotificationTypes.NEW_CONSIGNOR_REQUEST,
            Title = "New Consignor Request",
            Message = $"{firstName} {lastName} wants to join your shop",
            Payload = new
            {
                consignorName = pendingConsignorUser.Name ?? pendingConsignorUser.Email,
                consignorEmail = pendingConsignorUser.Email,
                requestedAt = DateTime.UtcNow
            },
            ActionUrl = $"/owner/users/{pendingConsignorUser.Id}",
            ReferenceType = "user",
            ReferenceId = pendingConsignorUser.Id
        };
    }

    public static CreateNotificationRequest CreateNewItemRequestNotification(
        ItemRequest itemRequest, Consignor consignor, User owner)
    {
        return new CreateNotificationRequest
        {
            FromUserId = consignor.UserId,
            FromType = "consignor",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = itemRequest.OrganizationId,
            Type = NotificationTypes.NEW_ITEM_REQUEST,
            Title = "New Item Request",
            Message = $"{consignor.Name} submitted \"{itemRequest.Name}\" for review",
            Payload = new
            {
                itemName = itemRequest.Name,
                consignorName = consignor.Name,
                suggestedPrice = itemRequest.SuggestedPrice,
                submittedAt = itemRequest.CreatedAt
            },
            ActionUrl = $"/owner/item-requests/{itemRequest.Id}",
            ItemRequestId = itemRequest.Id,
        };
    }

    public static CreateNotificationRequest CreateItemRequestApprovedNotification(
        ItemRequest itemRequest, Item approvedItem, Consignor consignor)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = consignor.UserId!.Value,
            ToType = "consignor",
            OrganizationId = itemRequest.OrganizationId,
            Type = NotificationTypes.ITEM_REQUEST_APPROVED,
            Title = "Item Request Approved",
            Message = $"Your item \"{itemRequest.Name}\" has been approved and added to inventory",
            Payload = new
            {
                itemName = itemRequest.Name,
                approvedPrice = approvedItem.Price,
                approvedAt = itemRequest.ReviewedAt
            },
            ActionUrl = $"/consignor/items/{approvedItem.Id}",
            ItemRequestId = itemRequest.Id,
            ItemId = approvedItem.Id
        };
    }

    public static CreateNotificationRequest CreateItemRequestRejectedNotification(
        ItemRequest itemRequest, Consignor consignor, string rejectionReason)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = consignor.UserId!.Value,
            ToType = "consignor",
            OrganizationId = itemRequest.OrganizationId,
            Type = NotificationTypes.ITEM_REQUEST_REJECTED,
            Title = "Item Request Declined",
            Message = $"Your item \"{itemRequest.Name}\" request has been declined",
            Payload = new
            {
                itemName = itemRequest.Name,
                rejectionReason = rejectionReason,
                rejectedAt = itemRequest.ReviewedAt
            },
            ActionUrl = $"/consignor/item-requests/{itemRequest.Id}",
            ItemRequestId = itemRequest.Id
        };
    }

    public static CreateNotificationRequest CreateWelcomeNotification(
        User user, string role, Guid? organizationId = null)
    {
        var welcomeMessage = role switch
        {
            "consignor" => "Welcome to the consignor portal! You can now submit items and track your sales.",
            "owner" => "Welcome to ConsignmentGenie! Your shop is ready to go.",
            _ => "Welcome to ConsignmentGenie!"
        };

        var notificationType = role switch
        {
            "consignor" => NotificationTypes.CONSIGNOR_WELCOME,
            "owner" => NotificationTypes.OWNER_WELCOME,
            _ => NotificationTypes.CONSIGNOR_WELCOME
        };

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = user.Id,
            ToType = role,
            OrganizationId = organizationId,
            Type = notificationType,
            Title = "Welcome!",
            Message = welcomeMessage,
            Payload = new
            {
                firstName = user.Name,
                welcomedAt = DateTime.UtcNow
            },
            ActionUrl = $"/{role}/dashboard"
        };
    }

    public static CreateNotificationRequest CreateSubscriptionFailedNotification(
        User owner, Guid organizationId, string failureReason)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = organizationId,
            Type = NotificationTypes.SUBSCRIPTION_FAILED,
            Title = "Subscription Payment Failed",
            Message = "Your subscription payment failed. Please update your payment method to avoid service interruption.",
            Payload = new
            {
                failureReason = failureReason,
                failedAt = DateTime.UtcNow
            },
            ActionUrl = "/owner/settings/billing"
        };
    }

    public static CreateNotificationRequest CreateNewOwnerRequestNotification(
        User owner, Organization organization, User admin)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = admin.Id,
            ToType = "admin",
            OrganizationId = null, // Admin notifications don't have org scope
            Type = NotificationTypes.NEW_OWNER_REQUEST,
            Title = "New Shop Registration",
            Message = $"{owner.Name} registered shop \"{organization.Name}\"",
            Payload = new
            {
                ownerName = owner.Name,
                shopName = organization.Name,
                ownerEmail = owner.Email,
                registeredAt = organization.CreatedAt
            },
            ActionUrl = $"/admin/organizations/{organization.Id}",
            ReferenceType = "organization",
            ReferenceId = organization.Id
        };
    }

    public static CreateNotificationRequest CreateSystemErrorNotification(
        string errorMessage, string errorType, User admin)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = admin.Id,
            ToType = "admin",
            OrganizationId = null,
            Type = NotificationTypes.SYSTEM_ERROR,
            Title = "System Error",
            Message = $"System error occurred: {errorType}",
            Payload = new
            {
                errorType = errorType,
                errorMessage = errorMessage,
                occurredAt = DateTime.UtcNow
            },
            ActionUrl = "/admin/system/errors"
        };
    }

    // Customer notification methods
    public static CreateNotificationRequest CreateCustomerWelcomeNotification(
        User customerUser, Organization organization)
    {
        var portalUrl = $"/customer/{organization.StoreCode}?orgId={organization.Id}";

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = customerUser.Id,
            ToType = "customer",
            OrganizationId = organization.Id,
            Type = NotificationTypes.CUSTOMER_WELCOME,
            Title = $"Welcome to {organization.Name}!",
            Message = $"Welcome! Your account is ready. Visit your customer portal to browse items and view reservations.",
            Payload = new
            {
                shopName = organization.Name,
                portalUrl = portalUrl,
                welcomedAt = DateTime.UtcNow
            },
            ActionUrl = portalUrl
        };
    }

    public static CreateNotificationRequest CreateReservationConfirmedNotification(
        Reservation reservation, Customer customer, Organization organization, int itemCount)
    {
        var portalUrl = $"/customer/{organization.StoreCode}?orgId={organization.Id}";

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = customer.Id, // Customer ID directly, not UserId
            ToType = "customer",
            OrganizationId = organization.Id,
            Type = NotificationTypes.RESERVATION_CONFIRMED,
            Title = "Reservation Confirmed!",
            Message = $"Your reservation at {organization.Name} is confirmed! You have {itemCount} item{(itemCount > 1 ? "s" : "")} reserved worth {reservation.TotalValue:C}.",
            Payload = new
            {
                reservationId = reservation.Id,
                shopName = organization.Name,
                itemCount = itemCount,
                totalValue = reservation.TotalValue,
                expiresAt = reservation.ExpiresAt,
                portalUrl = portalUrl
            },
            ActionUrl = portalUrl,
            ReferenceType = "reservation",
            ReferenceId = reservation.Id
        };
    }

    public static CreateNotificationRequest CreateReservationExpiringSoonNotification(
        Reservation reservation, Customer customer, Organization organization, int itemCount, int hoursUntilExpiration)
    {
        var portalUrl = $"/customer/{organization.StoreCode}?orgId={organization.Id}";

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = customer.Id, // Customer ID directly, not UserId
            ToType = "customer",
            OrganizationId = organization.Id,
            Type = NotificationTypes.RESERVATION_EXPIRING_SOON,
            Title = "Reservation Expiring Soon",
            Message = $"Your reservation at {organization.Name} expires in {hoursUntilExpiration} hours. Please pick up your {itemCount} reserved item{(itemCount > 1 ? "s" : "")} soon!",
            Payload = new
            {
                reservationId = reservation.Id,
                shopName = organization.Name,
                itemCount = itemCount,
                totalValue = reservation.TotalValue,
                expiresAt = reservation.ExpiresAt,
                hoursUntilExpiration = hoursUntilExpiration,
                portalUrl = portalUrl
            },
            ActionUrl = portalUrl,
            ReferenceType = "reservation",
            ReferenceId = reservation.Id
        };
    }

    public static CreateNotificationRequest CreateReservationExpiredNotification(
        Reservation reservation, Customer customer, Organization organization, int itemCount)
    {
        var portalUrl = $"/customer/{organization.StoreCode}?orgId={organization.Id}";

        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = customer.Id, // Customer ID directly, not UserId
            ToType = "customer",
            OrganizationId = organization.Id,
            Type = NotificationTypes.RESERVATION_EXPIRED,
            Title = "Reservation Expired",
            Message = $"Your reservation at {organization.Name} has expired and your {itemCount} item{(itemCount > 1 ? "s have" : " has")} been returned to inventory.",
            Payload = new
            {
                reservationId = reservation.Id,
                shopName = organization.Name,
                itemCount = itemCount,
                totalValue = reservation.TotalValue,
                expiredAt = reservation.ExpiresAt,
                portalUrl = portalUrl
            },
            ActionUrl = portalUrl,
            ReferenceType = "reservation",
            ReferenceId = reservation.Id
        };
    }

    // Owner notification methods for customer activities
    public static CreateNotificationRequest CreateItemReservedNotification(
        Reservation reservation, Customer customer, User owner, int itemCount)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = reservation.OrganizationId,
            Type = NotificationTypes.ITEM_RESERVED,
            Title = "Items Reserved",
            Message = $"{customer.Name} reserved {itemCount} item{(itemCount > 1 ? "s" : "")} worth {reservation.TotalValue:C}",
            Payload = new
            {
                reservationId = reservation.Id,
                customerName = customer.Name,
                customerPhone = customer.PhoneNumber,
                customerEmail = customer.Email,
                itemCount = itemCount,
                totalValue = reservation.TotalValue,
                expiresAt = reservation.ExpiresAt,
                reservedAt = reservation.CreatedAt
            },
            ActionUrl = $"/owner/reservations/{reservation.Id}",
            ReferenceType = "reservation",
            ReferenceId = reservation.Id
        };
    }

    public static CreateNotificationRequest CreateReservationExpiredOwnerNotification(
        Reservation reservation, Customer customer, User owner, int itemCount)
    {
        return new CreateNotificationRequest
        {
            FromUserId = null, // System notification
            FromType = "system",
            ToUserId = owner.Id,
            ToType = "owner",
            OrganizationId = reservation.OrganizationId,
            Type = NotificationTypes.RESERVATION_EXPIRED_OWNER,
            Title = "Reservation Expired",
            Message = $"{customer.Name}'s reservation expired. {itemCount} item{(itemCount > 1 ? "s" : "")} returned to inventory ({reservation.TotalValue:C})",
            Payload = new
            {
                reservationId = reservation.Id,
                customerName = customer.Name,
                customerPhone = customer.PhoneNumber,
                itemCount = itemCount,
                totalValue = reservation.TotalValue,
                expiredAt = reservation.ExpiresAt,
                reservedAt = reservation.CreatedAt
            },
            ActionUrl = $"/owner/reservations/{reservation.Id}",
            ReferenceType = "reservation",
            ReferenceId = reservation.Id
        };
    }
}