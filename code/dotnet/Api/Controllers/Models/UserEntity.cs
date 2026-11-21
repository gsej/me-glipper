namespace Api.Controllers.Models;

public class UserEntity
{
    public UserEntity()
    {
    }

    public UserEntity(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; init; } = null!;
}
