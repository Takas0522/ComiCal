namespace ComiCal.Infrastructure.KeyVault;

public interface ISecretProvider
{
    System.Threading.Tasks.Task<string> GetSecretAsync(string name, System.Threading.CancellationToken cancellationToken);
}
