using System.ComponentModel.DataAnnotations;

namespace MongoConnectionDiagnostic;

/// <summary>
/// The configuration object for an x509 certificate used to communicate with Mongo.
/// Based on Chr.Common.Mongo PkiConfigurationRecord.
/// </summary>
public class PkiConfigurationRecord
{
  /// <summary>
  /// Gets or sets the client certificate used to configure the ssl settings in MongoDB client.
  /// This should contain both the issued certificate and intermediate certificate in PEM format.
  /// </summary>
  public string ClientCertificate { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the certificate private key in PEM format.
  /// </summary>
  public string ClientCertificateKey { get; set; } = string.Empty;
}

/// <summary>
/// The configuration object for MongoDB connection.
/// Based on Chr.Common.Mongo MongoConfigurationRecord.
/// </summary>
public class MongoConfigurationRecord
{
  /// <summary>
  /// Gets or sets the connection string used to configure the MongoDB client.
  /// Should include authSource=$external for X.509 authentication.
  /// </summary>
  [Required]
  public string ConnectionString { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the database name.
  /// </summary>
  [Required]
  public string DatabaseName { get; set; } = string.Empty;
}

/// <summary>
/// Application configuration containing both PKI and MongoDB settings.
/// </summary>
public class AppConfiguration
{
  public PkiConfigurationRecord Pki { get; set; } = new();
  public MongoConfigurationRecord Mongo { get; set; } = new();
}