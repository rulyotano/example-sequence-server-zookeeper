using org.apache.zookeeper;

namespace SequenceNode.Infrastructure.Zoo;

public class ZookeeperWatcher : Watcher
{
  private readonly ILogger<ZookeeperWatcher> _logger;
  private IList<Func<WatchedEvent, Task>> _watchers = new List<Func<WatchedEvent, Task>>();

  public ZookeeperWatcher(ILogger<ZookeeperWatcher> logger)
  {
    _logger = logger;
  }

  public void Subscribe(Func<WatchedEvent, Task> action) => _watchers.Add(action);

  public void UnSubscribe(Func<WatchedEvent, Task> action) => _watchers.Remove(action);

  public override async Task process(WatchedEvent zookeeperEvent)
  {
    _logger.LogInformation("New zooKeeper event, {State} {Type}", zookeeperEvent.getState(), zookeeperEvent.get_Type());

    foreach (var watcher in _watchers)
    {
      await watcher(zookeeperEvent);
    }
  }
}