using AlbumApi.Data;
using AlbumApi.Migrations; // <--- 關鍵修正 1：補上這個引用，才能找到 Migration 檔案
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 1. 設定 Port (對接 Railway)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 2. 讀取連線字串
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 3. 註冊資料庫服務 (關鍵修正 2：手動指定版本，防止啟動崩潰)
builder.Services.AddDbContext<AlbumContext>(options =>
{
    // 不要在雲端使用 AutoDetect，因為網路延遲會導致它誤判並崩潰
    // 我們直接告訴它：「你是 MySQL 8.0，不要懷疑」
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 0)); 
    
    options.UseMySql(connectionString, serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()); // 斷線重連機制
});

// 4. 註冊 Identity (極簡模式)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 0;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AlbumContext>()
.AddDefaultTokenProviders();

// 5. 設定 CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 關鍵修正 3：自動執行資料庫遷移 ---
// 這段程式碼必須在 app.Run() 之前執行
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine("正在嘗試連線資料庫並執行遷移...");
        var context = services.GetRequiredService<AlbumContext>();
        
        // 這行會檢查資料庫，如果沒有資料表就會自動建立
        // 因為上面改用了手動版本，這裡現在應該能順利執行了
        context.Database.Migrate(); 
        
        Console.WriteLine("✅ 資料庫遷移成功！資料表已建立。");
    }
    catch (Exception ex)
    {
        // 這裡會印出真正的錯誤原因 (例如密碼錯誤)
        Console.WriteLine($"❌ 資料庫遷移失敗: {ex.Message}");
    }
}
// ----------------------------------

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();