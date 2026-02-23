namespace BasicAuthWorkshop.Auth;

public interface IWorkshopUserStore
{
    WorkshopUser? Validate(string username, string password);
}
