namespace LawyerCustomerApp.External.Jwt.Common.Models;

public class JwtConfiguration
{
    public enum Roles
    {
        User,
        Administrator
    }

    public required string UserId { get; init; }
    public required string RoleId { get; init; }

    public required string NameIdentifier { get; init; }
    public required string Email { get; init; }
    public required Roles Role { get; init; }

    public required TimeSpecificationProperties TimeSpecification { get; init; }
    public class TimeSpecificationProperties
    {
        public enum Types
        {
            Second,
            Minute,
            Hour,
            Day,
            Month,
            Year
        }

        public required DateTime Base { get; init; }
        public required int Quantity { get; init; }
        public required Types Type { get; init; }
    }
}
