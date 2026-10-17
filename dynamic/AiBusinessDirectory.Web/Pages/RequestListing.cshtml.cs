using System.ComponentModel.DataAnnotations;
using AiBusinessDirectory.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AiBusinessDirectory.Web.Pages;

public sealed class RequestListingModel(ListingDbContext db, ILogger<RequestListingModel> logger) : PageModel
{
    public const string ConsentStatement = "I confirm that I am authorized to request this business listing, that the information is accurate to the best of my knowledge, and that AI Business Directory may review it and contact me about this request.";

    [BindProperty] public ListingInput Input { get; set; } = new();
    [TempData] public bool Submitted { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!string.IsNullOrEmpty(Input.WebsiteFax))
            ModelState.AddModelError(string.Empty, "This request could not be accepted.");
        if (!Input.Consent)
            ModelState.AddModelError("Input.Consent", "Please confirm your authorization and the accuracy of the information.");
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        var now = DateTime.UtcNow;
        db.ListingRequests.Add(new ListingRequest
        {
            BusinessName = Input.BusinessName.Trim(),
            OfficialWebsite = Input.OfficialWebsite.Trim(),
            Country = Input.Country.Trim(),
            BusinessCategory = Input.BusinessCategory.Trim(),
            ContactEmail = Input.ContactEmail.Trim(),
            Description = Input.Description.Trim(),
            AdditionalNotes = Input.AdditionalNotes?.Trim(),
            Status = "Pending",
            ConsentGiven = true,
            ConsentText = ConsentStatement,
            SubmittedAtUtc = now,
            ConsentAtUtc = now
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            logger.LogError("A listing request could not be stored. No request contents were logged.");
            ModelState.AddModelError(string.Empty, "Your request could not be saved. Please try again later.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }
        Submitted = true;
        return RedirectToPage("RequestListing");
    }
}

public sealed class ListingInput
{
    [Required, StringLength(160), Display(Name = "Business name")]
    public string BusinessName { get; set; } = "";

    [Required, StringLength(2048), HttpWebsite, Display(Name = "Official website URL")]
    public string OfficialWebsite { get; set; } = "";

    [Required, StringLength(100)] public string Country { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Business category")]
    public string BusinessCategory { get; set; } = "";

    [Required, EmailAddress, StringLength(254), Display(Name = "Contact email")]
    public string ContactEmail { get; set; } = "";

    [Required, StringLength(1000), Display(Name = "Short business description")]
    public string Description { get; set; } = "";

    [StringLength(2000), Display(Name = "Additional notes")]
    public string? AdditionalNotes { get; set; }
    public bool Consent { get; set; }
    [StringLength(200)] public string? WebsiteFax { get; set; }
}

public sealed class HttpWebsiteAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return ValidationResult.Success;
        return Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == "https" || uri.Scheme == "http") && uri.Host.Length > 0 &&
            uri.UserInfo.Length == 0 && !text.Any(char.IsWhiteSpace)
                ? ValidationResult.Success
                : new ValidationResult("Enter a complete http:// or https:// website address without embedded credentials.");
    }
}
