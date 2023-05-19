
namespace TaskManager.API.Data.DTOs
{
    public class MoveTaskDto
    {
        public int Id { get; set; }
        public Dictionary<string, int> Before { get; set; }
        public Dictionary<string, int> After { get; set; }
        
    }
}