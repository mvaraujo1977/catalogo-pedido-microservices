namespace Orders.Application.Common;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsInRole(string role);
}
