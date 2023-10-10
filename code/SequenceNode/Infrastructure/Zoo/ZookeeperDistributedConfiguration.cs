using System.Net;
using System.Text;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using SequenceNode.Application;
using SequenceNode.Infrastructure.Zoo;

namespace SequenceNode;

public class ZookeeperDistributedConfiguration : IDistributedConfiguration
{
  private const string SequenceNode = "/sequence";
  private const int FirstSequence = 1;
    private readonly ZookeeperConnection _connection;
  private readonly ILogger<ZookeeperDistributedConfiguration> _logger;
  private bool _isInitialized = false;
  private int _sequenceNumber = default;

  public ZookeeperDistributedConfiguration(ZookeeperConnection connection, ILogger<ZookeeperDistributedConfiguration> logger)
  {
    _connection = connection;
    _logger = logger;
    connection.OnConnected += OnConnected;
    connection.OnDisconnected += OnDisconnected;
  }

  public async Task<int> GetSequenceNumberAsync(CancellationToken cancellationToken = default)
  {
    await AssignSequenceIfNoAsync(cancellationToken);
    return _sequenceNumber;
  }

  private Task OnConnected()
  {
    return Task.CompletedTask;
  }

  private Task OnDisconnected()
  {
    _isInitialized = false;
    _sequenceNumber = default;
    return Task.CompletedTask;
  }

  private async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    if (_isInitialized) return;
    var zookeeper = await _connection.GetZookeeperAsync();
    if ((await zookeeper.existsAsync(SequenceNode)) is not null)
    {
      _logger.LogWarning("Sequence node exist");
      return;
    }
    if (cancellationToken.IsCancellationRequested == true) return;
    _logger.LogInformation("Creating new sequence node");
    await zookeeper.createAsync(SequenceNode, Array.Empty<byte>(), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
    _isInitialized = true;
    _logger.LogInformation("Sequence node created");
  }

  private async Task AssignSequenceIfNoAsync(CancellationToken cancellationToken)
  {
    await InitializeAsync();
    if (IsSequenceAssigned()) return;
    var created = false;
    var triesLeft = 5;

    while (!created && triesLeft > 0)
    {
      var assignedSequenceNumbers = await GetAssignedSequenceNumbersAsync(cancellationToken);
      if (assignedSequenceNumbers.Count == 0 || assignedSequenceNumbers[0] != FirstSequence)
      {
        _sequenceNumber = FirstSequence;
      }
      _sequenceNumber = FindFirstFreeSequenceNumber(assignedSequenceNumbers);

      created = await AssignSequenceNumberAsync(_sequenceNumber, cancellationToken);
      triesLeft--;
    }
  }

  private bool IsSequenceAssigned() => _sequenceNumber != default;

  private async Task<IList<int>> GetAssignedSequenceNumbersAsync(CancellationToken cancellationToken)
  {
    return (await GetExistingSequenceNodesAsync(cancellationToken)).Select(node => int.Parse(node)).ToList();
  }

  private async Task<IEnumerable<string>> GetExistingSequenceNodesAsync(CancellationToken cancellationToken)
  {
    var zookeeper = await _connection.GetZookeeperAsync();
    var result = await zookeeper.getChildrenAsync(SequenceNode);
    return result.Children ?? Enumerable.Empty<string>();
  }

  private async Task<bool> AssignSequenceNumberAsync(int sequenceNumber, CancellationToken cancellationToken)
  {
    var hostName = Dns.GetHostName();
    _logger.LogWarning("Host name: {HostName}", hostName);
    var zookeeper = await _connection.GetZookeeperAsync();
    try
    {
      var result = await zookeeper.createAsync($"{SequenceNode}/{sequenceNumber}",
                                                  Encoding.UTF8.GetBytes(hostName),
                                                  ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);
      return !string.IsNullOrEmpty(result);
    }
    catch (Exception e)
    {
      _logger.LogError("Error creating sequence node. {Error}", e.Message);
      return false;
    }
  }

  private int FindFirstFreeSequenceNumber(IList<int> assignedSequenceNumbers)
  {
    for (int i = 1; i < assignedSequenceNumbers.Count; i++)
    {
      if (assignedSequenceNumbers[i] > assignedSequenceNumbers[i - 1] + 1) return assignedSequenceNumbers[i - 1] + 1;
    }
    return FirstSequence + assignedSequenceNumbers.Count;
  }
}
