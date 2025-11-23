namespace ConsignmentGenie.Application.DTOs.Reports;

public class TrendsReportDto
{
    public List<WeeklyTrendDto> WeeklyTrends { get; set; } = new();
    public List<CategoryTrendDto> CategoryTrends { get; set; } = new();
    public TrendsSummaryDto Summary { get; set; } = new();
}

public class WeeklyTrendDto
{
    public int Year { get; set; }
    public int Week { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTicket { get; set; }
    public DateTime StartDate { get; set; }
}

public class CategoryTrendDto
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int ItemsSold { get; set; }
    public decimal AveragePrice { get; set; }
    public List<WeeklyDataDto> WeeklyData { get; set; } = new();
}

public class WeeklyDataDto
{
    public int Week { get; set; }
    public int Year { get; set; }
    public decimal Revenue { get; set; }
    public int Count { get; set; }
}

public class TrendsSummaryDto
{
    public int TotalPeriods { get; set; }
    public decimal AverageWeeklyRevenue { get; set; }
    public decimal GrowthRate { get; set; }
    public string TopCategory { get; set; } = string.Empty;
}

public class TrendsFilterDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}