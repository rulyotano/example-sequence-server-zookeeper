namespace SequenceNode.Application;

public interface IDistributedConfiguration
{
  Task<int> GetSequenceNumberAsync(CancellationToken cancellationToken = default);
}