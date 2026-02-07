using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConditionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<LookupDto>> GetConditions()
    {
        return Enum.GetValues<ItemCondition>()
            .Select(e => new LookupDto
            {
                Value = e.ToString(),
                Label = e.ToDisplayName(),
                SortOrder = (int)e
            }).ToList();
    }
}