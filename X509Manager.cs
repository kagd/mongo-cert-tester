using System.Security.Cryptography.X509Certificates;

namespace MongoConnectionDiagnostic;

/// <summary>
/// Manages X.509 certificates for MongoDB client authentication.
/// Based on Chr.Common.Mongo X509Manager implementation.
/// </summary>
internal class X509Manager
{
  private X509Certificate2? certificate;
  private PkiConfigurationRecord pkiConfiguration;

  public X509Manager(PkiConfigurationRecord pkiConfiguration)
  {
    this.pkiConfiguration = pkiConfiguration;
  }

  /// <summary>
  /// Gets the X.509 certificate for MongoDB client authentication.
  /// </summary>
  /// <returns>The X.509 certificate.</returns>
  public X509Certificate2 GetCertificate()
  {
    return this.certificate ??= this.LoadPkiCertificate(this.pkiConfiguration);
  }

  /// <summary>
  /// Loads the PKI certificate from the given configuration record.
  /// </summary>
  /// <param name="pkiConfigurationRecord">The PKI configuration record.</param>
  /// <returns>The loaded X.509 certificate.</returns>
  private X509Certificate2 LoadPkiCertificate(PkiConfigurationRecord pkiConfigurationRecord)
  {
    if (string.IsNullOrEmpty(pkiConfigurationRecord.ClientCertificate))
    {
      throw new InvalidOperationException("Client certificate is required - basic authentication is not configured.");
    }

    Console.WriteLine("Loading PKI certificate...");

    // Assumes that ClientCertificate in PkiConfigurationRecord contains both the issued and intermediate certificates.
    // The intermediate certificate is going to be different per vault instance.
    // OpenSSL must be configured to trust the intermediate certificate from Vault.
    var vaultCertificates = pkiConfigurationRecord.ClientCertificate.Split(new string[] { "\n-----END CERTIFICATE-----\n" }, StringSplitOptions.None);

    if (vaultCertificates.Length > 1)
    {
      // The intermediate certificate is the second certificate in the chain (also the last).
      // The mongo driver defers to trust certificate authorities in a system's trust store
      var intermediateCert = vaultCertificates[1] + "\n-----END CERTIFICATE-----";
      Console.WriteLine("Adding intermediate certificate to trust store...");
      X509StoreHelper.AddIfNotPresentOrExpired(intermediateCert);
    }
    else
    {
      Console.WriteLine("No intermediate certificate found in the certificate chain.");
    }

    var certificateFromDisk = X509Certificate2.CreateFromPem(
        pkiConfigurationRecord.ClientCertificate,
        pkiConfigurationRecord.ClientCertificateKey);

    var exportableCert = new X509Certificate2(certificateFromDisk.Export(X509ContentType.Pfx));

    Console.WriteLine($"Certificate loaded successfully. Subject: {exportableCert.Subject}");
    Console.WriteLine($"Certificate valid from {exportableCert.NotBefore} to {exportableCert.NotAfter}");

    return exportableCert;
  }
}