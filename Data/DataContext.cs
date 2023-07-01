
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Data
{
    public class DataContext: IdentityDbContext<Account>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Activation> Activations { get; set; }
        public DbSet<MemberTask> MemberTasks { get; set; }
        public DbSet<MemberWorkspace> MemberWorkspaces { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<TaskLabel> TaskLabels { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Subtask> Subtasks { get; set; }
        public DbSet<Schedule> Schedules { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Workspace>(
            w =>{
                w.HasMany(w => w.Users).WithMany(u => u.Workspaces)
                .UsingEntity<MemberWorkspace>(
                    u => u.HasOne<Account>(e => e.User).WithMany(e => e.MemberWorkspaces),
                    w => w.HasOne<Workspace>(e => e.Workspace).WithMany(e => e.MemberWorkspaces)
                );
                w.HasMany(w => w.Schedules).WithOne(s => s.Workspace).HasForeignKey(s => s.WorkspaceId);
            });

            modelBuilder.Entity<Card>(
                c=>{
                    c.HasOne(c => c.Workspace).WithMany(w => w.Cards).HasForeignKey(c => c.WorkspaceId);
                }
            );
            modelBuilder.Entity<Label>(
                c=>{
                    c.HasOne(c => c.Workspace).WithMany(w => w.Labels).HasForeignKey(c => c.WorkspaceId);
                }
            );

            modelBuilder.Entity<TaskItem>(
                t=>{
                    t.HasOne(t => t.Card).WithMany(c => c.TaskItems).HasForeignKey(t => t.CardId);
                    t.HasOne(t => t.Creator).WithMany(u => u.TaskItems).HasForeignKey(t => t.CreatorId);

                    t.HasMany(u => u.Labels).WithMany(w => w.TaskItems)
                    .UsingEntity<TaskLabel>(
                        tl => tl.HasOne<Label>(e => e.Label).WithMany(e => e.TaskLabels),
                        tl => tl.HasOne<TaskItem>(e => e.TaskItem).WithMany(e => e.TaskLabels));
                    t.HasMany(t => t.Subtasks).WithOne(s => s.TaskItem).HasForeignKey(s => s.TaskItemId);
                }
            );

            modelBuilder.Entity<Activation>(
                a=>{
                    a.HasOne(a => a.Workspace).WithMany(w=>w.Activations).HasForeignKey(a => a.WorkspaceId);
                    a.HasOne(a => a.User).WithMany(w=>w.Activations).HasForeignKey(a => a.UserId);
                }
            );

            modelBuilder.Entity<Comment>(
                c=>{
                    c.HasOne(c => c.TaskItem).WithMany(t=>t.Comments).HasForeignKey(c => c.TaskItemId);
                    c.HasOne(c => c.User).WithMany(t=>t.Comments).HasForeignKey(c => c.UserId);
                }
            );
            modelBuilder.Entity<Schedule>(
                c=>{
                    c.HasOne(c => c.Creator).WithMany(t=>t.Schedules).HasForeignKey(c => c.CreatorId);
                }
            );

            modelBuilder.Entity<MemberTask>(
                m=>{
                    m.HasOne(c => c.TaskItem).WithMany(t=>t.MemberTasks).HasForeignKey(c => c.TaskItemId);
                    m.HasOne(c => c.User).WithMany(t=>t.MemberTasks).HasForeignKey(c => c.UserId);
                }
            );

        }
    }
}
