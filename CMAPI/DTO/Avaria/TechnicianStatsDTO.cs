using System;
using System.Collections.Generic;

namespace CMAPI.DTO.Avaria;

public class TechnicianStatsDTO
{
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; }
    public int TotalAvariaResolved { get; set; }
    public double AverageResolutionTime { get; set; } // in hours
    public int OnTimeResolutions { get; set; }
    public int DelayedResolutions { get; set; }
    public List<AvariaTypeStatsDTO> AvariaTypeStats { get; set; }
    public List<MonthlyStatsDTO> MonthlyStats { get; set; }
}

public class AvariaTypeStatsDTO
{
    public string AvariaType { get; set; }
    public int Count { get; set; }
    public double AverageResolutionTime { get; set; } // in hours
}

public class MonthlyStatsDTO
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalAvariaResolved { get; set; }
    public double AverageResolutionTime { get; set; } // in hours
    public int OnTimeResolutions { get; set; }
    public int DelayedResolutions { get; set; }
} 