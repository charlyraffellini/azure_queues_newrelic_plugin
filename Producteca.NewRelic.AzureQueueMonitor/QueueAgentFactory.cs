using System.Collections.Generic;
using NewRelic.Platform.Sdk;

namespace Producteca.NewRelic.AzureQueueMonitor.Plugin
{
	public class QueueAgentFactory : AgentFactory
	{
		public QueueAgentFactory()
			: base()
		{
		}

		// This will return the deserialized properties from the specified configuration file
		// It will be invoked once per JSON object in the configuration file
		public override Agent CreateAgentWithConfiguration(IDictionary<string, object> properties)
		{
			var systemName = (string)properties["systemName"];
			var storageAccounts = (List<object>)properties["storageAccounts"];
			var serviceBusAccounts = (List<object>) properties["serviceBusAccounts"];

			var typedStorageAccounts = MapToDictionary(storageAccounts);
			var typedServiceBusAccounts = MapToDictionary(serviceBusAccounts);

			return new QueueAgent(systemName, typedStorageAccounts, typedServiceBusAccounts);
		}

		private static List<Dictionary<string, string>> MapToDictionary(List<object> storageAccounts)
		{
			var typedStorageAccounts = new List<Dictionary<string, string>>();
			foreach (var obj in storageAccounts)
			{
				var dic = (Dictionary<string, object>) obj;

				var newDic = new Dictionary<string, string>();

				foreach (var acc in dic)
					newDic[acc.Key] = (string) acc.Value;

				typedStorageAccounts.Add(newDic);
			}
			return typedStorageAccounts;
		}
	}
}
