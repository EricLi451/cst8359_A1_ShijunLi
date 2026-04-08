using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using A1_ShijunLi.Models;

namespace A1_ShijunLi.Data
{
    // 注意这里：从继承 DbContext 改成了继承 IdentityDbContext<IdentityUser>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Attendee> Attendees { get; set; }
    }
}