using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace ConsignmentGenie.Core.Entities;

public class SupportTicket : BaseEntity
{
    [Required]
    public SupportTicketCategory Category { get; set; }

    [Required]
    public SupportTicketAssignedTo AssignedTo { get; set; } = SupportTicketAssignedTo.NotAssigned;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid SubmittedById { get; set; }

    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

    // Navigation properties
    public User SubmittedBy { get; set; } = null!;
}