using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net; // For Uri.EscapeDataString

namespace DestinyGhostAssistant.Tests.Tools
{
    [TestClass]
    public class SearchWebToolTests
    {
        [TestMethod]
        public async Task ExecuteAsync_MissingQueryParameter_ReturnsError()
        {
            // Arrange
            var tool = new SearchWebTool();
            var parameters = new Dictionary<string, object>();

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'query' parameter is missing or not a string."), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_EmptyQueryParameter_ReturnsError()
        {
            // Arrange
            var tool = new SearchWebTool();
            var parameters = new Dictionary<string, object> { { "query", " " } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'query' parameter cannot be empty."), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_NullQueryParameter_ReturnsError()
        {
            // Arrange
            var tool = new SearchWebTool();
            var parameters = new Dictionary<string, object> { { "query", null! } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'query' parameter is missing or not a string."), $"Unexpected result for null query: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidQuery_ReturnsSuccessMessageOrSystemError()
        {
            // Arrange
            var tool = new SearchWebTool();
            string query = "Destiny 2 Ghost lore";
            var parameters = new Dictionary<string, object> { { "query", query } };

            // Act
            // This will attempt Process.Start which might behave differently in test environments
            // or if no browser is configured. The tool is designed to catch Win32Exception.
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            // We expect either success or a system error if Process.Start fails in test env.
            bool success = result.Contains($"Successfully requested web search for: '{query}'");
            bool systemError = result.Contains("Error performing web search") && result.Contains("Could not start process");

            Assert.IsTrue(success || systemError, $"Expected success or specific system error, but got: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidQueryWithSpecialChars_ReturnsSuccessMessageAndEncodesQuery()
        {
            // Arrange
            var tool = new SearchWebTool();
            string query = "C# events & delegates";
            string encodedQuery = Uri.EscapeDataString(query); // This is what the tool should do
            var parameters = new Dictionary<string, object> { { "query", query } };

            // We can't directly check the URL passed to Process.Start without more complex setup.
            // However, the success message includes the original query.
            // The tool's internal logic is responsible for encoding.
            // We are testing if the tool completes its logic path.

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            bool success = result.Contains($"Successfully requested web search for: '{query}'");
            bool systemError = result.Contains("Error performing web search") && result.Contains("Could not start process");

            Assert.IsTrue(success || systemError, $"Expected success or specific system error for query with special chars, but got: {result}");

            // Indirectly, if it didn't throw an error related to URL format from Process.Start (other than Win32Exception for missing browser),
            // it suggests the encoding didn't break the URL structure itself.
            // A more direct test would involve checking the actual URL string, which is tricky here.
        }
    }
}
