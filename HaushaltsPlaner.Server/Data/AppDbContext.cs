using Microsoft.EntityFrameworkCore;
using HaushaltsPlaner.Shared.Models;

namespace HaushaltsPlaner.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Core Entities
    public DbSet<User> Users { get; set; }
    public DbSet<Household> Households { get; set; }

    // Feature Entities
    public DbSet<TodoList> TodoLists { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(u => u.Household)
        .WithMany(h => h.Members)
   .HasForeignKey(u => u.HouseholdId)
          .OnDelete(DeleteBehavior.SetNull);
            });

        // Household Configuration
        modelBuilder.Entity<Household>(entity =>
   {
       entity.HasKey(e => e.Id);
       entity.HasIndex(e => e.Name);
   });

        // TodoList Configuration
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(tl => tl.Household)
          .WithMany()
          .HasForeignKey(tl => tl.HouseholdId)
        .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tl => tl.CreatedBy)
                .WithMany()
                   .HasForeignKey(tl => tl.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        });

        // TodoItem Configuration
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(ti => ti.TodoList)
                  .WithMany(tl => tl.Items)
             .HasForeignKey(ti => ti.TodoListId)
                 .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ti => ti.AssignedTo)
      .WithMany()
          .HasForeignKey(ti => ti.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ti => ti.CompletedBy)
          .WithMany()
         .HasForeignKey(ti => ti.CompletedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        });

        // CalendarEvent Configuration
        modelBuilder.Entity<CalendarEvent>(entity =>
     {
         entity.HasKey(e => e.Id);

         entity.HasOne(ce => ce.Household)
    .WithMany()
.HasForeignKey(ce => ce.HouseholdId)
  .OnDelete(DeleteBehavior.Cascade);

         entity.HasOne(ce => ce.AssignedTo)
   .WithMany()
        .HasForeignKey(ce => ce.AssignedToUserId)
     .OnDelete(DeleteBehavior.SetNull);

         entity.HasOne(ce => ce.CreatedBy)
      .WithMany()
   .HasForeignKey(ce => ce.CreatedByUserId)
   .OnDelete(DeleteBehavior.SetNull);

         entity.HasIndex(e => e.StartDate);
         entity.HasIndex(e => new { e.HouseholdId, e.StartDate });
     });

        // Message Configuration
        modelBuilder.Entity<Message>(entity =>
    {
        entity.HasKey(e => e.Id);

        entity.HasOne(m => m.Household)
      .WithMany()
 .HasForeignKey(m => m.HouseholdId)
 .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(m => m.Sender)
         .WithMany()
         .HasForeignKey(m => m.SenderUserId)
          .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(m => m.Recipient)
         .WithMany()
                      .HasForeignKey(m => m.RecipientUserId)
          .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(e => e.CreatedAt);
        entity.HasIndex(e => e.IsRead);
    });

        // MealPlan Configuration
        modelBuilder.Entity<MealPlan>(entity =>
{
    entity.HasKey(e => e.Id);

    entity.HasOne(mp => mp.Household)
      .WithMany()
                .HasForeignKey(mp => mp.HouseholdId)
     .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(mp => mp.CreatedBy)
 .WithMany()
  .HasForeignKey(mp => mp.CreatedByUserId)
 .OnDelete(DeleteBehavior.SetNull);

    entity.HasOne(mp => mp.AssignedTo)
      .WithMany()
      .HasForeignKey(mp => mp.AssignedToUserId)
        .OnDelete(DeleteBehavior.SetNull);

    entity.HasIndex(e => e.Date);
    entity.HasIndex(e => new { e.HouseholdId, e.Date });
});

        // Photo Configuration
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(p => p.Household)
             .WithMany()
               .HasForeignKey(p => p.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.UploadedBy)
                 .WithMany()
                   .HasForeignKey(p => p.UploadedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt);
        });

        // Video Configuration
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(v => v.Household)
            .WithMany()
           .HasForeignKey(v => v.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.UploadedBy)
      .WithMany()
  .HasForeignKey(v => v.UploadedByUserId)
          .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedAt);
        });

        // Recipe configuration
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(r => r.Household)
        .WithMany()
     .HasForeignKey(r => r.HouseholdId)
    .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.CreatedBy)
      .WithMany()
        .HasForeignKey(r => r.CreatedByUserId)
   .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.HouseholdId, e.Name });
        });

        // RecipeIngredient configuration
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(ri => ri.Recipe)
   .WithMany(r => r.Ingredients)
 .HasForeignKey(ri => ri.RecipeId)
     .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.RecipeId, e.SortOrder });
        });
    }
}
