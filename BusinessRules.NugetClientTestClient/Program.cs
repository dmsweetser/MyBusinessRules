using MyBusinessRules;
using Newtonsoft.Json;

//This is staging stuff
Console.WriteLine("Press Enter to test");
Console.ReadLine();
MyBusinessRulesClient.BaseUrl = "https://localhost:7282";
//End staging stuff







//This is a sample class representing your domain object
var customer = new Customer()
{
    Name = "Test Name",
    Address = "Test Address"
};

var emailAddress = Guid.NewGuid().ToString("N") + "@mybizrules.com";

var registrationResult = await MyBusinessRulesClient.Register(emailAddress);

if (registrationResult.wasSuccessful)
{
    var myBusinessRulesClient = registrationResult.registeredClient;
    
    var apiKey = myBusinessRulesClient.ApiKey; //Store this API key for use later on

    //Call ExecuteRulesAsync, which runs your production business rules and returns an updated version of your domain object
    var liveResult = await myBusinessRulesClient.ExecuteRulesAsync(customer);

    //Alternatively, you can call ExecuteTestRulesAsync, which runs both your test and production rules
    var testResult = await myBusinessRulesClient.ExecuteTestRulesAsync(customer);

    if (liveResult.wasSuccessful)
    {
        //Simply re-assign the result back to your domain object and use it normally
        customer = liveResult.outputObject;
    }

    Console.WriteLine(JsonConvert.SerializeObject(customer));
}










Console.ReadLine();
