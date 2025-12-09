using UtilityTools.Domain.Common;

namespace UtilityTools.Domain.Entities;

/// <summary>
/// Role entity for RBAC
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ICollection<User> Users { get; private set; } = new List<User>();

    private Role() { }

    public Role(string name, string description)
    {
        Name = name;
        Description = description;
        UpdateTimestamp();
    }
}

