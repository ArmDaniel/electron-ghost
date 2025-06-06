using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services;
using System.IO; // Required for Path and File operations if directly checking file
using System; // Required for Environment

namespace DestinyGhostAssistant.Tests
{
    [TestClass]
    public class ApiKeyServiceTests
    {
        // Helper to get the actual path ApiKeyService uses, for direct file assertions if needed.
        // This is for test verification purposes only, not to change ApiKeyService behavior.
        private static string GetActualApiKeyFilePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // These must match the private static readonly fields in ApiKeyService
            string appDataFolderName = "DestinyGhostAssistant";
            string apiKeyFileName = "settings.dat";

            string appFolderPath = Path.Combine(localAppDataPath, appDataFolderName);
            return Path.Combine(appFolderPath, apiKeyFileName);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Ensures a clean state by deleting the *actual* file ApiKeyService uses before each test.
            // This makes tests operate on the real path but isolates them by cleaning up.
            ApiKeyService.DeleteApiKey();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up after each test by deleting the *actual* file.
            ApiKeyService.DeleteApiKey();
        }

        [TestMethod]
        public void SaveAndLoadApiKey_Success()
        {
            // Arrange
            string testKey = "test_api_key_123_save_load";

            // Act
            ApiKeyService.SaveApiKey(testKey);
            string? loadedKey = ApiKeyService.LoadApiKey();

            // Assert
            Assert.AreEqual(testKey, loadedKey, "Loaded key should match the saved key.");
        }

        [TestMethod]
        public void HasApiKey_ReturnsTrue_WhenKeyExistsAndNotEmpty()
        {
            // Arrange
            string testKey = "test_key_for_hasapikey";
            ApiKeyService.SaveApiKey(testKey);

            // Act
            bool hasKey = ApiKeyService.HasApiKey();

            // Assert
            Assert.IsTrue(hasKey, "HasApiKey should return true when a valid key has been saved.");
        }

        [TestMethod]
        public void HasApiKey_ReturnsFalse_WhenKeyDoesNotExist()
        {
            // Arrange (TestInitialize ensures file is deleted)

            // Act
            bool hasKey = ApiKeyService.HasApiKey();

            // Assert
            Assert.IsFalse(hasKey, "HasApiKey should return false when no key file exists.");
        }

        [TestMethod]
        public void HasApiKey_ReturnsFalse_WhenKeyFileIsEmpty()
        {
            // Arrange
            // Directly create an empty file at the location ApiKeyService uses
            string filePath = GetActualApiKeyFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!); // Ensure directory exists
            File.WriteAllText(filePath, "");

            // Act
            bool hasKey = ApiKeyService.HasApiKey();

            // Assert
            Assert.IsFalse(hasKey, "HasApiKey should return false when the key file is empty.");
        }

        [TestMethod]
        public void HasApiKey_ReturnsFalse_WhenKeyFileIsWhitespace()
        {
            // Arrange
            string filePath = GetActualApiKeyFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "   ");

            // Act
            bool hasKey = ApiKeyService.HasApiKey();

            // Assert
            Assert.IsFalse(hasKey, "HasApiKey should return false when the key file contains only whitespace.");
        }


        [TestMethod]
        public void LoadApiKey_ReturnsNull_WhenKeyDoesNotExist()
        {
            // Arrange (TestInitialize ensures file is deleted)

            // Act
            string? loadedKey = ApiKeyService.LoadApiKey();

            // Assert
            Assert.IsNull(loadedKey, "LoadApiKey should return null when no key file exists.");
        }

        [TestMethod]
        public void DeleteApiKey_RemovesFile()
        {
            // Arrange
            string testKey = "key_to_delete_123";
            ApiKeyService.SaveApiKey(testKey);
            // Verify setup: file should exist after save
            Assert.IsTrue(File.Exists(GetActualApiKeyFilePath()), "Test setup failed: API key file should exist after saving.");

            // Act
            ApiKeyService.DeleteApiKey();

            // Assert
            Assert.IsFalse(File.Exists(GetActualApiKeyFilePath()), "File should be deleted after DeleteApiKey is called.");
            Assert.IsFalse(ApiKeyService.HasApiKey(), "HasApiKey should return false after deletion.");
        }

        [TestMethod]
        public void SaveApiKey_NullOrWhitespace_DoesNotSave()
        {
            // Arrange (TestInitialize ensures no file initially)
            string filePath = GetActualApiKeyFilePath();

            // Act & Assert for null
            ApiKeyService.SaveApiKey(null!); // Test with null
            Assert.IsFalse(File.Exists(filePath), "File should not be created for null API key.");
            Assert.IsFalse(ApiKeyService.HasApiKey(), "HasApiKey should be false after attempting to save null.");

            // Act & Assert for whitespace
            ApiKeyService.SaveApiKey("   "); // Test with whitespace
            Assert.IsFalse(File.Exists(filePath), "File should not be created for whitespace API key.");
            Assert.IsFalse(ApiKeyService.HasApiKey(), "HasApiKey should be false after attempting to save whitespace.");
        }

        [TestMethod]
        public void LoadApiKey_HandlesEmptyFileGracefully_ReturnsNull()
        {
            // Arrange
            string filePath = GetActualApiKeyFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!); // Ensure directory exists
            File.WriteAllText(filePath, ""); // Create an empty file

            // Act
            string? loadedKey = ApiKeyService.LoadApiKey();

            // Assert
            // Based on ApiKeyService logic: "if (string.IsNullOrWhiteSpace(apiKey)) { return null; }"
            Assert.IsNull(loadedKey, "LoadApiKey should return null for an empty file.");
        }

        [TestMethod]
        public void LoadApiKey_HandlesWhitespaceFileGracefully_ReturnsNull()
        {
            // Arrange
            string filePath = GetActualApiKeyFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, "   \t   \n  "); // Create a file with only whitespace

            // Act
            string? loadedKey = ApiKeyService.LoadApiKey();

            // Assert
            Assert.IsNull(loadedKey, "LoadApiKey should return null for a file with only whitespace.");
        }
    }
}
