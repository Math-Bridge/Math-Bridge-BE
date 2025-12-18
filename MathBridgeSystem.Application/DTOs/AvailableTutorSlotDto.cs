using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class AvailableTutorSlotDto
    {
        public DateOnly Date { get; set; }
        public string Slot { get; set; } = null!; 
        public List<string> AvailableTutors { get; set; } = new(); 
    }
}
