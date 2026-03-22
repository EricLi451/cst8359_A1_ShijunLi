using Microsoft.EntityFrameworkCore;
using A1_ShijunLi.Models;

namespace A1_ShijunLi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<Attendee> Attendees { get; set; }
    }
}