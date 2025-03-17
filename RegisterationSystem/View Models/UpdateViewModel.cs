using RegisterationSystem.Models;

namespace RegisterationSystem.View_Models
{
    public class UpdateViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Gender? Gender { get; set; }
        public Level? Level { get; set; }
        public IFormFile? Photo { get; set; }

    }
}
