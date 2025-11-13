﻿using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateRescheduleRequestDto
    {
        public Guid BookingId { get; set; }
        public DateOnly RequestedDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public Guid? RequestedTutorId { get; set; }
        public string? Reason { get; set; }
    }
}