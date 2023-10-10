using Microsoft.Extensions.Options;
using org.apache.zookeeper;

namespace SequenceNode.Infrastructure.Zoo;

public class ZookeeperConnection
{
  private const int MaxInitializationRetries = 50;
  public delegate Task ZookeeperConnectionEvent();
  private readonly ZookeeperWatcher _watcher;
  private ZooKeeper? _zookeeper;
  private ZookeeperConfig _zookeeperOptions;
  private bool _connected = false;

  public ZookeeperConnection(IOptions<ZookeeperConfig> options, ZookeeperWatcher watcher)
  {
    _zookeeperOptions = options.Value;
    _watcher = watcher;
    watcher.Subscribe(OnZookeeperEvent);
  }

  public async Task<ZooKeeper> GetZookeeperAsync()
  {
    if (_connected && _zookeeper is not null) return _zookeeper;
    if (_zookeeper is null) _zookeeper = new ZooKeeper(_zookeeperOptions.ConnectionString, _zookeeperOptions.SessionTimeoutMs, _watcher);

    var retries = MaxInitializationRetries;

    while (!_connected && retries > 0)
    {
      await Task.Delay(100);
      retries--;
    }

    if (_connected) return _zookeeper;

    _zookeeper = null;
    throw new TimeoutException("Couldn't connect to zookeeper");
  }
  public event ZookeeperConnectionEvent OnConnected;
  public event ZookeeperConnectionEvent OnDisconnected;

  private async Task OnZookeeperEvent(WatchedEvent zookeeperEvent)
  {
    var state = zookeeperEvent.getState();
    switch (state)
    {
      case Watcher.Event.KeeperState.Disconnected:
      case Watcher.Event.KeeperState.Expired:
        _zookeeper = null;
        _connected = false;
        await (OnDisconnected?.Invoke() ?? Task.CompletedTask);
        break;
      case Watcher.Event.KeeperState.SyncConnected:
        _connected = true;
        await (OnConnected?.Invoke() ?? Task.CompletedTask);
        break;
    }
  }
}