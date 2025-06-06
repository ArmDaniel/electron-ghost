using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services.Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Tests.Tools
{
    [TestClass]
    public class OpenUrlToolTests
    {
        [TestMethod]
        public async Task ExecuteAsync_MissingUrlParameter_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            var parameters = new Dictionary<string, object>();

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'url' parameter is missing or not a string."), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_EmptyUrlParameter_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            var parameters = new Dictionary<string, object> { { "url", " " } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'url' parameter cannot be empty."), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_NullUrlParameter_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            var parameters = new Dictionary<string, object> { { "url", null! } }; // Pass null explicitly

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: 'url' parameter is missing or not a string."), $"Unexpected result for null URL: {result}");
        }


        [TestMethod]
        public async Task ExecuteAsync_InvalidUrlScheme_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            var parameters = new Dictionary<string, object> { { "url", "ftp://example.com" } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: Invalid URL provided"), $"Unexpected result: {result}");
            Assert.IsTrue(result.Contains("Please provide a full URL starting with http:// or https://"), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_MalformedUrl_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            // Uri.TryCreate might handle "htp://invalid_url" differently than truly malformed.
            // Let's use something clearly not a URI for this specific test of Uri.TryCreate.
            var parameters = new Dictionary<string, object> { { "url", "this is not a url" } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: Invalid URL provided"), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_UrlWithoutScheme_ReturnsError()
        {
            // Arrange
            var tool = new OpenUrlTool();
            var parameters = new Dictionary<string, object> { { "url", "www.google.com" } }; // Missing scheme

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.Contains("Error: Invalid URL provided"), $"Unexpected result for URL without scheme: {result}");
             Assert.IsTrue(result.Contains("Please provide a full URL starting with http:// or https://"), $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidHttpUrl_ReturnsSuccessMessage()
        {
            // Arrange
            var tool = new OpenUrlTool();
            string validUrl = "http://www.google.com";
            var parameters = new Dictionary<string, object> { { "url", validUrl } };

            // Act
            // This will attempt Process.Start which might behave differently in test environments
            // or if no browser is configured. The tool is designed to catch Win32Exception.
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            // We expect either success or a system error if Process.Start fails in test env.
            bool success = result.Contains($"Successfully requested to open URL: {validUrl}");
            bool systemError = result.Contains("Error opening URL") && result.Contains("Could not start process");

            Assert.IsTrue(success || systemError, $"Expected success or specific system error, but got: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_ValidHttpsUrl_ReturnsSuccessMessage()
        {
            // Arrange
            var tool = new OpenUrlTool();
            string validUrl = "https://www.google.com";
            var parameters = new Dictionary<string, object> { { "url", validUrl } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            bool success = result.Contains($"Successfully requested to open URL: {validUrl}");
            bool systemError = result.Contains("Error opening URL") && result.Contains("Could not start process");

            Assert.IsTrue(success || systemError, $"Expected success or specific system error, but got: {result}");
        }
    }
}
