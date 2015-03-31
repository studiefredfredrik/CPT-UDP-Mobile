using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Our crypto imports
using System.Security;
using System.Security.Cryptography;
namespace CPTTCPmobile
{
	public class Cryptography
	{
			private string privateKey = "";
			public string publicKey = "";
			public Cryptography()
			{
				// Some great intro here:
				// http://geraldgibson.net/dnn/Home/2WayEncryptedCommunication/tabid/150/Default.aspx

				// Generate private and public keypair
				string[] keypair = generateKeypair_PrivatePublic();
				privateKey = keypair[0];
				publicKey = keypair[1];
			}
			private string[] generateKeypair_PrivatePublic() 
			{
				//Setup crypto
				CspParameters cspParams = new CspParameters();
				//cspParams.Flags = CspProviderFlags.UseMachineKeyStore;
				cspParams.Flags = CspProviderFlags.UseDefaultKeyContainer;
				//RSACryptoServiceProvider crypto = new RSACryptoServiceProvider(cspParams);

				// Use custom key size for better security. Keep in mind that:
				// 16384bit RSA operations are 4096x as slow as 1024bit RSA operations.
				RSACryptoServiceProvider crypto = new RSACryptoServiceProvider(384, cspParams);
				crypto.PersistKeyInCsp = false;
				// The RSACryptoServiceProvider supports key sizes from 384 bits to 16384 bits
				// in increments of 8 bits if you have the Microsoft Enhanced Cryptographic Provider installed
				// The Basic provider supports 512bits
				// You cannot set the KeySize directly, (you can not: crypto.KeySize = 512;)
				// it has to passed when creating the provider 

				string[] keys = { "", "" };
				//Get private key
				keys[0] = crypto.ToXmlString(true);
				//Get public key
				keys[1] = crypto.ToXmlString(false);

				return keys;
			}

			public string EncryptMessage(string publicKey, string messagePlainText)
			{
				//Setup 
				RSACryptoServiceProvider crypto = new RSACryptoServiceProvider();
				//Use the Public Key to do the encryption
				crypto.FromXmlString(publicKey);

				//Encrypt
				byte[] stringEncryptedAsByteArray = crypto.Encrypt(System.Text.Encoding.UTF8.GetBytes(messagePlainText), false);

				//Show result
				return Convert.ToBase64String(stringEncryptedAsByteArray);
			}
			public string EncryptMessage(string messagePlainText) // Uses the object defined public key to encrypt
			{
				//Setup 
				RSACryptoServiceProvider crypto = new RSACryptoServiceProvider();
				//Use the Public Key to do the encryption
				crypto.FromXmlString(publicKey);

				//Encrypt
				byte[] stringEncryptedAsByteArray = crypto.Encrypt(System.Text.Encoding.UTF8.GetBytes(messagePlainText), false);

				//Show result
				return Convert.ToBase64String(stringEncryptedAsByteArray);
			}

			public string DecryptMessage(string privateKey, string messageEncrypted)
			{
				//Setup 
				RSACryptoServiceProvider crypto = new RSACryptoServiceProvider();

				//Decrypt 
				crypto = new RSACryptoServiceProvider();
				crypto.FromXmlString(privateKey);
				byte[] stringDecryptedAsByteArray = crypto.Decrypt(Convert.FromBase64String(messageEncrypted), false);

				//Show result
				return System.Text.Encoding.UTF8.GetString(stringDecryptedAsByteArray);
			}
			public string DecryptMessage(string messageEncrypted) // Uses the object defined private key to decrypt
			{
				try
				{
					//Setup 
					RSACryptoServiceProvider crypto = new RSACryptoServiceProvider();

					//Decrypt 
					crypto = new RSACryptoServiceProvider();
					crypto.FromXmlString(privateKey);
					byte[] stringDecryptedAsByteArray = crypto.Decrypt(Convert.FromBase64String(messageEncrypted), false);

					//Show result
					return System.Text.Encoding.UTF8.GetString(stringDecryptedAsByteArray);
				}
				catch(Exception)
				{
					return "-crypt failure start-\n" + messageEncrypted + "\n-crypt failure end-";
				}
			}
		}
}

