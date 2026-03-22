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

        // GET: Events (列表页)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Events.ToListAsync());
        }

        // GET: Events/Details/5 (详情页)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events
                .Include(e => e.Attendees) // 确保连同参与者一起查出
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        // GET: Events/Create (创建页)
        public IActionResult Create()
        {
            return View();
        }
        // POST: Events/Create (处理创建提交)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Date,Location")] Event @event, IFormFile bannerImage)
        {
            //只要用户填了标题，就强制允许创
            if (!string.IsNullOrEmpty(@event.Title))
            {
                // 处理图片，如果没有传图片，必须给一个空字符串 ""，否则数据库会因为 NULL 报错
                @event.BannerUrl = await UploadImageAsync(bannerImage) ?? "";

                // 确保其他字段哪怕没填也不会是 null
                @event.Description = @event.Description ?? "";
                @event.Location = @event.Location ?? "";

                // 清除所有验证拦截器 (红灯变绿灯)
                ModelState.Clear();

                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // 成功后跳回列表页
            }
            return View(@event);
        }

        // GET: Events/Edit/5 (编辑页)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
     
            var @event = await _context.Events.FindAsync(id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Edit/5 (处理编辑提交)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Date,Location,BannerUrl")] Event @event, IFormFile bannerImage)
        {
            if (id != @event.Id) return NotFound();

            // 只要有标题就允许修改
            if (!string.IsNullOrEmpty(@event.Title))
            {
                try
                {
                    // 如果用户上传了新图片，则替换旧的 BannerUrl
                    if (bannerImage != null && bannerImage.Length > 0)
                    {
                        @event.BannerUrl = await UploadImageAsync(bannerImage) ?? "";
                    }

                    // 兜底，防止出现 null 导致数据库崩溃
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

        // GET: Events/Delete/5 (删除确认页)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @event = await _context.Events.FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null) return NotFound();

            return View(@event);
        }

        // POST: Events/Delete/5 (处理删除动作)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        // 2. ATTENDEE CRUD (参与者管理 - 使用属性路由)

        // GET: /events/{eventId}/attendees (查看某事件的所有参与者)
        [Route("events/{eventId}/attendees")]
        public async Task<IActionResult> ManageAttendees(int eventId)
        {
            var @event = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null) return NotFound();

            return View(@event); // 传递 Event 模型到视图，方便显示事件标题和参与者列表
        }

        // GET: /events/{eventId}/attendees/create (为事件添加参与者页面)
        [Route("events/{eventId}/attendees/create")]
        public IActionResult AddAttendee(int eventId)
        {
            ViewBag.EventId = eventId;
            return View();
        }

        // POST: /events/{eventId}/attendees/create (处理添加参与者)
        [HttpPost]
        [Route("events/{eventId}/attendees/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendee(int eventId, [Bind("Name,Email")] Attendee attendee)
        {
            // 只要用户填了名字和邮箱，就强制允许创建！
            if (!string.IsNullOrEmpty(attendee.Name) && !string.IsNullOrEmpty(attendee.Email))
            {
                attendee.EventId = eventId;

                // 确保 ID 有值 (保险起见)
                if (string.IsNullOrEmpty(attendee.Id))
                {
                    attendee.Id = Guid.NewGuid().ToString();
                }

                // 清除所有验证拦截器 (红灯变绿灯)
                ModelState.Clear();

                _context.Attendees.Add(attendee);
                await _context.SaveChangesAsync();

                // 成功后跳回该事件的参与者列表页
                return RedirectToAction(nameof(ManageAttendees), new { eventId = eventId });
            }

            // 如果没填全，再退回表单
            ViewBag.EventId = eventId;
            return View(attendee);
        }

        // POST: /events/{eventId}/attendees/{attendeeId}/delete (删除某位参与者)
        [HttpPost]
        [Route("events/{eventId}/attendees/{attendeeId}/delete")]
        [ValidateAntiForgeryToken]
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

        // 3. 辅助方法 (Helper Methods)

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        // 将图片上传逻辑提取出来
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