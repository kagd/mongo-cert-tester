# MongoDB Connection Diagnostic Tool

This workspace contains a .NET 8 console application designed to diagnose MongoDB connection issues using PKI certificates. It replicates the core functionality of the Chr.Common.Mongo library.

## Project Structure

- **MongoConnectionDiagnostic.csproj** - Main project file with dependencies
- **Program.cs** - Main application entry point with command line parsing
- **Configuration.cs** - Configuration classes (PkiConfigurationRecord, MongoConfigurationRecord)
- **X509StoreHelper.cs** - Certificate store management (replicates Chr.Common.Mongo behavior)
- **X509Manager.cs** - Certificate loading and management
- **MongoClientFactory.cs** - MongoDB client creation with PKI authentication
- **appsettings.json** - Sample configuration file

## Key Features

1. **Certificate Store Management**: Stores intermediate certificates in system trust store
2. **PKI Certificate Loading**: Loads certificates from PEM strings with proper error handling
3. **MongoDB Client Creation**: Creates MongoDB clients with X.509 authentication
4. **Connection Testing**: Tests actual MongoDB connections to verify certificate setup

## Usage

The application accepts either command line arguments or a configuration file:

### Command Line:

```bash
dotnet run -- --certificate "CERT_STRING" --private-key "KEY_STRING" --connection-string "mongodb+srv://..." --database-name "testdb"
```

### Configuration File:

```bash
dotnet run -- --config /abs/path/to/appsettings.json
```

## How It Replicates Chr.Common.Mongo

- **X509StoreHelper**: Mimics the AddIfNotPresentOrExpired method for certificate trust store management
- **X509Manager**: Replicates certificate loading, intermediate cert handling, and PFX export
- **MongoClientFactory**: Creates MongoDB clients with identical PKI settings (X.509 credential, SSL, etc.)
- **Certificate Processing**: Handles certificate chains and intermediate certificate extraction

## Development Notes

- Uses MongoDB.Driver 2.28.0 (same version as Chr.Common.Mongo)
- Targets .NET 8.0
- Includes proper error handling and detailed logging
- Cross-platform certificate store support (Windows/macOS/Linux)
