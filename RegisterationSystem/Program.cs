
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RegisterationSystem.Models;
using RegisterationSystem.Models.View_Models;
using RegisterationSystem.View_Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;

namespace RegisterationSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped(DataAccess => new DataAccess(builder.Configuration.GetConnectionString("DefaultConnection")!));

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = jwtSettings["Key"];

            builder.Services.AddAuthentication(opitons =>
            {
                opitons.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opitons.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            });

            builder.Services.AddAuthorization();

            builder.Services.AddAuthentication();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/api/signup", async (DataAccess dataAccess, [FromBody] SignUpViewModel model) =>
            {
                if (string.IsNullOrEmpty(model.Name))
                    return Results.BadRequest("Name is required.");

                if (string.IsNullOrEmpty(model.Email) || !model.Email.EndsWith("@stud.fci-cu.edu.eg"))
                    return Results.BadRequest("Email must be like studentID@stud.fci-cu.edu.eg");

                if (string.IsNullOrEmpty(model.Id) || !model.Email.StartsWith(model.Id + "@"))
                    return Results.BadRequest("Student ID must match the email prefix.");

                if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 8 || !model.Password.Any(char.IsDigit))
                    return Results.BadRequest("Password must be at least 8 characters and contain at least one number.");

                if (model.Password != model.ConfirmPassword)
                    return Results.BadRequest("Passwords do not match.");

                var studentExists = dataAccess.GetStudentById(model.Id);
                if (studentExists != null)
                    return Results.BadRequest("Student already exists.");

                if (model.Level < 0 || (int)model.Level! > 4)
                    return Results.BadRequest("Invalid Level.");

                if ((int)model.Gender! > 2)
                    return Results.BadRequest("Invalid Gender.");

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var student = new Student
                {
                    Id = model.Id,
                    Name = model.Name,
                    Gender = model.Gender,
                    Email = model.Email,
                    Level = model.Level,
                    PasswordHash = passwordHash,
                    PhotoPath = model.Photo
                };


                var success = dataAccess.AddStudent(student);
                if (success)
                    return Results.Ok("Signup successful!");
                else
                    return Results.BadRequest("Failed to add student.");
            })
            .WithName("SignUp")
            .WithOpenApi();



            app.MapPost("/api/signin", async (DataAccess dataAccess, IConfiguration config, [FromBody] SignInViewModel model) =>
            {
                if (string.IsNullOrEmpty(model.Email) || !model.Email.EndsWith("@stud.fci-cu.edu.eg"))
                    return Results.BadRequest("Email must be like StudentId@stud.fci-cu.edu.eg");

                var student = dataAccess.GetStudentByEmail(model.Email);
                if (student == null || !BCrypt.Net.BCrypt.Verify(model.Password, student.PasswordHash))
                    return Results.BadRequest("Invalid email or password.");

                // Generate JWT token
                var jwtSettings = config.GetSection("Jwt");
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

                return Results.Ok(new { Message = "Login successful!", Token = token });
            })
            .WithName("SignIn")
            .WithOpenApi();


            app.MapGet("/api/student/{id}", async (DataAccess dataAccess, string id) =>
            {
            var student = dataAccess.GetStudentById(id);
            if (student == null)
                return Results.NotFound("Student not found.");

            var ProfileStudent = new ProfileStudentViewModel
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Level = student.Level,
                Gender = student.Gender
            };

                return Results.Ok(ProfileStudent);
            })
            .WithName("GetStudentById")
            .WithOpenApi();

            app.Run();
        }
    }
}
