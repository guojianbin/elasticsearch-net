using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Tests.Framework;
using Xunit;

#if DOTNETCORE
using System.Net.Http;
#endif

namespace Tests.ClientConcepts.Connection
{
	public class ConfigurationOptions
	{
        /**[[configuration-options]]
		 * === Configuration Options
         * 
		 * Connecting to Elasticsearch with Elasticsearch.Net or NEST is easy, as demonstrated by the Getting started
         * documentation on the <<elasticsearch-net-getting-started, low level>> and <<nest-getting-started, high level>> clients demonstrates. 
         * 
         * There are a number of configuration options available on `ConnectionSettings` (and `ConnectionConfiguration` for
         * Elasticsearch.Net) that can be used to control how the clients interact with Elasticsearch.
         * 
         * ==== Options on ConnectionConfiguration
         * 
         * The following is a list of available connection configuration options on `ConnectionConfiguration`; since
         * `ConnectionSettings` derives from `ConnectionConfiguration`, these options are available for both 
         * Elasticsearch.Net and NEST:
         * 
         * :xml-docs: Elasticsearch.Net:ConnectionConfiguration`1
         * 
         * ==== Options on ConnectionSettings
         * 
         * The following is a list of available connection configuration options on `ConnectionSettings`:
         * 
         * :xml-docs: Nest:ConnectionSettingsBase`1
		 *
         * Here's an example to demonstrate setting configuration options
		 */
		public void AvailableOptions()
		{
			var connectionConfiguration = new ConnectionConfiguration()
				.DisableAutomaticProxyDetection() 
				.EnableHttpCompression() 
				.DisableDirectStreaming()
                .PrettyJson()
                .RequestTimeout(TimeSpan.FromMinutes(2));

			var client = new ElasticLowLevelClient(connectionConfiguration);
		

			/**[NOTE] 
            * ====
            * 
            * Basic Authentication credentials can alternatively be specified on the node URI directly
			*/
			var uri = new Uri("http://username:password@localhost:9200");
			var settings = new ConnectionConfiguration(uri);

			/**
			* but this may become tedious when using connection pooling with multiple nodes. For this reason,
            * we'd recommend specifying it on `ConnectionSettings`.
			*====
			*/
		}

		/**[float]
		 * === OnRequestCompleted
		 * You can pass a callback of type `Action<IApiCallDetails>` that can eavesdrop every time a response (good or bad) is created.
		 * If you have complex logging needs this is a good place to add that in.
		*/
		[U]
		public void OnRequestCompletedIsCalled()
		{
			var counter = 0;
			var client = TestClient.GetInMemoryClient(s => s.OnRequestCompleted(r => counter++));
			client.RootNodeInfo();
			counter.Should().Be(1);
			client.RootNodeInfoAsync();
			counter.Should().Be(2);
		}

		/**
		*`OnRequestCompleted` is called even when an exception is thrown
		*/
		[U]
		public void OnRequestCompletedIsCalledWhenExceptionIsThrown()
		{
			var counter = 0;
			var client = TestClient.GetFixedReturnClient(new { }, 500, s => s
				.ThrowExceptions()
				.OnRequestCompleted(r => counter++)
			);
			Assert.Throws<ElasticsearchClientException>(() => client.RootNodeInfo());
			counter.Should().Be(1);
			Assert.ThrowsAsync<ElasticsearchClientException>(() => client.RootNodeInfoAsync());
			counter.Should().Be(2);
		}

		/**[float]
		* [[complex-logging]]
		* === Complex logging with OnRequestCompleted
		* Here's an example of using `OnRequestCompleted()` for complex logging. Remember, if you would also like
		* to capture the request and/or response bytes, you also need to set `.DisableDirectStreaming()` to `true`
		*/
		[U]
		public async Task UsingOnRequestCompletedForLogging()
		{
			var list = new List<string>();
			var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));

			var settings = new ConnectionSettings(connectionPool, new InMemoryConnection()) // <1> Here we use `InMemoryConnection`; in reality you would use another type of `IConnection` that actually makes a request.
				.DefaultIndex("default-index")
				.DisableDirectStreaming()
				.OnRequestCompleted(response =>
				{
					// log out the request and the request body, if one exists for the type of request
					if (response.RequestBodyInBytes != null)
					{
						list.Add(
							$"{response.HttpMethod} {response.Uri} \n" +
							$"{Encoding.UTF8.GetString(response.RequestBodyInBytes)}");
					}
					else
					{
						list.Add($"{response.HttpMethod} {response.Uri}");
					}

					// log out the response and the response body, if one exists for the type of response
					if (response.ResponseBodyInBytes != null)
					{
						list.Add($"Status: {response.HttpStatusCode}\n" +
								 $"{Encoding.UTF8.GetString(response.ResponseBodyInBytes)}\n" +
								 $"{new string('-', 30)}\n");
					}
					else
					{
						list.Add($"Status: {response.HttpStatusCode}\n" +
								 $"{new string('-', 30)}\n");
					}
				});

			var client = new ElasticClient(settings);

			var syncResponse = client.Search<object>(s => s
				.AllTypes()
				.AllIndices()
				.Scroll("2m")
				.Sort(ss => ss
					.Ascending(SortSpecialField.DocumentIndexOrder)
				)
			);

			list.Count.Should().Be(2);

			var asyncResponse = await client.SearchAsync<object>(s => s
				.AllTypes()
				.AllIndices()
				.Scroll("2m")
				.Sort(ss => ss
					.Ascending(SortSpecialField.DocumentIndexOrder)
				)
			);

			list.Count.Should().Be(4);
			list.ShouldAllBeEquivalentTo(new[]
			{
				"POST http://localhost:9200/_search?scroll=2m \n{\"sort\":[{\"_doc\":{\"order\":\"asc\"}}]}",
				"Status: 200\n------------------------------\n",
				"POST http://localhost:9200/_search?scroll=2m \n{\"sort\":[{\"_doc\":{\"order\":\"asc\"}}]}",
				"Status: 200\n------------------------------\n"
			});
		}
	}
}
