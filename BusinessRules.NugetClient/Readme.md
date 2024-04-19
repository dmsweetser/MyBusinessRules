# MyBusinessRulesClient

MyBusinessRulesClient is a .NET Standard library that simplifies executing business rules via https://www.mybizrules.com. It encapsulates the logic to send JSON data to My Business Rules and handles the response, making it easy to integrate with the system.

Do you want to empower your business users to create and manage their own rules without depending on developers? My Business Rules! is the solution you need. Our application lets you define your business rules in a simple and intuitive way, using natural language and graphical interfaces. You can also test and validate your rules before enabling them in your production environment.

My Business Rules! is ideal for businesses that operate in industries where rules are constantly changing, such as insurance or finance. You can easily adapt your rules to new regulations, market conditions, or customer preferences, without waiting for developers to code them for you. Developers, on the other hand, can focus on building and maintaining your core systems, while integrating seamlessly with our API to apply your rules consistently across your applications.

## Installation

You can install this library via NuGet. Use the following command to install it into your project:

```shell
Install-Package MyBusinessRules.MyBusinessRulesClient -Version 1.0.6
```

## Usage - New Registration (C#)

```csharp
//This is a sample class representing your domain object
var customer = new Customer()
{
    Name = "Test Name",
    Address = "Test Address"
};

//Use a valid email address that you can use later to manage your rules https://www.mybizrules.com
var emailAddress = "VALID_EMAIL_ADDRESS";

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

    return customer;
}

```

## Usage - Existing Client (C#)

```csharp
//This is a sample class representing your domain object
var customer = new Customer()
{
    Name = "Test Name",
    Address = "Test Address"
};

//Instantiate the client using your provided API key from https://www.mybizrules.com
var myBusinessRulesClient = new MyBusinessRulesClient("%YOUR API KEY%");

//Call ExecuteRulesAsync, which runs your production business rules and returns an updated version of your domain object
var liveResult = await myBusinessRulesClient.ExecuteRulesAsync(customer);

//Alternatively, you can call ExecuteTestRulesAsync, which runs both your test and production rules
var testResult = await myBusinessRulesClient.ExecuteTestRulesAsync(customer);

if (liveResult.wasSuccessful)
{
    //Simply re-assign the result back to your domain object and use it normally
    customer = liveResult.outputObject;
}

return customer;
```