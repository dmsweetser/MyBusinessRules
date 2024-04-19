const express = require('express');
const axios = require('axios');
const bodyParser = require('body-parser');
const MyBusinessRulesClient = require('mybusinessrules.mybusinessrulesclient');

const app = express();
const port = 3000;

// This represents your current domain object
var domainObject = { name: "", address: "" };

// Create an instance of MyBusinessRulesClient with your API key
const myBusinessRulesClient = new MyBusinessRulesClient('958ff8cc-f71e-40a8-9d22-7f50fa024f32');

// This is a sample template of the HTML rendered on the page
function getTemplate(currentObject)
{ 
  return `
<form method="POST" action="/process">
  <label for="name">Name:</label>
  <input type="text" id="name" name="name" value="${currentObject.name}" required>
  <br>
  <label for="address">Address:</label>
  <input type="text" id="address" name="address" value="${currentObject.address}" required>
  <br>
  <button type="submit">Submit</button>
</form>
<a href="/">Back to Form</a>
`;
}

app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

app.get('/', async (req, res) => {
  // Render the updated values back to the user
  res.send(getTemplate(domainObject));
});

app.post('/process', async (req, res) => {
  domainObject = req.body;

  // Call executeRules and update the domainObject with the result
  var result = await myBusinessRulesClient.executeRules(domainObject);
  if (result !== null){
    domainObject = result;
  } else {
    // Handle your error condition here
  }

  // Render the updated values back to the user
  res.send(getTemplate(domainObject));
});

app.listen(port, () => {
  console.log(`Server is running on port ${port}`);
});
