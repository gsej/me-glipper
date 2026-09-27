namespace Api.Controllers.Models;

public class UserEntity
{
    public UserEntity()
    {
    }

    public UserEntity(string userId, int heightInCm)
    {
        UserId = userId;
        HeightInCm = heightInCm;
    }

    public string UserId { get; init; } = null!;

    public int HeightInCm { get; set; }
}
