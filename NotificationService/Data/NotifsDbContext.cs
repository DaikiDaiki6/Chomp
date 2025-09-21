using System;
using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

public class NotifsDbContext : DbContext
{
    public NotifsDbContext(DbContextOptions<NotifsDbContext> options) : base(options) { }
    
    public DbSet<Notification> Notifications { get; set; }
}
