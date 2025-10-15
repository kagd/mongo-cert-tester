using MongoDB.Driver;

namespace MongoConnectionDiagnostic;

/// <summary>
/// Factory for creating MongoDB clients with X.509 certificate authentication.
/// Based on Chr.Common.Mongo MongoClientFactory implementation.
/// </summary>
internal class MongoClientFactory
{
  private readonly X509Manager x509Manager;

  public MongoClientFactory(X509Manager x509Manager)
  {
    this.x509Manager = x509Manager;
  }

  /// <summary>
  /// Creates a MongoDB client configured with X.509 certificate authentication.
  /// </summary>
  /// <param name="connectionString">The MongoDB connection string.</param>
  /// <returns>A configured MongoClient instance.</returns>
  public MongoClient CreatePkiClient(string connectionString)
  {
    var certificate = this.x509Manager.GetCertificate();
    var pkiSettings = MongoClientSettings.FromConnectionString(connectionString);

    Console.WriteLine("Creating MongoDB client with PKI authentication...");

    // Set heartbeat interval (from Chr.Common.Mongo implementation)
    pkiSettings.HeartbeatInterval = TimeSpan.FromSeconds(300);

    // The username here is inferred from the certificate's subject distinguished name when null.
    // Note: this forces the auth source to be $external regardless of what is configured in the connection string.
    // This is because x509 users can only exist in the $external database.
    pkiSettings.Credential = MongoCredential.CreateMongoX509Credential(null);
    pkiSettings.UseTls = true;
    pkiSettings.SslSettings = new SslSettings
    {
      ClientCertificates = new[] { certificate },
    };

    Console.WriteLine($"MongoDB client configured with certificate: {certificate.Subject}");
    Console.WriteLine($"Connection string: {connectionString}");

    return new MongoClient(pkiSettings);
  }
}