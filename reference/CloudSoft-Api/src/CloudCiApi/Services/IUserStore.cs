using CloudCiApi.Models;

namespace CloudCiApi.Services;

public interface IUserStore
{
    User? Validate(string username, string password);
}
