using CrmMida.Domain.Common;

namespace CrmMida.Domain.Security;

public sealed class User : BaseEntity
{
    private User() { }

    public User(string firstName, string lastName, string email, string passwordHash)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime? LastLoginAtUtc { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
}
