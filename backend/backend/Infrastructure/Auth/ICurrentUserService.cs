namespace backend.Infrastructure.Auth;

public interface ICurrentUserService
{
    CurrentUser GetRequiredUser();
}
