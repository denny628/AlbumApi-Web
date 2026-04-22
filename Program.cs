using AlbumApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. 設定 Port (讓 Railway 和本地都能跑)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 2. 資料庫連線設定 (關鍵修復：防止崩潰)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 強制指定 Railway 預設的 MySQL 8.0 主版本
// var serverVersion = new MySqlServerVersion(new Version(8, 0, 33));

// 請將 AppDbContext 替換為你實際定義的 DbContext 類別名稱 (如位於 Data 資料夾下的 Context)
builder.Services.AddDbContext<AlbumContext>(options =>
    options.UseSqlServer(connectionString));

// 3. 註冊使用者系統 (Identity)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    // 密碼設定寬鬆一點，方便測試
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AlbumContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. 開放跨域限制 (CORS) - 讓前端可以順利呼叫
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

var app = builder.Build();

// --- 關鍵兩行開始 ---
app.UseDefaultFiles(); // 這會讓 "/" 自動尋找 "index.html"
app.UseStaticFiles();  // 這才允許存取 wwwroot 內的靜態檔案
// --- 關鍵兩行結束 ---

// --- 5. 關鍵魔法：啟動時自動修復資料庫 ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AlbumContext>();
        // 這行會自動建立資料表！如果本地登入失敗，跑這行就會修好
        context.Database.Migrate();
        Console.WriteLine("✅ 資料庫遷移成功！");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ 資料庫遷移訊息: {ex.Message}");
    }
}
// ---------------------------------------

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();