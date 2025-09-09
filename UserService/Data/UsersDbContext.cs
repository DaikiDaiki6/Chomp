using System;
using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
}
