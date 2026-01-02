using System.ComponentModel.DataAnnotations;

namespace AlbumApi.Models
{
    public class Album
    {
        public int Id { get; set; } // 全域唯一 ID (資料庫主鍵，不顯示給使用者看)
        
        // 🔥 新增：使用者的個人編號 (顯示用，例如 popopi 的第 1 號)
        public int LocalId { get; set; } 

        public string Artist { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string? CoverFileName { get; set; }

        // 🔥 新增：借出給誰 (null 代表在庫，有值代表借出)
        public string? LentTo { get; set; } 
    }
}