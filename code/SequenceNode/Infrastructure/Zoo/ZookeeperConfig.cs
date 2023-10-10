namespace SequenceNode.Infrastructure.Zoo;

public class ZookeeperConfig
{
  public const string ZookeeperSectionKey = "Zookeeper";
  public string ConnectionString { get; set; } = string.Empty;

  public int SessionTimeoutMs { get; set; } = 5000;
}
