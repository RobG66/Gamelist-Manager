
namespace Gamelist_Manager.Models;

public record SshConnectionInfo(string Host, string Username, string Password);

public record SshResult(bool Success, string Output, string Error);
