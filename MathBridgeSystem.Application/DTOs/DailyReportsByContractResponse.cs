using MathBridgeSystem.Application.DTOs.DailyReport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class DailyReportsByContractResponse
    {
        public Guid ContractId { get; set; }
        public List<SessionReportGroupDto> Sessions { get; set; } = new();
    }

    public class SessionReportGroupDto
    {
        public Guid BookingId { get; set; }
        public DateOnly SessionDate { get; set; }                
        public DateTime? StartTime { get; set; }                  
        public DateTime? EndTime { get; set; }
        public string SessionInfo => $"{SessionDate:yyyy-MM-dd} {(StartTime.HasValue ? $"{StartTime:HH:mm}-{EndTime:HH:mm}" : "")}".Trim();

        public List<DailyReportDto> Reports { get; set; } = new(); 
    }
}
