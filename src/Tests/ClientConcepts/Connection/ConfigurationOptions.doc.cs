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
		}

        /**
        * but this may become tedious when using connection pooling with multiple nodes. For this reason,
        * we'd recommend specifying it on `ConnectionSettings`.
        *====
        */  
	}
}
