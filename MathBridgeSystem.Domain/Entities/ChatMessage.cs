using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class ChatMessage
{
    public Guid MessageId { get; set; }

    public Guid UserId { get; set; }

    public string MessageText { get; set; } = null!;

    public Guid? RecipientUserId { get; set; }

    public string MessageType { get; set; } = null!;

    public DateTime SentDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User? RecipientUser { get; set; }

    public virtual User User { get; set; } = null!;
}
