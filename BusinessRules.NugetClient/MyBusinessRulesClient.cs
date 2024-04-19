using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MyBusinessRules
{
    /// <summary>
    /// MyBusinessRulesClient lets you connect to https://www.mybizrules.com to execute business rules against a domain object. 
    /// </summary>
    public class MyBusinessRulesClient
    {
        private readonly HttpClient _client;
        
        /// <summary>
        /// Your currently configured API key for https://www.mybizrules.com
        /// </summary>
        public readonly string ApiKey;

        /// <summary>
        /// The base URL for access to https://www.mybizrules.com
        /// </summary>
        public static string BaseUrl { get; set; } = "https://www.mybizrules.com";

        /// <summary>
        /// Instantiates your client with a provided API key
        /// </summary>
        /// <param name="apiKey">An API key you have configured on https://www.mybizrules.com</param>
        public MyBusinessRulesClient(string apiKey)
        {
            _client = new HttpClient();
            ApiKey = apiKey;
        }

        /// <summary>
        /// Register a new client by providing a valid email address
        /// </summary>
        /// <param name="emailAddress">A valid email address that you will be using to manage your account on https://www.mybizrules.com</param>
        /// <returns>A named tuple where the first item is whether the registration was successful, and where the second item is a configured MyBusinessRules client activated using a new API key</returns>
        public static async Task<(bool wasSuccessful, MyBusinessRulesClient registeredClient)> Register(string emailAddress)
        {
            try
            {
                var client = new HttpClient();

                var serializedObject = JsonConvert.SerializeObject(emailAddress);
                var content = new StringContent(serializedObject, Encoding.UTF8, "application/json");

                var url = BaseUrl + "/Public/Register";
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var myBusinessRulesClient = new MyBusinessRulesClient(JsonConvert.DeserializeObject<string>(result));
                    return (true, myBusinessRulesClient);
                }
            }
            catch
            {
                //Swallow the error and fall through to return the default
            }

            return (false, null);
        }

        /// <summary>
        /// Executes your configured live business rules on https://www.mybizrules.com for a provided domain object
        /// </summary>
        /// <typeparam name="T">The type of your object</typeparam>
        /// <param name="inputObject">The input object against which you want to run your business rules</param>
        /// <param name="refreshObject">Indicates whether the object should be refreshed based on the provided input object</param>
        /// <returns>A named tuple where the first item is whether the execution was successful, and where the second item is your same object returned back to you, but augmented by the executed business rules</returns>
        public async Task<(bool wasSuccessful, T outputObject)> ExecuteRulesAsync<T>(T inputObject, bool refreshObject = false)
        {
            try
            {
                //TODO - use refreshObject
                var serializedObject = JsonConvert.SerializeObject(inputObject);
                var content = new StringContent(serializedObject, Encoding.UTF8, "application/json");

                var url = BaseUrl + "/Public/ExecuteRules?apiKey=" + ApiKey;
                var response = await _client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return (true, JsonConvert.DeserializeObject<T>(result));
                }
            }
            catch
            {
                //Swallow the error and fall through to return the default
            }

            return (false, default(T));
        }

        /// <summary>
        /// Executes your configured live and test business rules on https://www.mybizrules.com for a provided domain object
        /// </summary>
        /// <typeparam name="T">The type of your object</typeparam>
        /// <param name="inputObject">The input object against which you want to run your business rules</param>
        /// <returns>A named tuple where the first item is whether the execution was successful, and where the second item is your same object returned back to you, but augmented by the executed business rules</returns>
        public async Task<(bool wasSuccessful, T outputObject)> ExecuteTestRulesAsync<T>(T inputObject)
        {
            try
            {
                var serializedObject = JsonConvert.SerializeObject(inputObject);
                var content = new StringContent(serializedObject, Encoding.UTF8, "application/json");

                var url = BaseUrl + "/Public/ExecuteTestRules?apiKey=" + ApiKey;
                var response = await _client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return (true, JsonConvert.DeserializeObject<T>(result));
                }
            }
            catch
            {
                //Swallow the error and fall through to return the default
            }

            return (false, default(T));
            
        }
    }
}
