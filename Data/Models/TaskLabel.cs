

namespace TaskManager.API.Data.Models
{
    public class TaskLabel
    {
        public int TaskItemId { get; set; }
        public int LabelId { get; set; }
        public TaskItem TaskItem { get; set; }
        public Label Label { get; set; }
    }
}