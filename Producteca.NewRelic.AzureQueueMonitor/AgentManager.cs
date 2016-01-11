using System.Threading;
using NewRelic.Platform.Sdk;

namespace Producteca.NewRelic.AzureQueueMonitor.Plugin
{
	public class AgentManager
	{
		private Thread _pollThread;

		public void Start()
		{
			_pollThread = new Thread(() =>
			{
				var runner = new Runner();

				// You can add either a factory to generate Components from a configuration file or 
				// you can directly add a component programmatically
				runner.Add(new QueueAgentFactory());

				runner.SetupAndRun();

			});

			_pollThread.Start();
		}

		public void Stop()
		{
			_pollThread.Abort();
		}
	}
}
