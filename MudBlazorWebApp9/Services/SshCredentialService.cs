namespace MudBlazorWebApp9.Services;

public class SshCredentialService
{
    private readonly List<SshCredential> _credentials = new();

    public List<SshCredential> GetAllCredentials() => _credentials;

    public SshCredential GetCredentialById(string id) =>
        _credentials.FirstOrDefault(c => c.Id == id);

    public void AddCredential(SshCredential credential) =>
        _credentials.Add(credential);

    public void RemoveCredential(string id) =>
        _credentials.RemoveAll(c => c.Id == id);
}