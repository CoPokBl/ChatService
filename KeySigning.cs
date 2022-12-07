using System.Security.Cryptography;
using System.Text;

namespace ChatService; 

public static class KeySigning {

    public static bool VerifySignature(string pubKey, string signatureText, string text) {
        try {
            DSACryptoServiceProvider verifier = new();
            verifier.FromXmlString(pubKey);
            byte[] textData = Encoding.UTF8.GetBytes(text);
            byte[] signature = Convert.FromBase64String(signatureText);
            return verifier.VerifyData(textData, signature);
        }
        catch (Exception) {
            return false;
        }
    }
    
    public static string SignText(string privKey, string text) {
        DSACryptoServiceProvider signer = new();
        signer.FromXmlString(privKey);
        byte[] textData = Encoding.UTF8.GetBytes(text);
        byte[] signature = signer.SignData(textData);
        return Convert.ToBase64String(signature);
    }
    
    /// <summary>
    /// Generates a new DSA key pair.
    /// </summary>
    /// <returns>(Private key, Public key)</returns>
    public static (string, string) GenerateKeyPair() {
        DSACryptoServiceProvider dsa = new();
        return (dsa.ToXmlString(true), dsa.ToXmlString(false));
    }
    
}