
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;


#nullable enable
namespace backend
{
    public class ResultDto
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;
    }
    public class GeneralResultDto<T> : ResultDto
    {
        public T Data { get; set; }
    }

    public static class SecurityUtility
    {
        private static string Key_Base64 = "S0ApMF89ZypzXjdMfnoxW1E7I0MmL2YlY09iNElePys=";
        private static string IV_Base64 = "JWIrQl11W0YzKU9fLkRAfA==";

        public static GeneralResultDto<string> Encrypt_AES(string plainText)
        {
            GeneralResultDto<string> generalResultDto = new GeneralResultDto<string>();
            generalResultDto.Data = "";
            string str1 = "خطا در رمزنگاری - ";
            try
            {
                if (plainText == null || plainText.Length <= 0)
                {
                    generalResultDto.Message = str1 + "متن ورودی خالی است";
                    return generalResultDto;
                }
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(SecurityUtility.Key_Base64);
                    aes.IV = Convert.FromBase64String(SecurityUtility.IV_Base64);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    ICryptoTransform encryptor = aes.CreateEncryptor();
                    byte[] array;
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                                streamWriter.Write(plainText);
                            array = memoryStream.ToArray();
                        }
                    }
                    generalResultDto.Data = Convert.ToBase64String(array);
                    generalResultDto.Succeeded = true;
                }
            }
            catch (Exception ex)
            {
                string str2 = ex.InnerException?.Message != null ? ex.InnerException.Message : ex.Message;
                generalResultDto.Message = str1 + str2;
            }
            return generalResultDto;
        }

        public static GeneralResultDto<string> Decrypt_AES(string cipherText)
        {
            GeneralResultDto<string> generalResultDto = new GeneralResultDto<string>();
            generalResultDto.Data = "";
            string str1 = "خطا در رمزگشایی - ";
            try
            {
                if (cipherText == null || cipherText.Length <= 0)
                {
                    generalResultDto.Message = str1 + "اطلاعات ورودی خالی است";
                    return generalResultDto;
                }
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(SecurityUtility.Key_Base64);
                    aes.IV = Convert.FromBase64String(SecurityUtility.IV_Base64);
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    ICryptoTransform decryptor = aes.CreateDecryptor();
                    using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                generalResultDto.Data = streamReader.ReadToEnd();
                                generalResultDto.Succeeded = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str2 = ex.InnerException?.Message != null ? ex.InnerException.Message : ex.Message;
                generalResultDto.Message = str1 + str2;
            }
            return generalResultDto;
        }

        public static string SHA256_Hash(string phrase)
        {
            using (SHA256 shA256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(phrase);
                return ((IEnumerable<byte>)shA256.ComputeHash(bytes)).Aggregate<byte, string>(string.Empty, (Func<string, byte, string>)((current, x) =>
                {
                    string str = current;
                    DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
                    interpolatedStringHandler.AppendFormatted<byte>(x, "x2");
                    string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                    return str + stringAndClear;
                }));
            }
        }
    }
}
