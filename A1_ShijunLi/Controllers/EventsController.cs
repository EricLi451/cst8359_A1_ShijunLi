using Microsoft.AspNetCore.Mvc;
using A1_ShijunLi.Models; 
using System.Collections.Generic;
using System.Linq;

namespace A1_ShijunLi.Controllers
{
    public class EventsController : Controller
    {
        private static List<Event> _events = new List<Event>
        {
            new Event { Id = 1, Title = "Career Fair", Date = DateTime.Parse("2026-02-01"), Location = "Gym" },
            new Event { Id = 2, Title = "Tech Talk", Date = DateTime.Parse("2026-02-08"), Location = "Auditorium" },
            new Event { Id = 3, Title = "Hack Night", Date = DateTime.Parse("2026-02-15"), Location = "Library" }
        };

        public IActionResult Index()
        {
            return View(_events);
        }

        [HttpGet]
        public IActionResult Manage(int id)
        {
            var eventToManage = _events.FirstOrDefault(e => e.Id == id);
            if (eventToManage == null)
            {
                return NotFound();
            }
            return View(eventToManage);
        }

        [HttpPost]
        public IActionResult Manage(int id, string name, string email)
        {
            var eventToManage = _events.FirstOrDefault(e => e.Id == id);

            if (eventToManage != null)
            {
                var newAttendee = new Attendee
                {
                    Name = name,
                    Email = email
                };

                eventToManage.Attendees.Add(newAttendee);
                ViewData["SuccessMessage"] = "Attendee registered!";
            }

            return View(eventToManage);
        }
    }
}