using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;

namespace Tests.ClientConcepts.LowLevel
{
    /**[[elasticsearch-net-getting-started]]
     * == Getting started
     * 
     * Elasticsearch.Net is a low level Elasticsearch .NET client that has no dependencies on other libraries
     * and is unopinionated about how you build your requests and responses.
     * 
     */
    public class GettingStarted
    {
        private IElasticLowLevelClient lowlevelClient = new ElasticLowLevelClient(new ConnectionConfiguration(new SingleNodeConnectionPool(new Uri("http://localhost:9200")), new InMemoryConnection()));


        /**[float]
		 * === Connecting
		 *
		 * To connect to Elasticsearch running locally at `http://localhost:9200` is as simple as 
         * instantiating a new instance of the client
		 */
        public void SimpleInstantiation()
        {
            var lowlevelClient = new ElasticLowLevelClient();
        }

        /**
		 * Often you may need to pass additional configuration options to the client such as the address of Elasticsearch if it's running on
		 * a remote machine. This is where `ConnectionConfiguration` come in; an instance can be instantiated to provide 
         * the client with different configuration values.
		 */
        public void UsingConnectionSettings()
        {
            var settings = new ConnectionConfiguration(new Uri("http://example.com:9200"))
                .RequestTimeout(TimeSpan.FromMinutes(2));

            var lowlevelClient = new ElasticLowLevelClient(settings);
        }

        /**
		 * In this example, a default request timeout was also specified to that will be applied to all requests to determine after how long the client should cancel a request. 
         * There are many other <<configuration-options,configuration options>> on `ConnectionConfiguration` to control things such as
         * 
         * - Basic Authentication header to attach to all requests
         * - Whether the client should connect through a proxy
         * - HTTP compression support should be enabled on the client
		 *
		 * `ConnectionConfiguration` is not restricted to being passed a single address for Elasticsearch; There are several different
		 * types of <<connection-pooling,IConnectionPool>> available, each with different characteristics that can be used to
		 * configure the client. The following example uses a <<sniffing-connection-pool,SniffingConnectionPool>> seeded with the addresses
		 * of three Elasticsearch nodes in the cluster, and the client will use this type of pool to maintain a list of available nodes within the
		 * cluster to which it can send requests in a round-robin fashion.
		 */
        public void UsingConnectionPool()
        {
            var uris = new[]
            {
                new Uri("http://localhost:9200"),
                new Uri("http://localhost:9201"),
                new Uri("http://localhost:9202"),
            };

            var connectionPool = new SniffingConnectionPool(uris);
            var settings = new ConnectionConfiguration(connectionPool);

            var lowlevelClient = new ElasticLowLevelClient(settings);
        }

        /**[float]
         * === Indexing
         *
         * Once a client had been configured to connect to Elasticsearch, we need to get some data into the cluster to work with.
         *
         * Imagine we have the following http://en.wikipedia.org/wiki/Plain_Old_CLR_Object[POCO]
         */
        public class Person
        {
            public Guid Id { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        /**
		 * Indexing a single instance of the POCO either synchronously or asynchronously, is as simple as
		 */
        public async Task Indexing()
        {
            var person = new
            {
                FirstName = "Martijn",
                LastName = "Laarman"
            };

            var indexResponse = lowlevelClient.Index<byte[]>("people", "person", "1", person); //<1> synchronous method that returns an `IIndexResponse`
            byte[] responseBytes = indexResponse.Body;

            var asyncIndexResponse = await lowlevelClient.IndexAsync<string>("people", "person", "1", person); //<2> asynchronous method that returns a `Task<IIndexResponse>` that can be awaited
            string asyncResponseString = asyncIndexResponse.Body;
        }

        /**
		 * NOTE: All methods available within Elasticsearch.Net are exposed as both synchronous and asynchronous versions,
		 * with the latter using the idiomatic *Async suffix on the method name.
		 *
		 * This will index the document to the endpoint `/people/person/1`. An https://msdn.microsoft.com/en-us/library/bb397696.aspx[anonymous type] was 
         * used to represent the document to index in Elasticsearch, which was implicitly converted to an instance of `PostData<T>` that
         * the method accepts. Check out the documentation on <<post-data, Post Data>> to see the other types that are supported.
         * 
         * The generic type parameter on the method specifies the type of the response body.
		 *
		 * [float]
		 * === Searching
		 *
		 * Now that we have indexed some documents we can begin to search for them.
         * 
         * The Elasticsearch Query DSL can be expressed using an anonymous type within the request
		 */
        public void SearchingWithAnonymousTypes()
        {
            var searchResponse = lowlevelClient.Search<string>("people", "person", new
            {
                from = 0,
                size = 10,
                query = new
                {
                    match = new
                    {
                        field = "firstName",
                        query = "Martijn"
                    }
                }
            });

            var successful = searchResponse.Success;
            var responseJson = searchResponse.Body;
        }

        /**
		 * `responseJson` now holds a JSON string for the response. The search endpoint for this query is
		 * `/people/person/_search` and it's possible to search over multiple indices and types by changing the arguments
         * supplied in the request for `index` and `type`, respectively.
         * 
         * Strings can also be used to express the request
         */ 
         public void SearchingWithStrings()
        {
            var searchResponse = lowlevelClient.Search<byte[]>("people", "person", @"
            {
                ""from"": 0,
                ""size"": 10,
                ""query"": {               
                    ""match"": {
                        ""field"": ""firstName"",
                        ""query"": ""Martijn""
                    }
                }
            }");

            var responseBytes = searchResponse.Body;
        }

        /**
        * As you can see, this can be a little more cumbersome than using anonymous types because of the need to escape
        * double quotes within the request string, but it can be useful at times nonetheless. `responseBytes` will contain
        * the bytes of the response from Elasticsearch.
        *
        * [NOTE]
        * --
        * Elasticsearch.Net does not provide typed objects to represent responses; if you need this, you should consider
        * using <<nest, NEST, the high level client>>, that does map all requests and responses to types. You can work with
        * strong types with Elasticsearch.Net but it will be up to you as the developer to configure Elasticsearch.Net so that
        * it understands how to deserialize your types, most likely by providing your own `IElasticsearchSerializer` implementation
        * to `ConnectionConfiguration`.
        * --
        * 
        * [float]
        * === Handling Errors
        * 
        * By default, Elasticsearch.Net is configured not to throw exceptions if a HTTP response status code is returned that is not in
        * the 200-300 range, nor an expected response status code allowed for a given request e.g. checking if an index exists
        * can return a 404.
        * 
        * The response from low level client calls provides a number of properties that can be used to determine if a call
        * is successful
        */
        public void ResponseProperties()
        {
            var searchResponse = lowlevelClient.Search<byte[]>("people", "person", new { match_all = new {} });

            var success = searchResponse.Success; // <1> Response is in the 200 range, or an expected response for the given request
            var successOrKnownError = searchResponse.SuccessOrKnownError; // <2> Response is successful, or has a response code between 400-599 that indicates the request cannot be retried.
            var serverError = searchResponse.ServerError; // <3> Details of any error returned from Elasticsearch
            var exception = searchResponse.OriginalException; // <4> If the response is unsuccessful, will hold the original exception.
        }

        /** 
        * Using these details, it is possible to make decisions around what should be done in your application.
        * 
        * The default behaviour of not throwing exceptions can be changed by setting `.ThrowExceptions()` on `ConnectionConfiguration`
        */
        public void HandlingErrors()
        {
            var settings = new ConnectionConfiguration(new Uri("http://example.com:9200"))
                .ThrowExceptions();

            var lowlevelClient = new ElasticLowLevelClient(settings);
        }

        /**
         * And if more fine grained control is required, custom exceptions can be thrown using `.OnRequestCompleted()` on
         * `ConnectionConfiguration`
         */
        public void FineGrainedControl()
        {
            var settings = new ConnectionConfiguration(new Uri("http://example.com:9200"))
                .OnRequestCompleted(apiCallDetails =>
                {
                    if (apiCallDetails.HttpStatusCode == 418)
                    {
                        throw new TimeForACoffeeException();
                    }
                });

            var lowlevelClient = new ElasticLowLevelClient(settings);
        }

        // hide
        private class TimeForACoffeeException : Exception { }
    }
}
