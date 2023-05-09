
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
        public DbSet<UserTask> UserTasks { get; set; }
        public DbSet<UserWorkspace> UserWorkspaces { get; set; }
        public DbSet<Label> Labels { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Workspace>(
            u =>{
                u.HasMany(u => u.Users).WithMany(w => w.Workspaces)
                .UsingEntity<UserWorkspace>(
                    u => u.HasOne<Account>(e => e.User).WithMany(e => e.UserWorkspaces),
                    w => w.HasOne<Workspace>(e => e.Workspace).WithMany(e => e.UserWorkspaces)
            );

            });

            modelBuilder.Entity<Card>(
                c=>{
                    c.HasOne(c => c.Workspace).WithMany(w => w.Cards).HasForeignKey(c => c.WorkspaceId);
                }
            );

            modelBuilder.Entity<TaskItem>(
                t=>{
                    t.HasOne(t => t.Card).WithMany(c => c.TaskItems).HasForeignKey(t => t.CardId);

                    t.HasMany(u => u.Labels).WithMany(w => w.TaskItems)
                    .UsingEntity<TaskLabel>(
                        tl => tl.HasOne<Label>(e => e.Label).WithMany(e => e.TaskLabels),
                        tl => tl.HasOne<TaskItem>(e => e.TaskItem).WithMany(e => e.TaskLabels));

                    t.HasMany(u => u.Users).WithMany(w => w.TaskItems)
                    .UsingEntity<UserTask>(
                        u => u.HasOne<Account>(e => e.User).WithMany(e => e.UserTasks),
                        w => w.HasOne<TaskItem>(e => e.TaskItem).WithMany(e => e.UserTasks));
                }
            );

            modelBuilder.Entity<Activation>(
                a=>{
                    a.HasOne(a => a.Workspace).WithMany(w=>w.Activations).HasForeignKey(a => a.WorkspaceId);
                    a.HasOne(a => a.User).WithMany(w=>w.Activations).HasForeignKey(a => a.UserId);
                }
            );

        }
    }
}
