
using System.Text.Json.Serialization;
// using Newtonsoft.Json;

namespace TaskManager.API.Data.DTOs
{
    public class CardDto
    {
        public int Id { get; internal set; }
        public string Name { get; set; }
        public int Code { get; set; }
        [JsonIgnore]
        public string TaskOrder { get; set; }
        public List<int> ListTasksOrder { get; set; }
        public List<TaskItemDto> TaskItems { get; internal set; } = new List<TaskItemDto>();
    }
}