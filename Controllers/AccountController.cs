using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlbumApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 定義接收前端資料的模型
        public class AuthModel
        {
            public string? Email { get; set; } // 註冊用
            public string Password { get; set; } = string.Empty;
            public string Nickname { get; set; } = string.Empty; // 登入與註冊的主鍵
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] AuthModel model)
        {
            // 如果 Email 沒填，自動生成一個假的 email 避免 Identity 報錯
            var email = string.IsNullOrEmpty(model.Email) ? $"{model.Nickname}@local.test" : model.Email;
            
            var user = new IdentityUser { UserName = model.Nickname, Email = email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "註冊成功" });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] AuthModel model)
        {
            // 🚨 修正：改用 FindByNameAsync (透過暱稱尋找使用者)
            var user = await _userManager.FindByNameAsync(model.Nickname);
            if (user == null)
            {
                return Unauthorized(new { Message = "登入失敗：找不到此暱稱" });
            }

            // 驗證密碼
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: true, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Ok(new { Message = "登入成功", User = user.UserName });
            }
            return Unauthorized(new { Message = "登入失敗：密碼錯誤" });
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.Select(u => u.UserName).ToListAsync();
            return Ok(users);
        }
    }
}