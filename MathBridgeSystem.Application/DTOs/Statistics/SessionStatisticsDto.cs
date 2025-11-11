using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Statistics
{
    public class SessionStatisticsDto
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int CancelledSessions { get; set; }
        public int UpcomingSessions { get; set; }
        public int RescheduledSessions { get; set; }
        public decimal CompletionRate { get; set; }
    }

    public class SessionOnlineVsOfflineDto
    {
        public int OnlineSessions { get; set; }
        public int OfflineSessions { get; set; }
        public decimal OnlinePercentage { get; set; }
        public decimal OfflinePercentage { get; set; }
    }

    public class SessionTrendDto
    {
        public DateTime Date { get; set; }
        public int SessionCount { get; set; }
    }

    public class SessionTrendStatisticsDto
    {
        public List<SessionTrendDto> Trends { get; set; } = new List<SessionTrendDto>();
        public int TotalSessionsInPeriod { get; set; }
    }
}

