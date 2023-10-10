using SequenceNode;
using SequenceNode.Application;
using SequenceNode.Infrastructure.Zoo;

public static class ZookeeperServiceCollectionExtensions
{
  public static IServiceCollection AddZookeeper(this IServiceCollection serviceCollection, ConfigurationManager configuration)
  {
    serviceCollection.Configure<ZookeeperConfig>(configuration.GetSection(ZookeeperConfig.ZookeeperSectionKey));
    serviceCollection.AddTransient<ZookeeperWatcher>();
    serviceCollection.AddSingleton<ZookeeperConnection>();
    serviceCollection.AddSingleton<IDistributedConfiguration, ZookeeperDistributedConfiguration>();
    return serviceCollection;
  }
}