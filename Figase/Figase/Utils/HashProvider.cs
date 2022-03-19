using System;
using System.Security.Cryptography;
using System.Text;

namespace Figase.Utils
{
    public class HashProvider
    {
        /// <summary>
        /// Вычисляет хеш по данным.
        /// </summary>
        /// <param name="data">Данные</param>
        /// <returns></returns>
        public string ComputeHash(string data)
        {
            HashAlgorithm algorithm = MD5.Create(); //or use SHA1.Create();
            byte[] buff = algorithm.ComputeHash(Encoding.UTF8.GetBytes(data));
            return ToHexString(buff);
        }

        private string ToHexString(byte[] buff)
        {
            StringBuilder sb = new StringBuilder(buff.Length);
            foreach (byte b in buff) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
