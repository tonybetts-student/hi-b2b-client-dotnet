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
using System.ServiceModel.Channels;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

using nehta.mcaR50.ProviderSearchForProviderOrganisation;
using Nehta.VendorLibrary.Common;

namespace Nehta.VendorLibrary.HI
{
    /// <summary>
    /// An implementation of a client for the Medicare Healthcare Identifiers service. This class may be used to 
    /// connect to Medicare's service to perform provider HPIO validation (for provider organisations not published on the provider directory).
    /// </summary>
    public class ProviderSearchForProviderOrganisationClient
    {
            internal ProviderSearchForProviderOrganisationPortType providerSearchForProviderOrganisationClient;

            /// <summary>
            /// SOAP messages for the last client call.
            /// </summary>
            public HIEndpointProcessor.SoapMessages SoapMessages { get; set; }

            /// <summary>
            /// The ProductType to be used in all searches.
            /// </summary>
            ProductType product;

            /// <summary>
            /// The User to be used in all searches.
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
            public const string HIServiceOperation = "ProviderSearchForProviderOrganisation";

            /// <summary>
            /// HI service version.
            /// </summary>
            public const string HIServiceVersion = "5.0.0";

            #region Constructors
    
            /// <summary>
            /// Initializes an instance of the provider search client.
            /// </summary>
            /// <param name="endpointUri">Web service endpoint for Medicare's provider search service.</param>
            /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
            /// <param name="user">Identifier for the application that is calling the service.</param>
            /// <param name="hpio">Identifier for the organisation</param>
            /// <param name="signingCert">Certificate to sign the soap message with.</param>
            /// <param name="tlsCert">Certificate for establishing TLS connection to the service.</param>
            public ProviderSearchForProviderOrganisationClient(Uri endpointUri, ProductType product, QualifiedId user,  QualifiedId hpio, X509Certificate2 signingCert, X509Certificate2 tlsCert)
            {
                Validation.ValidateArgumentRequired("endpointUri", endpointUri);

                InitializeClient(endpointUri.ToString(), null, signingCert, tlsCert, product, user, hpio);
            }

            /// <summary>
            /// Initializes an instance of the provider search client.
            /// </summary>
            /// <param name="endpointConfigurationName">Endpoint configuration name for the provider search endpoint.</param>
            /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
            /// <param name="user">Identifier for the application that is calling the service.</param>
            /// <param name="hpio">Identifier for the organisation</param>
            /// <param name="signingCert">Certificate to sign the soap message with.</param>
            /// <param name="tlsCert">Certificate for establishing TLS connection to the service.</param>
            public ProviderSearchForProviderOrganisationClient(string endpointConfigurationName, ProductType product, QualifiedId user, QualifiedId hpio, X509Certificate2 signingCert, X509Certificate2 tlsCert)
            {
                Validation.ValidateArgumentRequired("endpointConfigurationName", endpointConfigurationName);

                InitializeClient(null, endpointConfigurationName, signingCert, tlsCert, product, user, hpio);
            }

            #endregion

            #region Private and internal methods

            /// <summary>
            /// Perform the service call.
            /// </summary>
            /// <param name="request">The search criteria in a searchHIProviderDirectoryForIndividual object.</param>
            /// <returns>The search results in a searchHIProviderDirectoryForIndividualResponse object.</returns>
            public searchForProviderOrganisationResponse ProviderOrganisationSearch(searchForProviderOrganisation request)
            {
                var envelope = new searchForProviderOrganisationRequest()
                {
                    searchForProviderOrganisation = request,
                    product = product,
                    user = user,
                    hpio = hpio,
                    signature = new SignatureContainerType()
                };

                envelope.timestamp = new TimestampType()
                {
                    created = DateTime.Now,
                    expires = DateTime.Now.AddDays(30),
                    expiresSpecified = true
                };

                // Set LastSoapRequestTimestamp
                LastSoapRequestTimestamp = envelope.timestamp;

                searchForProviderOrganisationResponse1 response1 = null;

                try
                {
                    response1 = providerSearchForProviderOrganisationClient.searchForProviderOrganisation(envelope);
                }
                catch (Exception ex)
                {
                    // Catch generic FaultException and call helper to throw a more specific fault
                    // (FaultException<ServiceMessagesType>
                    FaultHelper.ProcessAndThrowFault<ServiceMessagesType>(ex);
                }

                if (response1 != null && response1.searchForProviderOrganisationResponse != null)
                    return response1.searchForProviderOrganisationResponse;
                else
                    throw new ApplicationException(Properties.Resources.UnexpectedServiceResponse);
            }

            /// <summary>
            /// Initialization for the Medicare provider search client.
            /// </summary>
            /// <param name="endpointUrl">Web service endpoint for the Medicare provider search.</param>
            /// <param name="endpointConfigurationName">Endpoint configuration name for the Medicare's provider search endpoint.</param>
            /// <param name="signingCert">Certificate to sign the soap message with.</param>
            /// <param name="tlsCert">Certificate for establishing TLS connection to the service.</param>
            /// <param name="product">PCIN (generated by Medicare) and platform name values.</param>
            /// <param name="user">Identifier for the application that is calling the service.</param>
            /// <param name="hpio">Identifier for the CSP</param>
            private void InitializeClient(string endpointUrl, string endpointConfigurationName, X509Certificate2 signingCert, X509Certificate2 tlsCert, ProductType product, QualifiedId user, QualifiedId hpio)
            {
                Validation.ValidateArgumentRequired("product", product);
                Validation.ValidateArgumentRequired("user", user);
                Validation.ValidateArgumentRequired("signingCert", signingCert);
                Validation.ValidateArgumentRequired("tlsCert", tlsCert);

                this.product = product;
                this.user = user;
                this.hpio = hpio;

                SoapMessages = new HIEndpointProcessor.SoapMessages();

                CustomBinding tlsBinding = GetBinding();

                ProviderSearchForProviderOrganisationPortTypeClient client = null;

                if (!string.IsNullOrEmpty(endpointUrl))
                {
                    var address = new EndpointAddress(endpointUrl);

                    client = new ProviderSearchForProviderOrganisationPortTypeClient(tlsBinding, address);
                }
                else if (!string.IsNullOrEmpty(endpointConfigurationName))
                {
                    client = new ProviderSearchForProviderOrganisationPortTypeClient(endpointConfigurationName);
                }

                if (client != null)
                {
                    HIEndpointProcessor.ProcessEndpoint(client.Endpoint, signingCert, SoapMessages);

                    if (tlsCert != null) client.ClientCredentials.ClientCertificate.Certificate = tlsCert;
                    providerSearchForProviderOrganisationClient = client;
                }
            }

            /// <summary>
            /// Gets the binding configuration for the client.
            /// </summary>
            /// <returns>Configured customBinding instance.</returns>
            internal CustomBinding GetBinding()
            {
                // Set up binding
                var tlsBinding = new CustomBinding();

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
                ClientBase<ProviderSearchForProviderOrganisationPortType> lClient;

                if (providerSearchForProviderOrganisationClient is ClientBase<ProviderSearchForProviderOrganisationPortType>)
                {
                    lClient = (ClientBase<ProviderSearchForProviderOrganisationPortType>)providerSearchForProviderOrganisationClient;
                    if (lClient.State != CommunicationState.Closed)
                        lClient.Close();
                }
            }

            #endregion
    }
}
