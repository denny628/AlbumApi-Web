using AlbumApi.Migrations; // 確保程式能抓到 Migrations 資料夾裡的檔案
using AlbumApi.Data;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // 確保這個命名空間在最上面
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 強制讓程式監聽 Railway 分配的 Port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 從 appsettings.json 讀取連線字串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 註冊資料庫服務，使用 MySql
// builder.Services.AddDbContext<AlbumContext>(options =>
// {
//     options.UseMySql(connectionString,
//         ServerVersion.AutoDetect(connectionString),
//         mySqlOptions => mySqlOptions.EnableRetryOnFailure());
// });
// 註冊資料庫服務，手動指定版本 (MariaDB 或 MySql 8.0)
builder.Services.AddDbContext<AlbumContext>(options =>
{
    // Railway 的 MySQL 基礎通常是 8.0.x
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 0)); 
    options.UseMySql(connectionString, serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure());
});

// 1. 註冊 Identity 服務 (調整為極簡模式)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // --- 密碼規則極簡化 ---
    options.Password.RequireDigit = false;             // 不需要數字
    options.Password.RequiredLength = 1;              // 長度只要 1 位以上即可
    options.Password.RequireNonAlphanumeric = false;    // 不需要特殊符號 (@#$!)
    options.Password.RequireUppercase = false;         // 不需要大寫字母
    options.Password.RequireLowercase = false;         // 不需要小寫字母
    options.Password.RequiredUniqueChars = 0;          // 不需要包含不同類型的字元

    // --- 使用者規則 ---
    options.User.RequireUniqueEmail = true;            // 確保 Email 不重複即可
})
.AddEntityFrameworkStores<AlbumContext>()
.AddDefaultTokenProviders();

// 設定 CORS 政策名稱
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 新增 CORS 服務
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


// 🚨 修正：只保留一次 AddControllers()
builder.Services.AddControllers();

// Add services to the container.
// Swagger 服務
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. 設定 HTTP 請求
var app = builder.Build();

// --- 加上這段：自動執行資料庫遷移 ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // try
    // {
        var context = services.GetRequiredService<AlbumContext>();
        context.Database.Migrate();
        Console.WriteLine("資料庫遷移成功！資料表已建立。");
    // }
    // catch (Exception ex)
    // {
    //     Console.WriteLine($"資料庫遷移失敗: {ex.Message}");
    // }
}
// ----------------------------------


// 放在最上面，確保流量被重導向 HTTPS (雖然目前在 HTTP 測試)
app.UseHttpsRedirection();

// 2. Swagger 應該在開發模式下盡早啟動
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. 啟用靜態檔案服務 (圖片和 index.html)
app.UseStaticFiles();

// 4. 路由選擇中介軟體
app.UseRouting();

// 5. 授權/安全相關中介軟體 (必須在 UseRouting 和 MapControllers 之間)
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication(); // 認證：判斷使用者是誰

app.UseAuthorization();


// 6. 控制器映射與端點執行 (執行路由系統選擇的端點)
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}