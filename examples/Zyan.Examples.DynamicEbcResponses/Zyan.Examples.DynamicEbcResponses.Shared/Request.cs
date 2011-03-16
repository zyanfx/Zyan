using System;
using Zyan.Communication;

namespace Zyan.Examples.DynamicEbcResponses.Shared
{
    /// <summary>
    /// this class stores the argument for the service methods
    /// and provides a callback delegate to the service for responding the results back to the client
    /// </summary>
    /// <typeparam name="TRequestData">Type of the requesting argument data</typeparam>
    /// <typeparam name="TResponseData">Type of the resulting data</typeparam>
    [Serializable]
    public class Request<TRequestData, TResponseData>
    {

        /// <summary>
        /// initializes a request with the argument and the callback delegate
        /// </summary>
        /// <param name="requestData">argument data for the service</param>
        /// <param name="responseDelegate">delegate for the service to callback with the resulting data</param>
        public Request(TRequestData requestData, Action<TResponseData> responseDelegate)
        {
            this.RequestData = requestData;
            
            // store the delegate within a Zyan compatible serializable type
            if (responseDelegate!=null)
                this.responseDelegateInterceptor = new DelegateInterceptor() { ClientDelegate = responseDelegate };
        }

        /// <summary>
        /// argument data for the service
        /// </summary>
        public TRequestData RequestData { get; private set; }        
        
        /// <summary>
        /// delegate for the service to callback with the resulting data
        /// </summary>
        private readonly DelegateInterceptor responseDelegateInterceptor;

        /// <summary>
        /// method to invoke the callback delegate, passing the resulting data back to the client
        /// </summary>
        /// <param name="responseData">the resulting data</param>
        public void Response(TResponseData responseData)
        {
            if (this.responseDelegateInterceptor != null)
                this.responseDelegateInterceptor.InvokeClientDelegate(new object[] {responseData});
        }

    }

}