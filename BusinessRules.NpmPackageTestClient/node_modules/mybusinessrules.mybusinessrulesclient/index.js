const axios = require('axios');

class MyBusinessRulesClient {
  constructor(apiKey) {
    this.apiKey = apiKey;
  }

  async executeRules(inputObject) {
    try {
      const url = `https://www.mybizrules.com/Public/ExecuteRules?apiKey=${this.apiKey}`;
      const response = await axios.post(url, inputObject, {
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.status === 200) {
        return response.data;
      } else {
        return null;
      }
    } catch (error) {
      return null;
    }
  }
}

module.exports = MyBusinessRulesClient;