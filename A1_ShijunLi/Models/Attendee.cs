using System;
using System.ComponentModel.DataAnnotations;
namespace A1_ShijunLi.Models
{
    public class Attendee
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); 
        public string Name { get; set; } 
        public string Email { get; set; }

        public int EventId { get; set; } 
        public Event Event { get; set; }
        public string? UserId { get; set; }
    }
}