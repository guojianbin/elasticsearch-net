using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests.ClientConcepts.Connection
{
    /**[[configuring-ssl]]
     * === Configuring SSL
     * SSL can be configured via the `ServerCertificateValidationCallback` property on 
     * 
     * - `ServerPointManager` for Desktop CLR
     * - `HttpClientHandler` for Core CLR
     * 
     * On the Desktop CLR, this must be done outside of the client using .NET's built-in
     * http://msdn.microsoft.com/en-us/library/system.net.servicepointmanager%28v=vs.110%29.aspx[ServicePointManager] class:
     */
    public class ConfiguringSsl
    {
        /** The bare minimum to make .NET accept self-signed SSL certs that are not in the 
         * https://technet.microsoft.com/en-us/library/cc754841(v=ws.11).aspx[Trusted Root Certificate Authorities store]
         * is to have the `ServerCertificateValidationCallback` callback simply return `true`
         */
        public void ServerCertificateValidationCallback()
        {

#if !DOTNETCORE
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
#endif
        }
        /**[WARNING]
         * --
         * This will accept **all** requests from the AppDomain to untrusted SSL sites,
         * therefore **we recommend doing some minimal introspection on the passed in certificate.**
         * --
         */

#if DOTNETCORE
		/**
		 * If running on Core CLR, then a custom connection type must be created by deriving from `HttpConnection` and
		 * overriding the `CreateHttpClientHandler` method in order to set the `ServerCertificateCustomValidationCallback` property:
		*/
		public class SecureHttpConnection : HttpConnection
		{
			protected override HttpClientHandler CreateHttpClientHandler(RequestData requestData)
			{
				var handler = base.CreateHttpClientHandler(requestData);
				handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true;
				return handler;
			}
		}
#endif
    }
}
