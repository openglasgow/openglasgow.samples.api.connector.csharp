// ***********************************************************************
// Assembly         : GCC.API.ConnectorLib
// ***********************************************************************

/// <summary>
/// GCC.CTP.PublicAPIDemo.ConnectorLib namespace.
/// </summary>
namespace GCC.API.ConnectorLib
{
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Configuration;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Web;

    /// <summary>
    /// Publisher
    /// </summary>
    public class Publisher
    {
        const string API_NEW_FILE_ENDPOINT = "https://api.open.glasgow.gov.uk/Files/Organisation/{0}/Dataset/{1}";
        const string API_NEW_FILE_VERSION_ENDPOINT = "https://api.open.glasgow.gov.uk/Files/Organisation/{0}/Dataset/{1}/File/{2}";
        
        public Publisher(string authBase, string tenantId, string resourceId, string clientId, string clientKey, 
            string subscriptionKey, Guid orgId, Guid datasetId)
        {
            this.Authbase = authBase;
            this.TenantId = tenantId;
            this.ResourceId = resourceId;
            this.ClientId = clientId;
            this.ClientKey = clientKey;
            this.SubscriptionKey = subscriptionKey;
            this.OrgId = orgId;
            this.DatasetId = datasetId;
        }

        #region fields

        Guid OrgId { get; set; }
        Guid DatasetId { get; set; }

        /// <summary>
        /// Gets or sets the authbase.
        /// </summary>
        /// <value>
        /// The authbase.
        /// </value>
        public String Authbase { get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public String ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client key.
        /// </summary>
        /// <value>
        /// The client key.
        /// </value>
        public String ClientKey { get; set; }

        /// <summary>
        /// Gets or sets the data collection application identifier.
        /// </summary>
        /// <value>
        /// The data collection application identifier.
        /// </value>
        public String ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        /// <value>
        /// The tenant identifier.
        /// </value>
        public String TenantId { get; set; }

        /// <summary>
        /// Gets or sets the subscription key.
        /// </summary>
        /// <value>
        /// The subscription key.
        /// </value>
        public String SubscriptionKey { get; set; }
        
        #endregion

        public Guid AddExternalFile(string json)
        {
            // now make the api request - pass the token to the header
            String token = GetToken();

            // perform the request
            return MakeRequest(token, json);
        }

        public Guid AddFile(string json, byte[] stream, string filename)
        {
            // now make the api request - pass the token to the header
            String token = GetToken();

            // perform the request
            return MakeRequestWithFile(token, null, json, stream, filename);
        }

        public Guid AddExternalFileVersion(Guid fileId, string json)
        {            
            // now make the api request - pass the token to the header
            String token = GetToken();

            // perform the request
            return MakeRequest(token, fileId, json);
        }

        public Guid AddFileVersion(Guid fileId, string json, byte[] stream, string filename)
        {            
            // now make the api request - pass the token to the header
            String token = GetToken();

            // perform the request
            return MakeRequestWithFile(token, fileId, json, stream, filename);
        }

        Guid MakeRequest(string token, string json)
        {
            return MakeRequest(token, null, json);
        }

        Guid MakeRequestWithFile(string token, Guid? fileid, string json, byte[] filedata, string filename)
        {
            using (var client = new HttpClient())
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                HttpResponseMessage response;

                // set up authentication with api subscription key
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                queryString["subscription-key"] = SubscriptionKey;

                string uri = String.Empty;

                if (!fileid.HasValue)
                    uri = String.Format(String.Concat(API_NEW_FILE_ENDPOINT, "?", queryString), this.OrgId, this.DatasetId);
                else
                    uri = String.Format(String.Concat(API_NEW_FILE_VERSION_ENDPOINT, "?", queryString), this.OrgId, this.DatasetId, fileid.Value);


                using (var multipartFormDataContent = new MultipartFormDataContent("multipart/form-data"))
                { 
                    // add the json
                    multipartFormDataContent.Add(new StringContent(json, Encoding.UTF8, "application/json"));
                    multipartFormDataContent.Add(new ByteArrayContent(filedata), "content", filename);

                    response = client.PostAsync(uri, multipartFormDataContent).Result;
                }

                #region the result

                if (response.Content != null)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode.Equals(System.Net.HttpStatusCode.OK))
                    {
                        dynamic resp = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);

                        return resp.RequestId;
                    }
                    else
                    {
                        throw new Exception(responseString);
                    }
                }

                #endregion

                // something went wrong, just pass it up
                throw new Exception(response.ReasonPhrase);
            }
        }

        Guid MakeRequest(string token, Guid? fileid, string json)
        {
            using (var client = new HttpClient())
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                HttpResponseMessage response;

                // set up authentication with api subscription key
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                queryString["subscription-key"] = SubscriptionKey;

                string uri = String.Empty;

                if (!fileid.HasValue)
                    uri = String.Format(String.Concat(API_NEW_FILE_ENDPOINT, "?", queryString), this.OrgId, this.DatasetId);
                else
                    uri = String.Format(String.Concat(API_NEW_FILE_VERSION_ENDPOINT, "?", queryString), this.OrgId, this.DatasetId, fileid.Value);

                byte[] byteData = Encoding.UTF8.GetBytes(json);

                using (var content = new ByteArrayContent(byteData))
                {
                    response = client.PostAsync(uri, content).Result;
                }

                #region the result 
                if (response.Content != null)
                {
                    var responseString = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode.Equals(System.Net.HttpStatusCode.OK))
                    {
                        dynamic resp = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);

                        return resp.RequestId;
                    }
                    else
                    {
                        throw new Exception(responseString);
                    }
                }
                #endregion

                // something went wrong, just pass it up
                throw new Exception(response.ReasonPhrase);
            }
        }
        
        /// <summary>
        /// Gets the token to make an oauth request.
        /// </summary>
        /// <returns></returns>
        string GetToken()
        {
            #region auth to get the access token to make the api request with

            AuthenticationContext context = new AuthenticationContext(Authbase + TenantId, false);

            // build a credential
            ClientCredential cred = new ClientCredential(ClientId, ClientKey);

            // get a token
            AuthenticationResult ar = context.AcquireTokenAsync(ResourceId, cred).Result;

            return ar.AccessToken;

            #endregion
        }
    }
}
