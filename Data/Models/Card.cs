using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TaskManager.API.Data.Models
{
    public enum CARD_CODE_ENUM{
        Todos = 0,
        InProgress = 1,
        Completed = 2,
    }
    public class Card
    {

        public Card(string name, CARD_CODE_ENUM code)
        {
            Name = name;
            Code = code;
        }

        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        public CARD_CODE_ENUM Code { get; set; }
        public int TaskQuantity { get; set;} = 0;
        [MaxLength(256)]
        public string TaskOrder { get; set; } = "";

        // Relationship
        [Required]
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();

        
    }
}