using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MongoConnectionDiagnostic;

class Program
{
  private static StreamWriter? _logWriter;

  static async Task Main(string[] args)
  {
    // Initialize logging
    InitializeLogging();

    LogLine("MongoDB Connection Diagnostic Tool");
    LogLine("==================================");
    LogLine("This tool replicates Chr.Common.Mongo library behavior for PKI certificate authentication.\n");

    try
    {
      // Parse command line arguments and configuration
      var config = ParseConfiguration(args);

      if (config == null)
      {
        ShowUsage();
        return;
      }

      // Validate configuration
      if (string.IsNullOrEmpty(config.Pki.ClientCertificate) || string.IsNullOrEmpty(config.Pki.ClientCertificateKey))
      {
        LogLine("❌ Error: PKI certificate and private key are required.");
        ShowUsage();
        return;
      }

      if (string.IsNullOrEmpty(config.Mongo.ConnectionString))
      {
        LogLine("❌ Error: MongoDB connection string is required.");
        ShowUsage();
        return;
      }

      LogLine("Step 1: Loading and storing PKI certificate...");
      LogLine("==============================================");

      // Create X509Manager and load certificate (this will also store intermediate certs)
      var x509Manager = new X509Manager(config.Pki);
      var certificate = x509Manager.GetCertificate();

      LogLine("✅ Certificate loaded successfully!\n");

      LogLine("Step 2: Creating MongoDB client with PKI authentication...");
      LogLine("========================================================");

      // Create MongoDB client factory and client
      var mongoClientFactory = new MongoClientFactory(x509Manager);
      var mongoClient = mongoClientFactory.CreatePkiClient(config.Mongo.ConnectionString);

      LogLine("✅ MongoDB client created successfully!\n");

      LogLine("Step 3: Testing MongoDB connection...");
      LogLine("====================================");

      // Test the connection by listing databases
      await TestMongoConnection(mongoClient, config.Mongo.DatabaseName);

      LogLine("\n🎉 SUCCESS: MongoDB PKI authentication test completed successfully!");
      LogLine("The certificate has been stored and the MongoDB client can connect.");
    }
    catch (Exception ex)
    {
      LogLine($"\n❌ ERROR: {ex.Message}");
      LogLine($"Stack trace: {ex.StackTrace}");
    }
    finally
    {
      CloseLogging();
    }

    LogLine("\nPress any key to exit...");
    Console.ReadKey();
  }

  private static AppConfiguration? ParseConfiguration(string[] args)
  {
    var configBuilder = new ConfigurationBuilder();

    // Add command line arguments
    configBuilder.AddCommandLine(args);
    var commandLineConfig = configBuilder.Build();

    // Check if a config file was specified
    var configFile = commandLineConfig["config"];
    if (!string.IsNullOrEmpty(configFile))
    {
      // If it's a relative path, make it relative to the project directory
      if (!Path.IsPathRooted(configFile))
      {
        // Get the directory where the executable is located
        var executableDir = AppContext.BaseDirectory;
        // Navigate up from bin/Debug/net8.0/ to the project root
        var projectDir = Directory.GetParent(executableDir)?.Parent?.Parent?.FullName;
        if (projectDir != null)
        {
          configFile = Path.Combine(projectDir, configFile);
        }
      }

      if (!File.Exists(configFile))
      {
        LogLine($"❌ Configuration file not found: {configFile}");
        LogLine($"   Looked in: {Path.GetFullPath(configFile)}");
        return null;
      }

      configBuilder = new ConfigurationBuilder();
      configBuilder.AddJsonFile(configFile);
      var configuration = configBuilder.Build();

      var config = new AppConfiguration();
      configuration.Bind(config);
      return config;
    }

    // Parse from command line arguments
    var certificate = commandLineConfig["certificate"];
    var privateKey = commandLineConfig["private-key"];
    var connectionString = commandLineConfig["connection-string"];
    var databaseName = commandLineConfig["database-name"] ?? "test";

    if (string.IsNullOrEmpty(certificate) || string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(connectionString))
    {
      return null;
    }

    return new AppConfiguration
    {
      Pki = new PkiConfigurationRecord
      {
        ClientCertificate = certificate,
        ClientCertificateKey = privateKey
      },
      Mongo = new MongoConfigurationRecord
      {
        ConnectionString = connectionString,
        DatabaseName = databaseName
      }
    };
  }

  private static async Task TestMongoConnection(MongoClient mongoClient, string databaseName)
  {
    try
    {
      LogLine("Testing connection by listing databases...");

      // List databases to test connection
      var databases = await mongoClient.ListDatabaseNamesAsync();
      var dbList = await databases.ToListAsync();

      LogLine($"✅ Successfully connected! Found {dbList.Count} databases:");
      foreach (var db in dbList.Take(5)) // Show first 5 databases
      {
        LogLine($"   - {db}");
      }

      if (dbList.Count > 5)
      {
        LogLine($"   ... and {dbList.Count - 5} more");
      }

      // Test accessing the specific database
      if (!string.IsNullOrEmpty(databaseName))
      {
        LogLine($"\nTesting access to database '{databaseName}'...");
        var database = mongoClient.GetDatabase(databaseName);
        var collections = await database.ListCollectionNamesAsync();
        var collectionList = await collections.ToListAsync();

        LogLine($"✅ Successfully accessed database '{databaseName}'. Found {collectionList.Count} collections.");

        if (collectionList.Any())
        {
          LogLine("Collections:");
          foreach (var collection in collectionList.Take(5))
          {
            LogLine($"   - {collection}");
          }
          if (collectionList.Count > 5)
          {
            LogLine($"   ... and {collectionList.Count - 5} more");
          }
        }
      }
    }
    catch (Exception ex)
    {
      LogLine($"❌ Connection test failed: {ex.Message}");
      throw;
    }
  }

  private static void ShowUsage()
  {
    LogLine("Usage:");
    LogLine("1. Using command line arguments:");
    LogLine("   dotnet run -- --certificate \"-----BEGIN CERTIFICATE-----...\" --private-key \"-----BEGIN RSA PRIVATE KEY-----...\" --connection-string \"mongodb+srv://...\" [--database-name \"test\"]");
    LogLine("");
    LogLine("2. Using configuration file:");
    LogLine("   dotnet run -- --config appsettings.json");
    LogLine("");
    LogLine("Configuration file format (appsettings.json):");
    LogLine("{");
    LogLine("  \"Pki\": {");
    LogLine("    \"ClientCertificate\": \"-----BEGIN CERTIFICATE-----\\n...\\n-----END CERTIFICATE-----\",");
    LogLine("    \"ClientCertificateKey\": \"-----BEGIN RSA PRIVATE KEY-----\\n...\\n-----END RSA PRIVATE KEY-----\"");
    LogLine("  },");
    LogLine("  \"Mongo\": {");
    LogLine("    \"ConnectionString\": \"mongodb+srv://your-cluster/?authSource=$external\",");
    LogLine("    \"DatabaseName\": \"testdb\"");
    LogLine("  }");
    LogLine("}");
    LogLine("");
    LogLine("Note: The certificate string should be obtained from 'dotnet vault transform' command.");
  }

  private static void InitializeLogging()
  {
    try
    {
      var logPath = "lastRun.txt";
      _logWriter = new StreamWriter(logPath, false); // false = overwrite existing file
      _logWriter.AutoFlush = true;

      // Write header with timestamp
      var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      _logWriter.WriteLine($"=== MongoDB Connection Diagnostic Tool - Run started at {timestamp} ===");
      _logWriter.WriteLine();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Warning: Could not initialize log file: {ex.Message}");
    }
  }

  private static void LogLine(string message)
  {
    // Write to console
    Console.WriteLine(message);

    // Write to log file if available
    try
    {
      _logWriter?.WriteLine(message);
    }
    catch
    {
      // Silently ignore logging errors to not disrupt the main application
    }
  }

  private static void Log(string message)
  {
    // Write to console
    Console.Write(message);

    // Write to log file if available
    try
    {
      _logWriter?.Write(message);
    }
    catch
    {
      // Silently ignore logging errors to not disrupt the main application
    }
  }

  private static void CloseLogging()
  {
    try
    {
      if (_logWriter != null)
      {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _logWriter.WriteLine();
        _logWriter.WriteLine($"=== Run completed at {timestamp} ===");
        _logWriter.Close();
        _logWriter.Dispose();
      }
    }
    catch
    {
      // Silently ignore cleanup errors
    }
  }
}