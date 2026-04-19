namespace CloudSoft.Auth.Web.Data;

public static class DummyUsers
{
    public record User(string Username, string Password, string[] Roles);

    public static readonly List<User> All =
    [
        new("admin", "admin", Roles: ["Admin"]),
        new("candidate", "candidate", Roles: ["Candidate"]),
    ];

    public static User? Find(string username, string password) =>
        All.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal) &&
            string.Equals(u.Password, password, StringComparison.Ordinal));
}
