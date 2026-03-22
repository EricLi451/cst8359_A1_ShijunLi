using A1_ShijunLi.Models;
using System;
using System.Linq;

namespace A1_ShijunLi.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
        
            context.Database.EnsureCreated();

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