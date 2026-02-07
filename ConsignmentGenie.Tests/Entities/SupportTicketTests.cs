using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ConsignmentGenie.Tests.Entities;

public class SupportTicketTests
{
    [Fact]
    public void SupportTicket_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var supportTicket = new SupportTicket();

        // Assert
        Assert.Equal(SupportTicketStatus.Open, supportTicket.Status);
        Assert.Equal(SupportTicketAssignedTo.NotAssigned, supportTicket.AssignedTo);
        Assert.Equal(string.Empty, supportTicket.Subject);
        Assert.Equal(string.Empty, supportTicket.Description);
        Assert.Equal(Guid.Empty, supportTicket.SubmittedById);
    }

    [Fact]
    public void SupportTicket_RequiredFields_ValidationAttributes()
    {
        // Arrange
        var supportTicket = new SupportTicket
        {
            // Required fields intentionally not set
            Category = SupportTicketCategory.Technical,
            AssignedTo = SupportTicketAssignedTo.Admin
        };

        // Act - Verify Required attributes exist on required properties
        var categoryProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.Category));
        var assignedToProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.AssignedTo));
        var subjectProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.Subject));
        var descriptionProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.Description));
        var submittedByIdProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.SubmittedById));

        // Assert properties exist
        Assert.NotNull(categoryProperty);
        Assert.NotNull(assignedToProperty);
        Assert.NotNull(subjectProperty);
        Assert.NotNull(descriptionProperty);
        Assert.NotNull(submittedByIdProperty);

        // Verify required attributes exist
        var categoryRequired = categoryProperty.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
        var assignedToRequired = assignedToProperty.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
        var subjectRequired = subjectProperty.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
        var descriptionRequired = descriptionProperty.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();
        var submittedByIdRequired = submittedByIdProperty.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        Assert.NotNull(categoryRequired);
        Assert.NotNull(assignedToRequired);
        Assert.NotNull(subjectRequired);
        Assert.NotNull(descriptionRequired);
        Assert.NotNull(submittedByIdRequired);
    }

    [Fact]
    public void SupportTicket_SubjectProperty_HasMaxLengthAttribute()
    {
        // Arrange
        var subjectProperty = typeof(SupportTicket).GetProperty(nameof(SupportTicket.Subject));

        // Act
        var maxLengthAttribute = subjectProperty.GetCustomAttributes(typeof(MaxLengthAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(maxLengthAttribute);
        var maxLength = ((MaxLengthAttribute)maxLengthAttribute).Length;
        Assert.Equal(200, maxLength);
    }

    [Fact]
    public void SupportTicket_ValidEntity_PassesValidation()
    {
        // Arrange
        var supportTicket = new SupportTicket
        {
            Category = SupportTicketCategory.Technical,
            AssignedTo = SupportTicketAssignedTo.Admin,
            Subject = "Test Subject",
            Description = "Test Description",
            SubmittedById = Guid.NewGuid(),
            Status = SupportTicketStatus.Open
        };

        // Act
        var context = new ValidationContext(supportTicket);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(supportTicket, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void SupportTicket_SubjectTooLong_FailsValidation()
    {
        // Arrange
        var supportTicket = new SupportTicket
        {
            Category = SupportTicketCategory.Technical,
            AssignedTo = SupportTicketAssignedTo.Admin,
            Subject = new string('a', 201), // Exceeds 200 char limit
            Description = "Test Description",
            SubmittedById = Guid.NewGuid()
        };

        // Act
        var context = new ValidationContext(supportTicket);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(supportTicket, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SupportTicket.Subject)));
    }

    [Theory]
    [InlineData(SupportTicketCategory.Content)]
    [InlineData(SupportTicketCategory.Technical)]
    [InlineData(SupportTicketCategory.Billing)]
    [InlineData(SupportTicketCategory.Other)]
    public void SupportTicket_AllCategories_CanBeSet(SupportTicketCategory category)
    {
        // Arrange
        var supportTicket = new SupportTicket();

        // Act
        supportTicket.Category = category;

        // Assert
        Assert.Equal(category, supportTicket.Category);
    }

    [Theory]
    [InlineData(SupportTicketStatus.Open)]
    [InlineData(SupportTicketStatus.InProgress)]
    [InlineData(SupportTicketStatus.Resolved)]
    [InlineData(SupportTicketStatus.Closed)]
    public void SupportTicket_AllStatuses_CanBeSet(SupportTicketStatus status)
    {
        // Arrange
        var supportTicket = new SupportTicket();

        // Act
        supportTicket.Status = status;

        // Assert
        Assert.Equal(status, supportTicket.Status);
    }

    [Theory]
    [InlineData(SupportTicketAssignedTo.NotAssigned)]
    [InlineData(SupportTicketAssignedTo.Owner)]
    [InlineData(SupportTicketAssignedTo.Admin)]
    public void SupportTicket_AllAssignedToValues_CanBeSet(SupportTicketAssignedTo assignedTo)
    {
        // Arrange
        var supportTicket = new SupportTicket();

        // Act
        supportTicket.AssignedTo = assignedTo;

        // Assert
        Assert.Equal(assignedTo, supportTicket.AssignedTo);
    }

    [Fact]
    public void SupportTicketCategory_EnumValues_AreCorrect()
    {
        // Assert
        Assert.Equal(1, (int)SupportTicketCategory.Content);
        Assert.Equal(2, (int)SupportTicketCategory.Technical);
        Assert.Equal(3, (int)SupportTicketCategory.Billing);
        Assert.Equal(4, (int)SupportTicketCategory.Other);
    }

    [Fact]
    public void SupportTicketStatus_EnumValues_AreCorrect()
    {
        // Assert
        Assert.Equal(1, (int)SupportTicketStatus.Open);
        Assert.Equal(2, (int)SupportTicketStatus.InProgress);
        Assert.Equal(3, (int)SupportTicketStatus.Resolved);
        Assert.Equal(4, (int)SupportTicketStatus.Closed);
    }

    [Fact]
    public void SupportTicketAssignedTo_EnumValues_AreCorrect()
    {
        // Assert
        Assert.Equal(0, (int)SupportTicketAssignedTo.NotAssigned);
        Assert.Equal(1, (int)SupportTicketAssignedTo.Owner);
        Assert.Equal(2, (int)SupportTicketAssignedTo.Admin);
    }

    [Fact]
    public void SupportTicket_InheritsFromBaseEntity()
    {
        // Arrange
        var supportTicket = new SupportTicket();

        // Act & Assert
        Assert.IsAssignableFrom<BaseEntity>(supportTicket);
        Assert.NotEqual(Guid.Empty, supportTicket.Id);
        Assert.True(supportTicket.CreatedAt <= DateTime.UtcNow);
        Assert.Null(supportTicket.UpdatedAt);
    }
}