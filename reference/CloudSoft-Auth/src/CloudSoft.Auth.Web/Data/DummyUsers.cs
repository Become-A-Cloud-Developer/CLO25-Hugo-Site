namespace CloudSoft.Auth.Web.Data;

public static class DummyUsers
{
    public record User(
        string Username,
        string Password,
        string[] Roles,
        Dictionary<string, string> Claims);

    public static readonly List<User> All =
    [
        new("admin", "admin",
            Roles: ["Admin"],
            Claims: new() { ["Department"] = "Engineering" }),

        new("candidate", "candidate",
            Roles: ["Candidate"],
            Claims: new() { ["Department"] = "Sales" }),
    ];

    public static User? Find(string username, string password) =>
        All.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.Ordinal) &&
            string.Equals(u.Password, password, StringComparison.Ordinal));
}
