public class SshCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string SshKeyContent { get; set; } // Make sure this property exists
}