using System;
using System.Collections.Generic;

namespace A1_ShijunLi.Models
{
    public class Event
    {
        public int Id { get; set; } 
        public string Title { get; set; } 
        public string Description { get; set; } 
        public DateTime Date { get; set; } 
        public string Location { get; set; }
        public string BannerUrl { get; set; }

        public List<Attendee> Attendees { get; set; } = new List<Attendee>();
        public string? OrganizerId { get; set; }
    }
}