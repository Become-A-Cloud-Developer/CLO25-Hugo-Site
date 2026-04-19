namespace CloudSoft.Auth.Web.Data;

public static class DummyUsers
{
    public record User(string Username, string Password);

    public static readonly List<User> All =
    [
        new("admin", "admin"),
        new("candidate", "candidate"),
    ];

    public static User? Find(string username, string password) =>
        All.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal) &&
            string.Equals(u.Password, password, StringComparison.Ordinal));
}
