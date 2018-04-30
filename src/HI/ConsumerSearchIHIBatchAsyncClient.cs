﻿/*
 * Copyright 2011 NEHTA
 *
 * Licensed under the NEHTA Open Source (Apache) License; you may not use this
 * file except in compliance with the License. A copy of the License is in the
 * 'license.txt' file, which should be provided with this work.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using Nehta.VendorLibrary.Common;
using nehta.mcaR3.ConsumerSearchIHIBatchAsync;
using Nehta.VendorLibrary.HI.Common;
using AutoMapper;

namespace Nehta.VendorLibrary.HI
{
    /// <summary>
    /// An implementation of a client for the Medicare Healthcare Identifiers service. This class may be used to 
    /// connect to Medicare's service to perform IHI batch search operations.
    /// </summary>
    public class ConsumerSearchIHIBatchAsyncClient : IDisposable
    {
        internal ConsumerSearchIHIBatchAsyncPortType ihiBatchClient;

        /// <summary>
        /// SOAP messages for the last client call.
        /// </summary>
        public HIEndpointProcessor.SoapMessages SoapMessages { get; set; }

        /// <summary>
        /// The ProductType to be used in all IHI searches.
        /// </summary>
        ProductType product;

        /// <summary>
        /// The User to be used in all IHI searches.
        /// </summary>
        QualifiedId user;

        /// <summary>
        /// The hpio of the organisation.
        /// </summary>
        QualifiedId hpio;

        /// <summary>
        /// Gets the timestamp for the soap request.
        /// </summary>
        public TimestampType LastSoapRequestTimestamp { get; private set; }

        /// <summary>
        /// HI service name.
        /// </summary>
        public const string HIServiceOperation = "ConsumerSearchIHIBatchAsync";

        /// <summary>
        /// HI service version.
        /// </summary>
        public const string HIServiceVersion = "3.0";

        #region Constructors

        /// <summary>
        /// Initializes an instance of the ConsumerSearchIHIBatchSyncClient.
        /// </summary>
        /// <param name="endpointUri">Web service endpoint for Medicare's consumer IHI batch search service.</param>
        /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
        /// <param name="user">Identifier for the application that is calling the service.</param>
        /// <param name="hpio">Identifier for the organisation that is calling the service.</param>
        /// <param name="signingCert">Certificate to sign the soap message with.</param>
        /// <param name="tlsCert">Certificate for establishing TLS connection to the HI service.</param>
        public ConsumerSearchIHIBatchAsyncClient(Uri endpointUri, ProductType product, QualifiedId user, QualifiedId hpio, X509Certificate2 signingCert, X509Certificate2 tlsCert)
        {
            Validation.ValidateArgumentRequired("endpointUri", endpointUri);

            InitializeClient(endpointUri.ToString(), null, signingCert, tlsCert, product, user, hpio);
        }

        /// <summary>
        /// Initializes an instance of the ConsumerSearchIHIBatchSyncClient.
        /// </summary>
        /// <param name="endpointConfigurationName">Endpoint configuration name for the ConsumerSearchIHI endpoint.</param>
        /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
        /// <param name="user">Identifier for the application that is calling the service.</param>
        /// /// <param name="hpio">Identifier for the organisation that is calling the service.</param>
        /// <param name="signingCert">Certificate to sign the soap message with.</param>
        /// <param name="tlsCert">Certificate for establishing TLS connection to the HI service.</param>
        public ConsumerSearchIHIBatchAsyncClient(string endpointConfigurationName, ProductType product, QualifiedId user, QualifiedId hpio, X509Certificate2 signingCert, X509Certificate2 tlsCert)
        {
            Validation.ValidateArgumentRequired("endpointConfigurationName", endpointConfigurationName);

            InitializeClient(null, endpointConfigurationName, signingCert, tlsCert, product, user, hpio);
        }

        #endregion

        /// <summary>
        /// Submit a batch search.
        /// </summary>
        /// <param name="searches">List of IHI searches.</param>
        /// <returns>A batch identifier that will be used subsequently for a status check of the search, and also to fetch the results.</returns>
        public submitSearchIHIBatchResponse1 SubmitSearchIHIBatch(List<CommonSearchIHIRequestType> searches)
        {
            Validation.ValidateArgumentRequired("searches", searches);

            var envelope = new submitSearchIHIBatchRequest();

            var mappedSearches = Mapper.Map<List<CommonSearchIHIRequestType>, List<SearchIHIRequestType>>(searches);

            envelope.submitSearchIHIBatch = mappedSearches.ToArray();
            envelope.product = product;
            envelope.user = user;
            envelope.hpio = hpio;
            envelope.signature = new SignatureContainerType();

            envelope.timestamp = new TimestampType()
            {
                created = DateTime.Now,
                expires = DateTime.Now.AddDays(30),
                expiresSpecified = true
            };

            // Set LastSoapRequestTimestamp
            LastSoapRequestTimestamp = envelope.timestamp;

            submitSearchIHIBatchResponse1 response = null;

            try
            {
                response = ihiBatchClient.submitSearchIHIBatch(envelope);
            }
            catch (Exception ex)
            {
                // Catch generic FaultException and call helper to throw a more specific fault
                // (FaultException<ServiceMessagesType>
                FaultHelper.ProcessAndThrowFault<ServiceMessagesType>(ex);
            }

            return response;
        }

        /// <summary>
        /// Retrieve the status of a batch search.
        /// </summary>
        /// <param name="batchIdentifier">The batch search identifier.</param>
        /// <returns>A status indicating if the batch search is ready for retrieval.</returns>
        public getSearchIHIBatchStatusResponse1 GetSearchIHIBatchStatus(string batchIdentifier)
        {
            Validation.ValidateArgumentRequired("batchIdentifier", batchIdentifier);

            var envelope = new getSearchIHIBatchStatusRequest();

            envelope.getSearchIHIBatchStatus = new getSearchIHIBatchStatus()
            {
                batchIdentifier = batchIdentifier
            };
            envelope.product = product;
            envelope.user = user;
            envelope.signature = new SignatureContainerType();

            envelope.timestamp = new TimestampType()
            {
                created = DateTime.Now,
                expires = DateTime.Now.AddDays(30),
                expiresSpecified = true
            };

            // Set LastSoapRequestTimestamp
            LastSoapRequestTimestamp = envelope.timestamp;

            getSearchIHIBatchStatusResponse1 response = null;

            try
            {
                response = ihiBatchClient.getSearchIHIBatchStatus(envelope);
            }
            catch (Exception ex)
            {
                // Catch generic FaultException and call helper to throw a more specific fault
                // (FaultException<ServiceMessagesType>
                FaultHelper.ProcessAndThrowFault<ServiceMessagesType>(ex);
            }

            return response;
        }

        /// <summary>
        /// Retrieve the results of a batch search.
        /// </summary>
        /// <param name="batchIdentifier">The batch search identifier for the batch results to be retrieved.</param>
        /// <returns>A results of the batch IHI search.</returns>
        public retrieveSearchIHIBatchResponse1 RetrieveSearchIHIBatch(string batchIdentifier)
        {
            Validation.ValidateArgumentRequired("batchIdentifier", batchIdentifier);

            var envelope = new retrieveSearchIHIBatchRequest();

            envelope.retrieveSearchIHIBatch = new retrieveSearchIHIBatch()
            {
                batchIdentifier = batchIdentifier
            };
            envelope.product = product;
            envelope.user = user;
            envelope.signature = new SignatureContainerType();

            envelope.timestamp = new TimestampType()
            {
                created = DateTime.Now,
                expires = DateTime.Now.AddDays(30),
                expiresSpecified = true
            };

            // Set LastSoapRequestTimestamp
            LastSoapRequestTimestamp = envelope.timestamp;

            retrieveSearchIHIBatchResponse1 response = null;

            try
            {
                response = ihiBatchClient.retrieveSearchIHIBatch(envelope);
            }
            catch (Exception ex)
            {
                // Catch generic FaultException and call helper to throw a more specific fault
                // (FaultException<ServiceMessagesType>
                FaultHelper.ProcessAndThrowFault<ServiceMessagesType>(ex);
            }

            return response;
        }

        /// <summary>
        /// Remove the results of a batch IHI search after the results have been retrieved.
        /// </summary>
        /// <param name="batchIdentifier">The batch search identifier for the batch results to be deleted.</param>
        /// <returns>The status of the delete operation.</returns>
        public deleteSearchIHIBatchResponse1 DeleteSearchIHIBatch(string batchIdentifier)
        {
            Validation.ValidateArgumentRequired("batchIdentifier", batchIdentifier);

            var envelope = new deleteSearchIHIBatchRequest();

            envelope.deleteSearchIHIBatch = new deleteSearchIHIBatch()
            {
                batchIdentifier = batchIdentifier
            };
            envelope.product = product;
            envelope.user = user;
            envelope.signature = new SignatureContainerType();

            envelope.timestamp = new TimestampType()
            {
                created = DateTime.Now,
                expires = DateTime.Now.AddDays(30),
                expiresSpecified = true
            };

            // Set LastSoapRequestTimestamp
            LastSoapRequestTimestamp = envelope.timestamp;

            deleteSearchIHIBatchResponse1 response = null;

            try
            {
                response = ihiBatchClient.deleteSearchIHIBatch(envelope);
            }
            catch (Exception ex)
            {
                // Catch generic FaultException and call helper to throw a more specific fault
                // (FaultException<ServiceMessagesType>
                FaultHelper.ProcessAndThrowFault<ServiceMessagesType>(ex);
            }

            return response;
        }

        #region Private and internal methods

        /// <summary>
        /// Initializes an instance of the ConsumerSearchIHIBatchSyncClient.
        /// </summary>
        /// <param name="endpointUrl">Web service endpoint for Medicare's consumer IHI batch search service.</param>
        /// <param name="endpointConfigurationName">Endpoint configuration name for the ConsumerSearchIHIBatchSync endpoint.</param>
        /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
        /// <param name="user">Identifier for the application that is calling the service.</param>
        /// <param name="signingCert">Certificate to sign the soap message with.</param>
        /// <param name="tlsCert">Certificate for establishing TLS connection to the HI service.</param>
        /// <param name="hpio">Identifer for the organisation</param>
        private void InitializeClient(string endpointUrl, string endpointConfigurationName, X509Certificate2 signingCert, X509Certificate2 tlsCert, ProductType product, QualifiedId user, QualifiedId hpio)
        {
            Utility.SetUpMapping();

            Validation.ValidateArgumentRequired("product", product);
            Validation.ValidateArgumentRequired("user", user);
            Validation.ValidateArgumentRequired("signingCert", signingCert);
            Validation.ValidateArgumentRequired("tlsCert", tlsCert);

            this.product = product;
            this.user = user;
            this.hpio = hpio;

            SoapMessages = new HIEndpointProcessor.SoapMessages();

            ConsumerSearchIHIBatchAsyncPortTypeClient client = null;

            if (!string.IsNullOrEmpty(endpointUrl))
            {
                var address = new EndpointAddress(endpointUrl);
                var tlsBinding = GetBinding();

                client = new ConsumerSearchIHIBatchAsyncPortTypeClient(tlsBinding, address);
            }
            else if (!string.IsNullOrEmpty(endpointConfigurationName))
            {
                client = new ConsumerSearchIHIBatchAsyncPortTypeClient(endpointConfigurationName);
            }

            if (client != null)
            {
                HIEndpointProcessor.ProcessEndpoint(client.Endpoint, signingCert, SoapMessages);

                if (tlsCert != null)
                {
                    client.ClientCredentials.ClientCertificate.Certificate = tlsCert;
                }

                ihiBatchClient = client;
            }
        }

        /// <summary>
        /// Gets the binding configuration for the client.
        /// </summary>
        /// <returns>Configured CustomBinding instance.</returns>
        internal CustomBinding GetBinding()
        {
            // Set up binding
            var tlsBinding = new CustomBinding();

            // Extend default timeouts
            tlsBinding.CloseTimeout = new TimeSpan(0, 3, 0);
            tlsBinding.OpenTimeout = new TimeSpan(0, 3, 0);
            tlsBinding.ReceiveTimeout = new TimeSpan(0, 10, 0);
            tlsBinding.SendTimeout = new TimeSpan(0, 3, 0);

            var tlsEncoding = new TextMessageEncodingBindingElement();
            tlsEncoding.ReaderQuotas.MaxDepth = 2147483647;
            tlsEncoding.ReaderQuotas.MaxStringContentLength = 2147483647;
            tlsEncoding.ReaderQuotas.MaxArrayLength = 2147483647;
            tlsEncoding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            tlsEncoding.ReaderQuotas.MaxNameTableCharCount = 2147483647;

            var httpsTransport = new HttpsTransportBindingElement();
            httpsTransport.RequireClientCertificate = true;
            httpsTransport.MaxReceivedMessageSize = 2147483647;
            httpsTransport.MaxBufferSize = 2147483647;

            tlsBinding.Elements.Add(tlsEncoding);
            tlsBinding.Elements.Add(httpsTransport);

            return tlsBinding;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Closes and disposes the client.
        /// </summary>
        public void Dispose()
        {
            ClientBase<ConsumerSearchIHIBatchAsyncPortType> searchClient;

            if (ihiBatchClient is ClientBase<ConsumerSearchIHIBatchAsyncPortType>)
            {
                searchClient = (ClientBase<ConsumerSearchIHIBatchAsyncPortType>)ihiBatchClient;
                if (searchClient.State != CommunicationState.Closed)
                    searchClient.Close();
            }
        }

        #endregion
    }
}
