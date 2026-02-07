using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class ConditionsControllerTests
    {
        private readonly ConditionsController _controller;

        public ConditionsControllerTests()
        {
            _controller = new ConditionsController();
        }

        [Fact]
        public void GetConditions_ReturnsAllConditions()
        {
            // Act
            var result = _controller.GetConditions();

            // Assert
            var okResult = Assert.IsType<ActionResult<List<LookupDto>>>(result);
            var conditions = Assert.IsType<List<LookupDto>>(okResult.Value);

            Assert.Equal(5, conditions.Count);

            // Verify all enum values are present with correct labels
            Assert.Contains(conditions, c => c.Value == "New" && c.Label == "New");
            Assert.Contains(conditions, c => c.Value == "LikeNew" && c.Label == "Like New");
            Assert.Contains(conditions, c => c.Value == "Good" && c.Label == "Good");
            Assert.Contains(conditions, c => c.Value == "Fair" && c.Label == "Fair");
            Assert.Contains(conditions, c => c.Value == "Poor" && c.Label == "Poor");
        }

        [Fact]
        public void GetConditions_ReturnsSortedByEnumValue()
        {
            // Act
            var result = _controller.GetConditions();

            // Assert
            var okResult = Assert.IsType<ActionResult<List<LookupDto>>>(result);
            var conditions = Assert.IsType<List<LookupDto>>(okResult.Value);

            // Verify sort order matches enum values
            Assert.Equal(1, conditions.First(c => c.Value == "New").SortOrder);
            Assert.Equal(2, conditions.First(c => c.Value == "LikeNew").SortOrder);
            Assert.Equal(3, conditions.First(c => c.Value == "Good").SortOrder);
            Assert.Equal(4, conditions.First(c => c.Value == "Fair").SortOrder);
            Assert.Equal(5, conditions.First(c => c.Value == "Poor").SortOrder);
        }
    }
}