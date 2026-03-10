using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class Notification
{
    public long NotiId { get; set; }

    public int RuleId { get; set; }

    public int UserId { get; set; }

    public int OrgId { get; set; }

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }

    public bool? IsRead { get; set; }

    public virtual AlertRule Rule { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;
}
