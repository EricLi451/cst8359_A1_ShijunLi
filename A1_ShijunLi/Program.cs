using A1_ShijunLi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace A1_ShijunLi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. 注册 MVC 控制器和视图服务
            builder.Services.AddControllersWithViews();

   
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR(); 

            // 2. 注册数据库服务
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=EventManagerDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

            // 3. 注册 Identity 身份服务
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
               .AddRoles<IdentityRole>() // 启用角色功能 (Organizer / Attendee) [cite: 21, 31, 32]
                .AddEntityFrameworkStores<ApplicationDbContext>(); 

            var app = builder.Build();

            // 4. 初始化数据库
            // 4. 初始化数据库
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();

                // 获取两个新的管家服务
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                // 调用新的异步方法，使用 .Wait() 同步等待它完成
                DbInitializer.InitializeAsync(context, userManager, roleManager).Wait();
            }
            // 5. 配置 HTTP 请求管道 (中间件顺序很重要)
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
// 确保 UseAuthentication 在 UseAuthorization 之前 [cite: 22]
            app.UseAuthentication();
            app.UseAuthorization();

            // 6. 配置路由
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

  
            app.MapRazorPages();
            app.MapHub<A1_ShijunLi.Hubs.EventHub>("/eventHub"); // 映射通讯塔的地址 
            app.Run();
        }
    }
}