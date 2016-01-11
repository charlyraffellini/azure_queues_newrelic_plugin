using Topshelf;

namespace Producteca.NewRelic.AzureQueueMonitor.Plugin
{
	class Program
	{
		public static void Main()
		{
			HostFactory.Run(x =>
			{
				x.Service<AgentManager>(s =>
				{
					s.ConstructUsing(name => new AgentManager());
					s.WhenStarted(tc => tc.Start());
					s.WhenStopped(tc => tc.Stop());
				});

				x.RunAsLocalSystem();

				x.SetDescription("NewRelic Windows Azure Queue Size plugin");
				x.SetDisplayName("ScalableBytes NewRelic Azure Queues");
				x.SetServiceName("Producteca.NewRelic.AzureQueueMonitor.Plugin");
			});
		}
	}
}
