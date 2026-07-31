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
        PasswordChangedAtUtc = DateTime.UtcNow;
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime? PasswordChangedAtUtc { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    public bool CanLogin(DateTime utcNow) =>
        IsActive && (!LockedUntilUtc.HasValue || LockedUntilUtc.Value <= utcNow);

    public void RegisterSuccessfulLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterFailedLogin(int maximumAttempts, TimeSpan lockDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maximumAttempts)
        {
            LockedUntilUtc = DateTime.UtcNow.Add(lockDuration);
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
