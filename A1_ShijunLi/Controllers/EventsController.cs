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
// === 新增：引入权限控制工具 ===
using Microsoft.AspNetCore.Authorization;

namespace A1_ShijunLi.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EventsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. EVENT CRUD (事件的增删改查)

        // GET: Events (列表页) - 任何人都能看
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Events.ToListAsync());
        }

        // GET: Events/Details/5 (详情页) - 任何人都能看
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

        // GET: Events/Create (创建页) - 只有组织者能进
        [Authorize(Roles = "Organizer")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Create([Bind("Title,Description,Date,Location")] Event @event, IFormFile bannerImage)
        {
            if (!string.IsNullOrEmpty(@event.Title))
            {
                @event.BannerUrl = await UploadImageAsync(bannerImage) ?? "";
                @event.Description = @event.Description ?? "";
                @event.Location = @event.Location ?? "";

                ModelState.Clear();

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5 - 只有组织者能进
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Date,Location,BannerUrl")] Event @event, IFormFile bannerImage)
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

        // GET: Events/Delete/5 - 只有组织者能进
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Delete/5
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

        // 2. ATTENDEE CRUD (参与者管理)

        // 查看参与者列表 - 允许登录用户查看 (根据作业要求)
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

        // 添加参与者 - 只有组织者能操作
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
                if (string.IsNullOrEmpty(attendee.Id))
                {
                    attendee.Id = Guid.NewGuid().ToString();
                }
                ModelState.Clear();
                _context.Attendees.Add(attendee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageAttendees), new { eventId = eventId });
            }
            ViewBag.EventId = eventId;
            return View(attendee);
        }

        // 删除参与者 - 只有组织者能操作
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

        // 3. 辅助方法 (保持不变)
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