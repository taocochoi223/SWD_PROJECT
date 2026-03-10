using System;
using System.Collections.Generic;

namespace SWD.DAL.Models;

public partial class Organization
{
    public int OrgId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Site> Sites { get; set; } = new List<Site>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
