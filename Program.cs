using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;

namespace MongoConnectionDiagnostic;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var clientCertificate = config.GetValue<string>("Pki:ClientCertificate");
        var clientCertificateKey = config.GetValue<string>("Pki:ClientCertificateKey");
        var mongoDatabase = config.GetValue<string>("Mongo:DatabaseName");
        var mongoConnectionString = config.GetValue<string>("Mongo:ConnectionString");

        var vaultCertificates = clientCertificate.Split("\n-----END CERTIFICATE-----\n");

        AddIfNotPresentOrExpired(vaultCertificates[1]);

        var certificateFromDisk = X509Certificate2.CreateFromPem(clientCertificate, clientCertificateKey);
        var certificate = new X509Certificate2(certificateFromDisk.Export(X509ContentType.Pfx));

        var mongoSettings = MongoClientSettings.FromConnectionString(mongoConnectionString);

        mongoSettings.Credential = MongoCredential.CreateMongoX509Credential(null);
        mongoSettings.UseTls = true;
        mongoSettings.SslSettings = new SslSettings
        {
            ClientCertificates = [certificate],
        };

        try
        {
            var mongoClient = new MongoClient(mongoSettings);
            var mongoClientDatabase = mongoClient.GetDatabase(mongoDatabase);

            /*** Do something against the database. Read, list, etc. ***/
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public static void AddIfNotPresentOrExpired(string certificate)
    {
        var storeName = StoreName.My;

        using X509Store store = new X509Store(storeName, StoreLocation.CurrentUser);

        store.Open(OpenFlags.ReadWrite);

        var cacertificate = X509Certificate2.CreateFromPem(certificate);

        var orderedCertificatesInTrust = store.Certificates
            .OrderByDescending(x => x.NotAfter);

        var certificateInTrust = orderedCertificatesInTrust
            .FirstOrDefault(x => x.Thumbprint == cacertificate.Thumbprint);

        if (certificateInTrust == null || !certificateInTrust.Verify())
        {
            store.Add(cacertificate);
        }
    }

}