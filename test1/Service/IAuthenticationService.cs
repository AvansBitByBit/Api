namespace test1.Service;

public interface IAuthenticationService
{
    /// <summary>
    /// Returns the user name of the authenticated user
    /// </summary>
    /// <returns></returns>
    string? GetCurrentAuthenticatedUserId();
}