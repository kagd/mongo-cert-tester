# MongoDB Connection Diagnostic Tool

This console application replicates the Chr.Common.Mongo library behavior for diagnosing MongoDB connection issues with PKI certificates.

## Features

- Stores PKI certificates in the certificate store (mimics X509StoreHelper)
- Creates MongoDB clients with X.509 certificate authentication
- Tests MongoDB connections using PKI certificates
- Handles both development and production certificate store scenarios

## Usage

1. **Basic usage with certificate string:**

   ```
   dotnet run -- --certificate "-----BEGIN CERTIFICATE-----...-----END CERTIFICATE-----" --private-key "-----BEGIN RSA PRIVATE KEY-----...-----END RSA PRIVATE KEY-----" --connection-string "mongodb+srv://your-cluster/?authSource=$external"
   ```

2. **Using configuration file:**
   ```
   dotnet run -- --config /absolute/path/to/appsettings.json
   ```

## Configuration

### Command Line Arguments

- `--certificate`: PEM-formatted certificate string
- `--private-key`: PEM-formatted private key string
- `--connection-string`: MongoDB connection string with authSource=$external
- `--config`: Path to configuration file

### Configuration File Format (appsettings.json)

```json
{
  "Pki": {
    "ClientCertificate": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----",
    "ClientCertificateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
  },
  "Mongo": {
    "ConnectionString": "mongodb+srv://your-cluster/?authSource=$external",
    "DatabaseName": "testdb"
  }
}
```

## How It Works

1. **Certificate Storage**: Uses X509StoreHelper to add intermediate certificates to the system trust store
2. **Certificate Loading**: Loads PKI certificates from PEM strings and creates X509Certificate2 objects
3. **MongoDB Client Creation**: Creates MongoClient with X.509 authentication and SSL settings
4. **Connection Testing**: Attempts to connect and list collections to verify the connection

## Based on Chr.Common.Mongo Library

This tool replicates the core functionality from the Chr.Common.Mongo library:

- X509StoreHelper.AddIfNotPresentOrExpired() method
- X509Manager certificate loading logic
- MongoClientFactory PKI client creation
- Proper SSL settings and certificate configuration

## Requirements

- .NET 8.0
- MongoDB.Driver 2.28.0
- Valid PKI certificate and private key from Vault
