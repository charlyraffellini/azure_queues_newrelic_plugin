using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NewRelic.Platform.Sdk;

namespace Producteca.NewRelic.AzureQueueMonitor.Plugin
{
	public class QueueAgentFactory : AgentFactory
	{
		// This will return the deserialized properties from the specified configuration file
		// It will be invoked once per JSON object in the configuration file
		public override Agent CreateAgentWithConfiguration(IDictionary<string, object> properties)
		{
			var systemName = (string)properties["systemName"];
			var type = (string)properties["type"];
			var accountName = (string)properties["accountName"];
			var connectionString = (string)properties["connectionString"];
			

			return type == "storage" ?
				new StorageAgent(systemName, accountName, connectionString) : (Agent)new ServiceBusAgent(systemName, accountName, connectionString);
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




	public class StorageAgent : QueueAgent
	{
		public override string Version { get { return "0.0.0"; } }

		public StorageAgent(string systemName, string accountName, string accountConnectionString) : base(systemName, accountName, accountConnectionString) { }

		public override string Guid
		{
			get
			{
				return "producteca.newrelic.azure.queues.storage";
			}
		}

		public override void PollCycle()
		{
			#region storage
			var storageAccount = CloudStorageAccount.Parse(AccountConnectionString);
			var queueClient = storageAccount.CreateCloudQueueClient();

			var continuationToken = new QueueContinuationToken();

			while (continuationToken != null)
			{
				var listResponse = queueClient.ListQueuesSegmented(continuationToken);

				// We must ask Azure for the size of each queue individually.
				// This can be done in parallel.
				Parallel.ForEach(listResponse.Results, queue =>
				{
					try
					{
						queue.FetchAttributes();
					}
					catch (Exception e)
					{
						// Failed to communicate with Azure Storage, or queue is gone.
					}

				});

				// ReportMetric is not thread-safe, so we can't call it in the parallel
				foreach (var queue in listResponse.Results)
				{
					int count = queue.ApproximateMessageCount.HasValue ? queue.ApproximateMessageCount.Value : 0;
					string metricName = string.Format("storage/{0}/{1}", AccountName, queue.Name);

					ReportMetric(metricName, "messages", count);
				}

				continuationToken = listResponse.ContinuationToken;
			}
			#endregion
		}
	}

	public class ServiceBusAgent : QueueAgent
	{
		public override string Version { get { return "0.0.0"; } }

		public ServiceBusAgent(string systemName, string accountName, string accountConnectionString) : base(systemName, accountName, accountConnectionString) { }

		public override string Guid
		{
			get
			{
				return "producteca.newrelic.azure.queues.servicebus";
			}
		}

		public override void PollCycle()
		{
			#region servicebus
			var nsm = Microsoft.ServiceBus.NamespaceManager.CreateFromConnectionString(AccountConnectionString);
			var queues = nsm.GetQueues();

			foreach (var queue in queues)
			{
				var queueName = queue.Path;

				ReportQueueMessages(queue, AccountName, queueName);

				ReportDeadLetterMessages(queue, AccountName, queueName);
			}

			var topics = nsm.GetTopics();

			foreach (var topic in topics)
			{
				var topicName = topic.Path;

				var subscriptions = nsm.GetSubscriptions(topicName);
				foreach (var subscription in subscriptions)
				{
					var count = subscription.MessageCountDetails.ActiveMessageCount;
					var metricName = string.Format("servicebus/{0}/topic/{1}/subscription/{2}", AccountName, topicName, subscription.Name);
					ReportMetric(metricName, "messages", count);

					count = subscription.MessageCountDetails.DeadLetterMessageCount;
					metricName = string.Format("servicebus/{0}/topic/{1}/subscription/{2}/DeadLetter", AccountName, topicName, subscription.Name);
					ReportMetric(metricName, "messages", count);
				}
			}
			#endregion
		}
	}



	public abstract class QueueAgent : Agent
	{
		protected readonly string AccountName;
		protected readonly string AccountConnectionString;
		public string SystemName { get; set; }

		public QueueAgent(string systemName, string accountName, string accountConnectionString)
		{
			SystemName = systemName;
			AccountName = accountName;
			AccountConnectionString = accountConnectionString;
		}

		/// <summary>
		/// Returns a human-readable string to differentiate different hosts/entities in the site UI
		/// </summary>
		/// <returns></returns>
		public override string GetAgentName()
		{
			return SystemName;
		}

		public override string Guid
		{
			get
			{
				return "producteca.newrelic.azure.queues";
			}
		}

		protected void ReportQueueMessages(QueueDescription queue, string accountName, string queueName)
		{
			var count = queue.MessageCountDetails.ActiveMessageCount;
			string metricName = string.Format("servicebus/{0}/{1}", accountName, queueName);
			ReportMetric(metricName, "messages", count);
		}

		protected void ReportDeadLetterMessages(QueueDescription queue, string accountName, string queueName)
		{
			var count = queue.MessageCountDetails.DeadLetterMessageCount;
			var metricName = string.Format("servicebus/{0}/{1}/DeadLetter", accountName, queueName);
			ReportMetric(metricName, "messages", count);
		}
	}


}
