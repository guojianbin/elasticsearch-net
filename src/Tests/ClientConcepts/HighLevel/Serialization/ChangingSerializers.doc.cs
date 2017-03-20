using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;

namespace Tests.ClientConcepts.HighLevel.Serialization
{
    /**[[changing-serializers]]
     * === Changing Serializers
     *
     * NEST uses http://www.newtonsoft.com/json[JSON.Net] to serialize requests to and deserialize responses from JSON.
     *
     * Whilst JSON.Net does a good job of serialization, you may wish to use your own JSON serializer for a particular
     * reason. Elasticsearch.Net and NEST make it easy to replace the default serializer with your own.
     *
     */
    public class ChangingSerializers
    {
        /**
         * The main component needed is to provide an implementation of `IElasticsearchSerializer`
		 */
        public class CustomSerializer : IElasticsearchSerializer
        {
            public T Deserialize<T>(Stream stream)
            {
                // provide deserialization implementation
                throw new NotImplementedException();
            }

            public Task<T> DeserializeAsync<T>(Stream responseStream, CancellationToken cancellationToken = default(CancellationToken))
            {
                // provide an asynchronous deserialization implementation
                throw new NotImplementedException();
            }

            public void Serialize(object data, Stream writableStream, SerializationFormatting formatting = SerializationFormatting.Indented)
            {
                // provide a serialization implementation
                throw new NotImplementedException();
            }

            public IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo)
            {
                // provide an implementation, if the serializer can decide how properties should be mapped.
				// Otherwise return null.
                return null;
            }
        }

        /**
         * For Elasticsearch.Net, an implementation of `IElasticsearchSerializer` is all that is needed and a delegate can
		 * be passed to `ConnectionConfiguration` that will be called to construct an instance of the serializer
         */
        public void ConnectionConfigurationWithCustomSerializer()
        {
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            var connection = new HttpConnection();
            var connectionConfiguration =
                new ConnectionConfiguration(pool, connection, configuration => new CustomSerializer()); // <1> delegate gets passed `ConnectionConfiguration` and creates a serializer.

			var lowlevelClient = new ElasticLowLevelClient(connectionConfiguration);
        }

        /**
         * With NEST however, an implementation of `ISerializerFactory` in addition to an implementation
         * of `IElasticsearchSerializer` is required.
         */
        public class CustomSerializerFactory : ISerializerFactory
        {
            public IElasticsearchSerializer Create(IConnectionSettingsValues settings) => new CustomSerializer();

            public IElasticsearchSerializer CreateStateful(IConnectionSettingsValues settings, JsonConverter converter) =>
                new CustomSerializer();
        }

        /**
         * With an implementation of `ISerializerFactory` that can create instances of our custom serializer,
         * hooking this into `ConnectionSettings` is straightfoward
         */
        public void ConnectionSettingsWithCustomSerializer()
        {
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            var connection = new HttpConnection();
            var connectionSettings =
                new ConnectionSettings(pool, connection, new CustomSerializerFactory());

            var client = new ElasticClient(connectionSettings);
        }

        /**[IMPORTANT]
         * --
         * The implementation for how custom serialization is configured within the client is subject to
         * change in the next major release. NEST relies heavily on stateful deserializers that have access to details
         * from the original request for specialized features such a covariant search results.
         *
         * You may have noticed that this requirement leaks into the `ISerializerFactory` abstraction in the form of
         * the `CreateStateful` method signature. There are intentions to replace or at least internalize the usage of
         * JSON.Net within NEST in the future and in the process, simplifying how custom serialization can
         * be integrated.
         * --
         *
         * This has provided you details on how to implement your own custom serialization, but a much more common scenario
         * amongst NEST client users is the desire to change the serialization settings of the default JSON.Net serializer.
         * Take a look at <<modifying-default-serializer, modifying the default serializer>> to see how this can be done.
         *
         * [[modifying-default-serializer]]
         * === Modifying the default serializer
         *
         * In <<changing-serializers, Changing serializers>>, you saw how it is possible to provide your own serializer
         * implementation to NEST. A more common scenario is the desire to change the settings on the default JSON.Net
         * serializer.
         *
         * There are a couple of ways in which this can be done, depending on what it is you need to change.
         *
         * ==== Modifying settings using SerializerFactory
         *
         * The default implementation of `ISerializerFactory` allows a delegate to be passed that can change
         * the settings for JSON.Net serializers created by the factory
         *
         */
        public void ModifyingJsonNetSettings()
        {
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            var connection = new HttpConnection();
            var connectionSettings =
                new ConnectionSettings(pool, connection, new SerializerFactory((settings, values) => // <1> delegate will be passed `JsonSerializerSettings` and `IConnectionSettingsValues`
                {
                    settings.NullValueHandling = NullValueHandling.Include;
                    settings.TypeNameHandling = TypeNameHandling.Objects;
                }));

            var client = new ElasticClient(connectionSettings);
        }

        /**
         * Here, the JSON.Net serializer is configured to *always* serialize `null` values and
         * include the .NET type name when serializing to a JSON object structure.
         *
         * ==== Modifying settings using a custom ISerializerFactory
         *
         * If you need more control than passing a delegate to `SerializerFactory` provides, you can also
         * implement your own `ISerializerFactory` and derive an `IElasticsearchSerializer` from the
         * default `JsonNetSerializer`.
         *
         * Here's an example of doing so that effectively achieves the same configuration as in the previous example.
         * First, the custom factory and serializer are implemented
         */
        public class CustomJsonNetSerializerFactory : ISerializerFactory
        {
            public IElasticsearchSerializer Create(IConnectionSettingsValues settings)
            {
                return new CustomJsonNetSerializer(settings);
            }
            public IElasticsearchSerializer CreateStateful(IConnectionSettingsValues settings, JsonConverter converter)
            {
                return new CustomJsonNetSerializer(settings, converter);
            }
        }

        public class CustomJsonNetSerializer : JsonNetSerializer
        {
            public CustomJsonNetSerializer(IConnectionSettingsValues settings) : base(settings)
            {
                base.OverwriteDefaultSerializers(ModifyJsonSerializerSettings);
            }
            public CustomJsonNetSerializer(IConnectionSettingsValues settings, JsonConverter statefulConverter) :
                base(settings, statefulConverter)
            {
                base.OverwriteDefaultSerializers(ModifyJsonSerializerSettings);
            }

            private void ModifyJsonSerializerSettings(JsonSerializerSettings settings, IConnectionSettingsValues connectionSettings)
            {
                settings.NullValueHandling = NullValueHandling.Include;
                settings.TypeNameHandling = TypeNameHandling.Objects;
            }
        }

        /**
         * Then, create a new instance of the factory to `ConnectionSettings`
         */
        public void ModifyingJsonNetSettingsWithCustomSerializer()
        {
            var pool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            var connection = new HttpConnection();
            var connectionSettings =
                new ConnectionSettings(pool, connection, new CustomJsonNetSerializerFactory());

            var client = new ElasticClient(connectionSettings);
        }

        /**[IMPORTANT]
         * ====
         * Any custom serializer that derives from `JsonNetSerializer` and wishes to change the settings for the JSON.Net
         * serializer must do so using the `OverwriteDefaultSerializers` method in the constructor of the derived
         * serializer.
         *
         * NEST includes many custom changes to the http://www.newtonsoft.com/json/help/html/ContractResolver.htm[`IContractResolver`] that the JSON.Net serializer uses to resolve
         * serialization contracts for types. Examples of such changes are:
         *
         * - Allowing contracts for concrete types to be _inherited_ from interfaces that they implement
         * - Special handling of dictionaries to ensure dictionary keys are serialized verbatim
         * - Explicitly implemented interface properties are serialized in requests
         *
         * Therefore it's important that these changes to `IContractResolver` are not overwritten by a serializer derived
         * from `JsonNetSerializer`.
         * ====
         */
    }
}
