namespace RegisterationSystem.Models
{
    public class Student
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail => Email.ToUpper();
        public Gender? Gender { get; set; }
        public Level? Level { get; set; }
        public string PasswordHash { get; set; }
        public string ConfirmPassword { get; set; }
        public string PhotoPath { get; set; }


    }
}
