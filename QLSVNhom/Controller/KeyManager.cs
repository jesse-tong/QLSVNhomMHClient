using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
// Use BouncyCastle to import PEM key as C# doesn't support it natively
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.IO;


namespace QLSVNhom.Controller.KeyManager
{
    public class RsaHelper
    {
        public static (string publicKeyPem, string privateKeyPem) GenerateRsaKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                return (ExportPublicKeyToPem(rsa), ExportPrivateKeyToPem(rsa));
            }
        }

        public static string ExportPublicKeyToPem(RSACryptoServiceProvider rsa)
        {
            return rsa.ExportRSAPublicKeyPem();
        }

        public static string ExportPrivateKeyToPem(RSACryptoServiceProvider rsa)
        {
            return rsa.ExportRSAPrivateKeyPem();
        }

    }

    public class RSAServiceProvider
    {
        private RSACryptoServiceProvider _rsa;
        public RSAServiceProvider(RSACryptoServiceProvider provider)
        {
            _rsa = provider;
        }
        public byte[] Encrypt<T>(T data)
        {
            return _rsa.Encrypt(Encoding.UTF8.GetBytes(data.ToString()), false);
        }

        public T Decrypt<T>(byte[] data)
        {
            return (T)Convert.ChangeType(Encoding.UTF8.GetString(_rsa.Decrypt(data, false)), typeof(T));
        }
    }

    public class RSAKeyManager
    {
        private static string dbFilePath = "privateKeys.db";
        public SqliteConnection? connection;

        public RSAKeyManager()
        {
            try
            {
                if (connection == null)
                {
                    this.connection = new SqliteConnection($"Data Source={dbFilePath};");
                    connection.Open();

                    // RSA keys will be stored in PEM format
                    var command = new SqliteCommand(@"
                        CREATE TABLE IF NOT EXISTS Keys (
                            KeyName TEXT PRIMARY KEY,
                            PublicKey TEXT,
                            PrivateKey TEXT
                        )
                    ", connection);

                    command.ExecuteNonQuery();
                    Console.WriteLine("Successfully connected to the client key database.");
                }
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"Error connecting to the client key database: {ex.Message}");
                connection = null;
            }
        }

        public static void CloseConnection(SqliteConnection connection)
        {
            if (connection != null)
            {
                connection.Close();
                Console.WriteLine("Connection closed.");
            }
        }

        public void SaveKeys(string keyName, string publicKeyPem, string privateKeyPem)
        {

            using (var command = new SqliteCommand(@"
                INSERT OR REPLACE INTO Keys (KeyName, PublicKey, PrivateKey)
                VALUES (@KeyName, @PublicKey, @PrivateKey)
            ", connection))
            {
                command.Parameters.AddWithValue("@KeyName", keyName);
                command.Parameters.AddWithValue("@PublicKey", publicKeyPem);
                command.Parameters.AddWithValue("@PrivateKey", privateKeyPem);

                command.ExecuteNonQuery();
            }
        }

        public (string publicKeyPem, string privateKeyPem) GetKeys(string keyName)
        {
            using var command = new SqliteCommand(@"
                    SELECT PublicKey, PrivateKey
                    FROM Keys
                    WHERE KeyName = @KeyName
                ", connection);
            command.Parameters.AddWithValue("@KeyName", keyName);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (reader.GetString(0), reader.GetString(1));
            }
            else
            {
                throw new SqliteException("Key not found.", 2400);
            }
        }

        public void DeleteKeys(string keyName) {

            using var command = new SqliteCommand(@"
                DELETE FROM Keys
                WHERE KeyName = @KeyName
            ", connection);
            command.Parameters.AddWithValue("@KeyName", keyName);

            command.ExecuteNonQuery();
        }

        public RSAServiceProvider getKeyProvider(string keyName, string? publicKey = null)
        {

            using var command = new SqliteCommand(@"
                    SELECT PublicKey, PrivateKey
                    FROM Keys
                    WHERE KeyName = @KeyName
                ", connection);
            command.Parameters.AddWithValue("@KeyName", keyName);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                string publicKeyPem;
                if (publicKey == null)
                {
                    publicKeyPem = reader.GetString(0);
                }
                else
                {
                    publicKeyPem = publicKey;
                }
                string privateKeyPem = reader.GetString(1);

                // Import private and public keys from PEM

                var privateKeyReader = new StringReader(privateKeyPem);
                var privatePemReader = new PemReader(privateKeyReader);
                var privateKeyParams = (AsymmetricCipherKeyPair)privatePemReader.ReadObject();

                var publicKeyReader = new StringReader(publicKeyPem);
                var publicPemReader = new PemReader(publicKeyReader);
                var publicKeyParams = (AsymmetricKeyParameter)publicPemReader.ReadObject();

                var rsa = new RSACryptoServiceProvider();
                // Need to import public key parameters first as importing public key parameters will overwrite the existing private key parameters
                rsa.ImportParameters(DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKeyParams));
                rsa.ImportParameters(DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)privateKeyParams.Private));

                return new RSAServiceProvider(rsa);
            }
            else
            {
                throw new SqliteException("Key not found.", 2400);
            }
        }
    }
}