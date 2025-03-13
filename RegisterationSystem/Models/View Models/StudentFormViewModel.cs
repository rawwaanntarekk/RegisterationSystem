namespace RegisterationSystem.Models
{
    public class StudentFormViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public Gender? Gender { get; set; }
        public Level? Level { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string? Photo { get; set; }
    }
}
