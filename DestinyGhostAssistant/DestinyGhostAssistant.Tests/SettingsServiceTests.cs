using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services;
using DestinyGhostAssistant.Models;
using System.IO;
using System; // Required for Environment
using System.Text.Json; // Required for JsonSerializer
using System.Linq; // Required for EnumerateFileSystemEntries().Any()

namespace DestinyGhostAssistant.Tests
{
    [TestClass]
    public class SettingsServiceTests
    {
        private static string GetActualSettingsFilePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataFolderName = "DestinyGhostAssistant";
            string settingsFileName = "app_settings.json";

            string appFolderPath = Path.Combine(localAppDataPath, appDataFolderName);
            Directory.CreateDirectory(appFolderPath);
            return Path.Combine(appFolderPath, settingsFileName);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            string filePath = GetActualSettingsFilePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            string filePath = GetActualSettingsFilePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            // Optional: Clean up directory if empty and if it's safe (e.g. it's the test-specific one)
            // string appFolderPath = Path.GetDirectoryName(filePath)!;
            // if (Directory.Exists(appFolderPath) && appFolderPath.Contains(appDataFolderName) && !Directory.EnumerateFileSystemEntries(appFolderPath).Any())
            // {
            //    Directory.Delete(appFolderPath);
            // }
        }

        [TestMethod]
        public void SaveAndLoadSettings_Success()
        {
            // Arrange
            var settingsService = new SettingsService();
            var originalSettings = new AppSettings
            {
                SelectedOpenRouterModel = "test/model-v1",
                CustomSystemPrompt = "This is a custom test prompt."
            };

            // Act
            settingsService.SaveSettings(originalSettings);
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null.");
            Assert.AreEqual(originalSettings.SelectedOpenRouterModel, loadedSettings.SelectedOpenRouterModel, "SelectedOpenRouterModel should match.");
            Assert.AreEqual(originalSettings.CustomSystemPrompt, loadedSettings.CustomSystemPrompt, "CustomSystemPrompt should match.");
        }

        [TestMethod]
        public void LoadSettings_ReturnsDefaults_WhenFileDoesNotExist()
        {
            // Arrange
            var settingsService = new SettingsService();
            // TestInitialize ensures the file does not exist.

            // Act
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null even if file doesn't exist.");
            Assert.AreEqual("gryphe/mythomax-l2-13b", loadedSettings.SelectedOpenRouterModel, "Default SelectedOpenRouterModel should be applied.");
            Assert.IsNull(loadedSettings.CustomSystemPrompt, "Default CustomSystemPrompt should be null.");
        }

        [TestMethod]
        public void LoadSettings_HandlesCorruptJson_ReturnsDefaults()
        {
            // Arrange
            var settingsService = new SettingsService();
            string filePath = GetActualSettingsFilePath();
            File.WriteAllText(filePath, "this is not valid json {{{{");

            // Act
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null after corrupt JSON.");
            Assert.AreEqual("gryphe/mythomax-l2-13b", loadedSettings.SelectedOpenRouterModel, "Default model on corrupt JSON.");
            Assert.IsNull(loadedSettings.CustomSystemPrompt, "Default prompt on corrupt JSON.");
        }

        [TestMethod]
        public void LoadSettings_HandlesEmptyJsonFile_ReturnsDefaults()
        {
            // Arrange
            var settingsService = new SettingsService();
            string filePath = GetActualSettingsFilePath();
            File.WriteAllText(filePath, ""); // Empty file

            // Act
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null after empty JSON file.");
            Assert.AreEqual("gryphe/mythomax-l2-13b", loadedSettings.SelectedOpenRouterModel, "Default model on empty JSON file.");
            Assert.IsNull(loadedSettings.CustomSystemPrompt, "Default prompt on empty JSON file.");
        }

        [TestMethod]
        public void LoadSettings_HandlesJsonNullLiteral_ReturnsDefaultsWithSpecificModel()
        {
            // Arrange
            var settingsService = new SettingsService();
            string filePath = GetActualSettingsFilePath();
            File.WriteAllText(filePath, "null"); // JSON literal "null"

            // Act
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings, "Loaded settings should not be null for JSON 'null' literal.");
            // SettingsService.LoadSettings logic: if deserialized settings is null, it calls GetDefaultAppSettings().
            // If it's an empty file, it also calls GetDefaultAppSettings().
            // If it's a valid JSON but some fields are missing, it applies defaults to those fields.
            // The `SelectedOpenRouterModel ??= GetDefaultAppSettings().SelectedOpenRouterModel;` line handles if the *object* exists but the *property* is null.
            // If the whole JSON is "null", Deserialize returns null, so GetDefaultAppSettings() is called.
            Assert.AreEqual("gryphe/mythomax-l2-13b", loadedSettings.SelectedOpenRouterModel, "Default model on JSON 'null' literal.");
            Assert.IsNull(loadedSettings.CustomSystemPrompt, "Default prompt on JSON 'null' literal.");
        }

        [TestMethod]
        public void LoadSettings_PopulatesDefaultModel_IfModelIsNullInFile()
        {
            // Arrange
            var settingsService = new SettingsService();
            string filePath = GetActualSettingsFilePath();
            var settingsWithNullModel = new AppSettings { CustomSystemPrompt = "custom prompt test", SelectedOpenRouterModel = null };
            string json = JsonSerializer.Serialize(settingsWithNullModel);
            File.WriteAllText(filePath, json);

            // Act
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings);
            Assert.AreEqual("gryphe/mythomax-l2-13b", loadedSettings.SelectedOpenRouterModel, "Model should be populated with default if null in file.");
            Assert.AreEqual("custom prompt test", loadedSettings.CustomSystemPrompt, "Custom prompt should be preserved.");
        }

        [TestMethod]
        public void SaveSettings_NullCustomPrompt_IsSavedAndLoadedAsNull()
        {
            // Arrange
            var settingsService = new SettingsService();
            var originalSettings = new AppSettings
            {
                SelectedOpenRouterModel = "test-model-for-null-prompt",
                CustomSystemPrompt = null
            };

            // Act
            settingsService.SaveSettings(originalSettings);
            AppSettings loadedSettings = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedSettings);
            Assert.AreEqual("test-model-for-null-prompt", loadedSettings.SelectedOpenRouterModel);
            Assert.IsNull(loadedSettings.CustomSystemPrompt, "CustomSystemPrompt should be null when saved as null.");
        }
         [TestMethod]
        public void SaveSettings_DoesNotNullOverwriteGoodValuesWithDefaults_IfFieldMissingInSavedObject()
        {
            // Arrange
            var settingsService = new SettingsService();
            string filePath = GetActualSettingsFilePath();

            // Simulate a settings file that only has SelectedOpenRouterModel (e.g. from an older version)
            string partialJson = "{\"SelectedOpenRouterModel\": \"specific-model-partial-save\"}";
            File.WriteAllText(filePath, partialJson);

            // Act: Load these partial settings. CustomSystemPrompt will be null.
            AppSettings loadedOnce = settingsService.LoadSettings();
            Assert.AreEqual("specific-model-partial-save", loadedOnce.SelectedOpenRouterModel);
            Assert.IsNull(loadedOnce.CustomSystemPrompt); // It was missing, so it's null (correct)

            // Modify only one property (e.g. user changes model but doesn't touch prompt which is null)
            loadedOnce.SelectedOpenRouterModel = "new-model-partial-save";
            settingsService.SaveSettings(loadedOnce); // Save it back

            // Act: Load again
            AppSettings loadedTwice = settingsService.LoadSettings();

            // Assert
            Assert.IsNotNull(loadedTwice);
            Assert.AreEqual("new-model-partial-save", loadedTwice.SelectedOpenRouterModel);
            Assert.IsNull(loadedTwice.CustomSystemPrompt, "CustomSystemPrompt should remain null as it was never set by user.");
        }
    }
}
