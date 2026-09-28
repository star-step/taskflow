namespace TaskFlow.Api.Options;

/// <summary>Demo accounts created at startup when enabled (Development and Docker only).</summary>
public sealed class SeedOptions
{
    public const string Section = "Seed";

    public bool DemoUsers { get; set; }
    public List<DemoUser> Users { get; set; } = new();

    public sealed class DemoUser
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
