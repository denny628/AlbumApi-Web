using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlbumApi.Data;
using AlbumApi.Models;

namespace AlbumApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlbumsController : ControllerBase
    {
        private readonly AlbumContext _context;

        public AlbumsController(AlbumContext context)
        {
            _context = context;
        }

        // 定義上傳模型
        public class AlbumUploadModel
        {
            public string? Title { get; set; }
            public string? Artist { get; set; }
            public int ReleaseYear { get; set; }
            public string? Owner { get; set; }
            public IFormFile? CoverImage { get; set; }
            public string? LentTo { get; set; } // 新增：借出欄位
        }

        // 定義 CSV 匯入模型 (參數用)
        public class ImportModel
        {
            public IFormFile? File { get; set; }
            public string Owner { get; set; } = string.Empty;
            public int Year { get; set; }
        }

        // GET: api/albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbums(
            [FromQuery] string? search,
            [FromQuery] string? owner,
            [FromHeader(Name = "X-Current-User")] string? currentUser) // 從 Header 抓當前登入者
        {
            var query = _context.Albums.AsQueryable();

            // 1. 關鍵字搜尋
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(search) ||
                    a.Artist.ToLower().Contains(search)
                );
            }

            // 2. 擁有者篩選邏輯
            if (!string.IsNullOrWhiteSpace(owner) && owner != "ALL")
            {
                if (owner == "BORROWED_BY_ME")
                {
                    // 🔥 特殊模式：查詢「我借來的」
                    if (string.IsNullOrEmpty(currentUser)) return BadRequest("需要登入才能查詢借入項目");
                    query = query.Where(a => a.LentTo == currentUser);
                }
                else
                {
                    // 一般模式：查詢某人的收藏
                    query = query.Where(a => a.Owner == owner);
                }
            }

            // 排序：先排擁有者，再排個人編號
            query = query.OrderBy(a => a.Owner).ThenBy(a => a.LocalId);

            return await query.ToListAsync();
        }

        // POST: api/albums (新增)
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<Album>> PostAlbum([FromForm] AlbumUploadModel model)
        {
            // 1. 計算 LocalId (該擁有者的最大號碼 + 1)
            // 注意：如果有併發請求可能需要 Lock，但這裡簡化處理
            int nextLocalId = 1;
            var userAlbums = _context.Albums.Where(a => a.Owner == model.Owner);
            if (userAlbums.Any())
            {
                nextLocalId = await userAlbums.MaxAsync(a => a.LocalId) + 1;
            }

            // 2. 處理圖片
            string? savedFileName = null;
            if (model.CoverImage != null && model.CoverImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileExtension = Path.GetExtension(model.CoverImage.FileName);
                savedFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, savedFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CoverImage.CopyToAsync(fileStream);
                }
            }

            var album = new Album
            {
                LocalId = nextLocalId, // 🔥 設定個人編號
                Title = model.Title!,
                Artist = model.Artist!,
                ReleaseYear = model.ReleaseYear,
                Owner = model.Owner!,
                CoverFileName = savedFileName,
                LentTo = string.IsNullOrWhiteSpace(model.LentTo) ? null : model.LentTo // 處理借出
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return Ok(album);
        }

        [HttpPut("{id}")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PutAlbum(int id, [FromForm] AlbumUploadModel model, [FromHeader(Name = "X-Current-User")] string? requester)
        {
            // 1. 檢查有無傳入當前使用者 (Header)
            if (string.IsNullOrEmpty(requester))
            {
                return BadRequest(new { Message = "Header 遺失 X-Current-User" });
            }

            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            // 2. 權限檢查：只有本人或 denny 可以修改
            if (!string.Equals(album.Owner, requester, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(requester, "denny", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { Message = "權限不足，只有本人或 denny 可以修改" });
            }

            try
            {
                // 3. 更新內容 (若前端沒傳 Title 則保持原樣)
                album.Title = model.Title ?? album.Title;
                album.Artist = model.Artist ?? album.Artist;
                album.ReleaseYear = model.ReleaseYear;

                // 4. 處理借出狀態：如果是空字串就存 null
                album.LentTo = string.IsNullOrWhiteSpace(model.LentTo) ? null : model.LentTo;

                // 5. 處理圖片更新
                if (model.CoverImage != null && model.CoverImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string fileExtension = Path.GetExtension(model.CoverImage.FileName);
                    string savedFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, savedFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(fileStream);
                    }
                    album.CoverFileName = savedFileName;
                }

                await _context.SaveChangesAsync();
                return NoContent(); // 成功回傳 204
            }
            catch (Exception ex)
            {
                // 捕捉詳細的資料庫報錯
                return StatusCode(500, new { Message = $"資料庫錯誤: {ex.Message}" });
            }
        }

        // DELETE: api/albums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id, [FromHeader(Name = "X-Current-User")] string requester)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            // 🔥 權限檢查
            if (!string.Equals(album.Owner, requester, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(requester, "denny", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(403, new { Message = "權限不足：您不能刪除別人的收藏" });
            }

            // 刪除圖片
            if (!string.IsNullOrEmpty(album.CoverFileName))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", album.CoverFileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("Import")]
        public async Task<IActionResult> Import([FromForm] IFormFile file, [FromForm] string owner, [FromForm] int year)
        {
            if (file == null || file.Length == 0) return BadRequest("無檔案");

            try
            {
                // 🔥 重點：先查出這個擁有者目前最大的 LocalId 是多少
                int currentMaxLocalId = await _context.Albums
                    .Where(a => a.Owner == owner)
                    .Select(a => (int?)a.LocalId)
                    .MaxAsync() ?? 0;

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var newAlbums = new List<Album>();
                    // 如果 CSV 有標題列才需要 ReadLineAsync()，你的檔案看起來直接就是資料
                    // await reader.ReadLineAsync(); 

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');
                        if (values.Length >= 2)
                        {
                            currentMaxLocalId++; // 🔥 序號往後累加
                            newAlbums.Add(new Album
                            {
                                LocalId = currentMaxLocalId, // 賦予個人編號
                                Artist = values[0].Trim(),
                                Title = values[1].Trim(),
                                ReleaseYear = year,
                                Owner = owner,
                                CoverFileName = null,
                                LentTo = null
                            });
                        }
                    }
                    _context.Albums.AddRange(newAlbums);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { Message = "匯入成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"錯誤: {ex.Message}");
            }
        }
    }
}