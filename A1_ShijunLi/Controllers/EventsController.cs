using Microsoft.AspNetCore.Mvc;
using A1_ShijunLi.Models;
using A1_ShijunLi.Data;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using A1_ShijunLi.Hubs;
using System.Security.Claims;

namespace A1_ShijunLi.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<EventHub> _hubContext;

        public EventsController(ApplicationDbContext context, IConfiguration configuration, IHubContext<EventHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        // 1. EVENT CRUD (事件的增删改查)

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Events.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        [Authorize(Roles = "Organizer")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Create([Bind("Title,Description,Date,Location")] Event @event, IFormFile bannerImage)
        {
            if (!string.IsNullOrEmpty(@event.Title))
            {
                // === A3 新增：记录是哪个 Organizer 创建了这个活动 ===
                @event.OrganizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                @event.BannerUrl = await UploadImageAsync(bannerImage) ?? "";
                @event.Description = @event.Description ?? "";
                @event.Location = @event.Location ?? "";

                ModelState.Clear();

                _context.Add(@event);
                await _context.SaveChangesAsync();

                // 【修复】：去掉了这里贴错的 SignalR 代码

                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Date,Location,BannerUrl,OrganizerId")] Event @event, IFormFile bannerImage)
        {
            if (id != @event.Id) return NotFound();

            if (!string.IsNullOrEmpty(@event.Title))
            {
                try
                {
                    if (bannerImage != null && bannerImage.Length > 0)
                    {
                        @event.BannerUrl = await UploadImageAsync(bannerImage) ?? "";
                    }

                    @event.BannerUrl = @event.BannerUrl ?? "";
                    @event.Description = @event.Description ?? "";
                    @event.Location = @event.Location ?? "";
                    // 确保旧的 OrganizerId 不会丢失
                    @event.OrganizerId = @event.OrganizerId ?? "";

                    ModelState.Clear();

                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // === A3 新增：自我报名与取消报名逻辑 ===
        // ==========================================

        [HttpPost]
        [Authorize] // 只要登录了就能点报名
        public async Task<IActionResult> Register(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.Identity!.Name; // 获取登录的邮箱作为名字

            // 检查：防止重复报名
            var alreadyRegistered = await _context.Attendees
                .AnyAsync(a => a.EventId == id && a.UserId == userId);

            if (!alreadyRegistered)
            {
                var newAttendee = new Attendee
                {
                    EventId = id,
                    UserId = userId,
                    Name = userEmail,
                    Email = userEmail
                };

                _context.Attendees.Add(newAttendee);
                await _context.SaveChangesAsync();

                // 【修复】：SignalR 的广播代码正确地放在了这里！
                // 1. 获取这个 Event 的最新总人数，以及老板的 ID
                var eventData = await _context.Events.Include(e => e.Attendees).FirstOrDefaultAsync(e => e.Id == id);
                int count = eventData?.Attendees?.Count ?? 1;
                string organizerId = eventData?.OrganizerId;

                // 2. 广播给所有正在看这个页面的人（更新列表和人数）
                await _hubContext.Clients.Group($"event-{id}")
                    .SendAsync("UpdateAttendeeList", userEmail, count);

                // 3. 私聊通知老板
                if (!string.IsNullOrEmpty(organizerId))
                {
                    await _hubContext.Clients.User(organizerId)
                        .SendAsync("ReceiveNotification", $"{userEmail} just registered for your event '{eventData.Title}'.");
                }
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Unregister(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 找到属于当前用户的报名记录
            var attendee = await _context.Attendees
                .FirstOrDefaultAsync(a => a.EventId == id && a.UserId == userId);

            if (attendee != null)
            {
                _context.Attendees.Remove(attendee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // ==========================================
        // 2. 传统 ATTENDEE CRUD (保持不变，供 Organizer 使用)
        // ==========================================

        [Route("events/{eventId}/attendees")]
        [Authorize]
        public async Task<IActionResult> ManageAttendees(int eventId)
        {
            var @event = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null) return NotFound();

            return View(@event);
        }

        [Route("events/{eventId}/attendees/create")]
        [Authorize(Roles = "Organizer")]
        public IActionResult AddAttendee(int eventId)
        {
            ViewBag.EventId = eventId;
            return View();
        }

        [HttpPost]
        [Route("events/{eventId}/attendees/create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> AddAttendee(int eventId, [Bind("Name,Email")] Attendee attendee)
        {
            if (!string.IsNullOrEmpty(attendee.Name) && !string.IsNullOrEmpty(attendee.Email))
            {
                attendee.EventId = eventId;
                if (string.IsNullOrEmpty(attendee.Id)) attendee.Id = Guid.NewGuid().ToString();

                ModelState.Clear();
                _context.Attendees.Add(attendee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAttendees), new { eventId = eventId });
            }
            ViewBag.EventId = eventId;
            return View(attendee);
        }

        [HttpPost]
        [Route("events/{eventId}/attendees/{attendeeId}/delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteAttendee(int eventId, string attendeeId)
        {
            var attendee = await _context.Attendees.FirstOrDefaultAsync(a => a.Id == attendeeId && a.EventId == eventId);
            if (attendee != null)
            {
                _context.Attendees.Remove(attendee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageAttendees), new { eventId = eventId });
        }

        // 3. 辅助方法
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            string storageConnectionString = _configuration.GetConnectionString("AzureStorage");
            if (string.IsNullOrEmpty(storageConnectionString)) return null;

            BlobContainerClient containerClient = new BlobContainerClient(storageConnectionString, "event-banners");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return blobClient.Uri.ToString();
        }
    }
}