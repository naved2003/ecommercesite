namespace ecommercesite.Models
{
    /// <summary>
    /// View model for the global error page.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}