namespace ConsignmentGenie.Core.Enums;

public enum ReservationStatus
{
    /// <summary>
    /// Reservation created, awaiting SMS verification
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Phone verified, reservation is active and holding items
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Reservation expired (24+ hours with no pickup)
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Customer cancelled the reservation
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Items were picked up and purchased
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Items were picked up but not all purchased (partial completion)
    /// </summary>
    PartiallyCompleted = 5,

    /// <summary>
    /// Reservation was cancelled by staff
    /// </summary>
    CancelledByStaff = 6
}