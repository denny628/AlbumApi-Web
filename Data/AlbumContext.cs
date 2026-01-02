using Microsoft.AspNetCore.Identity.EntityFrameworkCore; 
using Microsoft.EntityFrameworkCore;
using AlbumApi.Models; // 引入您剛建立的 Album 類別

namespace AlbumApi.Data
{
    // AlbumContext 繼承自 DbContext，它負責所有資料庫的操作。
    public class AlbumContext : IdentityDbContext
    {
        // 建構子，接收設定選項並傳給基底類別
        public AlbumContext(DbContextOptions<AlbumContext> options)
            : base(options)
        {
        }

        // Dbset 代表資料庫中的一張資料表
        // 我們將 Album 類別映射到名為 "Albums" 的資料表
        public DbSet<Album> Albums { get; set; } = default!;

        // (可選) 在這裡可以進行更進階的資料庫模型設定
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // 🚨 這行非常重要，用來設定 Identity 的關聯
        }
    }
}