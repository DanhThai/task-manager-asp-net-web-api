
using System.Text.Json.Serialization;
// using Newtonsoft.Json;

namespace TaskManager.API.Data.DTOs
{
    public class CardDto
    {
        public int Id { get; internal set; }
        public string Name { get; set; }
        public int Code { get; set; }
        public string TaskOrder { get; internal set; }
        public int TaskQuantity { get; set;}
        public List<int> ListTaskIdOrder { get; internal set; } = null;
        public List<TaskItemDto> TaskItems { get; internal set; } = new List<TaskItemDto>();
    }
}