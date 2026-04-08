using A1_ShijunLi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace A1_ShijunLi.Data
{
    public static class DbInitializer
    {
        // 注意：这里改成了异步方法 InitializeAsync，并且引入了两个新管家
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.Migrate();

            // 1. 创建角色 (Roles)
            string[] roleNames = { "Organizer", "Attendee" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. 创建测试账号并分配角色
            // 组织者账号 (Organizer)
            if (await userManager.FindByEmailAsync("organizer@test.com") == null)
            {
                var orgUser = new IdentityUser { UserName = "organizer@test.com", Email = "organizer@test.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(orgUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(orgUser, "Organizer");
                }
            }

            // 参与者账号 (Attendee)
            if (await userManager.FindByEmailAsync("attendee@test.com") == null)
            {
                var attUser = new IdentityUser { UserName = "attendee@test.com", Email = "attendee@test.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(attUser, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(attUser, "Attendee");
                }
            }

            // 3. 原有的事件数据初始化 (如果数据库已经有事件了，就直接退出)
            if (context.Events.Any())
            {
                return;
            }

            var events = new Event[]
            {
                new Event { Title = "Routing Workshop", Description = "Learn about attribute routing.", Date = DateTime.Parse("2026-03-17 05:56"), Location = "Algonquin College - T Building", BannerUrl = "" },
                new Event { Title = "Tech Conference 2026", Description = "A full-day conference.", Date = DateTime.Parse("2026-03-27 05:56"), Location = "Ottawa Convention Centre", BannerUrl = "" },
                new Event { Title = "EF Core Bootcamp", Description = "Master Entity Framework.", Date = DateTime.Parse("2026-04-06 05:56"), Location = "Online", BannerUrl = "" }
            };
            context.Events.AddRange(events);
            context.SaveChanges();

            var attendees = new Attendee[]
            {
                new Attendee { Name = "Alice Smith", Email = "alice@example.com", EventId = events[0].Id },
                new Attendee { Name = "Bob Jones", Email = "bob@example.com", EventId = events[0].Id },
                new Attendee { Name = "Charlie", Email = "charlie@example.com", EventId = events[1].Id },
                new Attendee { Name = "Dave", Email = "dave@example.com", EventId = events[1].Id },
                new Attendee { Name = "Eve", Email = "eve@example.com", EventId = events[2].Id },
                new Attendee { Name = "Frank", Email = "frank@example.com", EventId = events[2].Id }
            };
            context.Attendees.AddRange(attendees);
            context.SaveChanges();
        }
    }
}