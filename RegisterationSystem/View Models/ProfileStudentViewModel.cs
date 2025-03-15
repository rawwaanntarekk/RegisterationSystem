using RegisterationSystem.Models;

namespace RegisterationSystem.View_Models
{
    public class ProfileStudentViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public Gender? Gender { get; set; }
        public Level? Level { get; set; }
        public byte[] Photo { get; set; }
    }

}
