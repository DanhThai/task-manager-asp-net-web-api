using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TaskManager.API.Data.Models
{
    public class Card
    {
        public Card(string name, int code)
        {
            Name = name;
            Code = code;
        }

        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        public int Code { get; set; }
        public int TaskQuantity { get; set;} = 0;
        [MaxLength(256)]
        public string TaskOrder { get; set; } = "";
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        // Relationship
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();

        
    }
}