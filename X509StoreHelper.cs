using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace MongoConnectionDiagnostic;

/// <summary>
/// This class is responsible for adding x509 certificates to the runtime trust store. 
/// In linux containers, Vault CHR intermediate certificates are pre added to be trusted by OpenSSL.
/// Based on Chr.Common.Mongo X509StoreHelper implementation.
/// </summary>
internal static class X509StoreHelper
{
  public static void AddIfNotPresentOrExpired(string certificate)
  {
    var storeName = StoreName.My;

    using X509Store store = new X509Store(storeName, StoreLocation.CurrentUser);

    store.Open(OpenFlags.ReadWrite);

    var caCertificate = X509Certificate2.CreateFromPem(certificate);

    // Order by descending expiration date to ensure the newest certificate is at the top.
    var orderedCertificatesInTrust = store.Certificates
        .OrderByDescending(x => x.NotAfter);

    // Get intermediate cert from trust store by its thumbprint.
    var certificateInTrust = orderedCertificatesInTrust
        .FirstOrDefault(x => x.Thumbprint == caCertificate.Thumbprint);

    // Add intermediate cert to trust store if one does not exist or exists but is expired.
    if (certificateInTrust == null || !certificateInTrust.Verify())
    {
      store.Add(caCertificate);
      Console.WriteLine($"Added certificate to trust store. Thumbprint: {caCertificate.Thumbprint}");
    }
    else
    {
      Console.WriteLine($"Certificate already exists in trust store. Thumbprint: {caCertificate.Thumbprint}");
    }
  }

  private static bool IsDevelopment()
  {
    var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(env, "development", StringComparison.OrdinalIgnoreCase);
  }
}