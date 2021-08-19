using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TDAmeritrade.Console
{
    class HelperMethods
    {
        bool _TESTING = true;
        public bool CredintialFound = false;

        private string _cKey = "";
        public string Consumerkey { get { return _cKey; } }

        public void LoadCredintialsFromFile(string fileName = "PriceHistoryBank")
        {
            string userAppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = string.Format("{0}\\{1}.txt", userAppFolder, fileName);
            string filePathTest = string.Format("{0}\\{1}.txt", userAppFolder, fileName + "Test");
            //string filePathTest = string.Format("{0}\\{1}.txt", userAppFolder, fileName + "Test" + System.Guid.NewGuid());

            System.Console.WriteLine();
            System.Console.WriteLine(string.Format("Checking Credintial File: {0}", filePath));

            if ((!_TESTING && !CredentialFileExists(filePath)) || _TESTING && !CredentialFileExists(filePathTest))
            {
                System.Console.WriteLine("Credintial File NOT FOUND");
                System.Console.WriteLine("Would you like to create a Credintial File now? ['y' or 'n']");

                var option = System.Console.ReadKey();

                while (option.Key != ConsoleKey.Y && option.Key != ConsoleKey.N)
                {
                    DisplayInvalidInput(option.Key.ToString());
                    option = System.Console.ReadKey();
                }

                System.Console.WriteLine();

                switch (option.Key)
                {
                    case ConsoleKey.Y:
                        CreateCredintialFile(filePath, filePathTest); break;
                    case ConsoleKey.N:
                        CredintialFound = false;
                        return;
                }
            }

            LoadFileCredential(filePath, filePathTest);
        }

        private void LoadFileCredential(string filePath, string filePathTest)
        {
            string jsonRead = !_TESTING ? File.ReadAllText(filePath) : File.ReadAllText(filePathTest);

            var job = JObject.Parse(jsonRead);
            if (job.ContainsKey("cKey")) _cKey = job["cKey"].ToString();
            //System.Console.WriteLine("Read from file for encrypted cKey {0}", _cKey);
            //System.Console.WriteLine("Read from file for decrypted cKey {0}", DecryptCredintialFileValue(_cKey));

            CredintialFound = true;
        }

        private bool CredentialFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        private void CreateCredintialFile(string filePath, string filePathTest)
        {
            StringBuilder data = new StringBuilder();
            data.Append("{");

            System.Console.WriteLine("Paste consumer key : (https://developer.tdameritrade.com/apis)");
            System.Console.WriteLine("[Example Consumer Key: SFGJLH48R4VNVLS84HVS8U430TOIKJV9]");

            _cKey = System.Console.ReadLine();
            _cKey = !_TESTING ? _cKey.Trim() : "SFGJLH48R4VNVLS84HVS8U430TOIKJV9"; // for development testing

            // TODO: encrypt _cKey before writing to file with system variable 
            System.Console.WriteLine(string.Format("Validating cKey Length: {0}", _cKey));

            while (!CheckConsumerKey(_cKey))
            {
                DisplayInvalidInput(_cKey);
                _cKey = System.Console.ReadLine();
            }

            data.Append(string.Format("\"cKey\":\"{0}\"", EncryptCredintialFileValue(_cKey)));

            data.Append("}");

            if (!_TESTING) File.WriteAllTextAsync(filePath, data.ToString());
            else File.WriteAllTextAsync(filePathTest, data.ToString());
        }

        private string EncryptCredintialFileValue(string value)
        {
            string key = GetManagemenKey();
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(value);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);

            if (array.Length <= 0) throw new ArgumentException("Encrypted value cannot be empty or null");

            return Convert.ToBase64String(array);
        }

        private string DecryptCredintialFileValue(string value)
        {
            string key = GetManagemenKey();

            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(value);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }

            throw new ArgumentException("Decrypted value cannot be empty or null");
        }

        private string GetManagemenKey()
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.physicaladdress?view=net-5.0

            string key = ""; // example key "b14ca5898a4e4133bbce2ea2315a1916 length of 32

            //System.Console.WriteLine();
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard");
            ManagementObjectCollection information = searcher.Get();
            foreach (ManagementObject obj in information)
            {
                foreach (PropertyData data in obj.Properties)
                {
                    //System.Console.WriteLine("{0} = {1}", data.Name, data.Value);
                    key += data.Value.ToString().Replace(" ", String.Empty).Replace("/", String.Empty).Replace(".", String.Empty);
                }
                //System.Console.WriteLine();
            }

            //System.Console.WriteLine("salt = {0}", key);
            //System.Console.WriteLine("salt.Length = {0}", key.Length);
            if (String.IsNullOrEmpty(key)) throw new ArgumentException("salt cannot be empty or null");

            if (key.Length >= 32) return key.Substring(0, 32);

            for(int i = key.Length; i < 32; i++) key += "1";

            //System.Console.WriteLine("salt = {0}", key);
            //System.Console.WriteLine("salt.Length = {0}", key.Length);
            return key;
        }

        private bool CheckConsumerKey(string line)
        {
            // Example Consumer Key: SFGJLH48R4VNVLS84HVS8U430TOIKJV9
            return line.Length > 32 ? false : true;
        }

        private void DisplayInvalidInput(string input)
        {
            System.Console.WriteLine(" [Entered \"" + input + "\"] Input not valid...");
        }
    }
}
