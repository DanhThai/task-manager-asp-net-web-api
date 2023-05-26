using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class MemberTask
    {
        [Key]
        public int Id { get; set; }
        public bool Requested { get; set; } = false;
        public DateTime? ExtendDate { get; set;}
        public string UserId { get; set; }
        public int TaskItemId { get; set; }
        public Account User { get; set; }
        public TaskItem TaskItem { get; set; }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is MemberTask))
                return false;
            else
            {
                var other = (MemberTask)obj;
                return Id == other.Id && UserId == other.UserId && TaskItemId == other.TaskItemId;
            }
        }

    }
}