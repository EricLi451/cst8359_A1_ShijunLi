using System.Collections.Generic;

namespace A1_ShijunLi.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }

        public List<Attendee> Attendees { get; set; } = new List<Attendee>();
    }
}