using Microsoft.EntityFrameworkCore;

namespace AiBusinessDirectory.Web.Data;

public sealed class ListingDbContext(DbContextOptions<ListingDbContext> options) : DbContext(options)
{
    public DbSet<ListingRequest> ListingRequests => Set<ListingRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListingRequest>().HasKey(request => request.Id);
        modelBuilder.Entity<ListingRequest>().HasIndex(request => request.Status);
    }
}

public sealed class ListingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BusinessName { get; set; } = "";
    public string OfficialWebsite { get; set; } = "";
    public string Country { get; set; } = "";
    public string BusinessCategory { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string Description { get; set; } = "";
    public string? AdditionalNotes { get; set; }
    public string Status { get; set; } = "Pending";
    public bool ConsentGiven { get; set; }
    public string ConsentVersion { get; set; } = "listing-request-v1";
    public string ConsentText { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime ConsentAtUtc { get; set; }
}
