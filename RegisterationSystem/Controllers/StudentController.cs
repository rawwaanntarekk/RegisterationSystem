using Microsoft.AspNetCore.Mvc;
using RegisterationSystem.Models;
using RegisterationSystem.Models.View_Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using RegisterationSystem.View_Models;
using Microsoft.AspNetCore.Authorization;

namespace RegisterationSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController(DataAccess dataAccess, IConfiguration config) : ControllerBase
    {
        private readonly DataAccess _dataAccess = dataAccess;
        private readonly IConfiguration _config = config;

        [HttpPost("signup")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SignUp([FromForm] SignUpViewModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
                return BadRequest("Name is required.");

            if (string.IsNullOrEmpty(model.Email) || !model.Email.EndsWith("@stud.fci-cu.edu.eg"))
                return BadRequest("Email must be like studentID@stud.fci-cu.edu.eg");

            if (string.IsNullOrEmpty(model.Id) || !model.Email.StartsWith(model.Id + "@"))
                return BadRequest("Student ID must match the email prefix.");

            if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 8 || !model.Password.Any(char.IsDigit))
                return BadRequest("Password must be at least 8 characters and contain at least one number.");

            if (model.Password != model.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var studentExists = _dataAccess.GetStudentById(model.Id);
            if (studentExists != null)
                return BadRequest("Student already exists.");

            if (model.Level < 0 || (int)model.Level! > 4)
                return BadRequest("Invalid Level.");

            if ((int)model.Gender! > 2)
                return BadRequest("Invalid Gender.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);



            var student = new Student
            {
                Id = model.Id,
                Name = model.Name,
                Gender = model.Gender,
                Email = model.Email,
                Level = model.Level,
                PasswordHash = passwordHash,

            };

            var success = _dataAccess.AddStudent(student);
            if (success)
                return Ok(new
                {
                    success = true,
                    message = "Signup successful!",
                    data = new
                    {
                        id = student.Id,
                        name = student.Name,
                        email = student.Email,
                        photoPath = student.PhotoPath
                    }
                });
            else
                return Ok(new { success = false, message = "Failed to add student.", data = (object)null! });

        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || !model.Email.EndsWith("@stud.fci-cu.edu.eg"))
                return BadRequest("Email must be like StudentId@stud.fci-cu.edu.eg");

            var student = _dataAccess.GetStudentByEmail(model.Email);
            if (student == null || !BCrypt.Net.BCrypt.Verify(model.Password, student.PasswordHash))
                return BadRequest("Invalid email or password.");

            // Generate JWT token
            var jwtSettings = _config.GetSection("Jwt");
            var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims:
                [
                    new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
                    new Claim(ClaimTypes.Email, student.Email)
                ],
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                    SecurityAlgorithms.HmacSha256Signature)
            ));

            return Ok(new
            {
                Success = true,
                message = "Signin successful!",
                data = new { token, id = student.Id, email = student.Email }
            });
        }

        [HttpPost("update")]
        [Authorize]
        public async Task<IActionResult> UpdateStudent(UpdateViewModel model)
        {
            var student = _dataAccess.GetStudentById(model.Id);

            if (student == null)
                return NotFound("Student not found.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != student.Id)
                return Unauthorized("You are not authorized to update this student.");

            string photoPath = null;
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Photo.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "wwwroot/uploads");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }
                photoPath = $"/uploads/{fileName}";
            }


            student.Name = model.Name;
            student.Level = model.Level;
            student.Gender = model.Gender;
            student.PhotoPath = photoPath!;

            var success = _dataAccess.UpdateStudent(student);


            if (success)
                return Ok(
                    new
                    {
                        success = true,
                        message = "Student updated successfully!",
                        data = new
                        {
                            id = student.Id,
                            name = student.Name,
                            email = student.Email,
                            photoPath = student.PhotoPath,
                            level = student.Level,
                            gender = student.Gender
                        }
                    }
                    );
            else
                return Ok(new { success = false, message = "Failed to update student.", data = (object) null! });
        }


        [HttpGet("{id}")]
        public IActionResult GetStudentById(string id)
        {
            var student = _dataAccess.GetStudentById(id);
            if (student == null)
                return NotFound("Student not found.");

            var ProfileStudent = new ProfileStudentViewModel
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Level = student.Level,
                Gender = student.Gender,
                PhotoPath = student.PhotoPath

            };

            return Ok(new
            {
                success = true,
                message = "Student data retrieved successfully!",
                data = ProfileStudent
            });
        }
    }
}