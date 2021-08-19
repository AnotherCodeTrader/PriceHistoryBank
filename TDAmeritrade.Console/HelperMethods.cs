using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
            string filePathTest = string.Format("{0}\\{1}.txt", userAppFolder, fileName + "Test" + System.Guid.NewGuid());

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
            System.Console.WriteLine(string.Format("Validating cKey Length: {0}", EncryptCredintialFileValue(_cKey)));

            while (!CheckConsumerKey(_cKey))
            {
                DisplayInvalidInput(_cKey);
                _cKey = System.Console.ReadLine();
            }

            data.Append(string.Format("\"cKey\":\"{0}\"", _cKey));

            data.Append("}");

            if (!_TESTING) File.WriteAllTextAsync(filePath, data.ToString());
            else File.WriteAllTextAsync(filePathTest, data.ToString());
        }

        private string EncryptCredintialFileValue(string value)
        {
            string mac = GetMACAddress();
            //TODO: encrypt value with mac salt
            //https://docs.microsoft.com/en-us/dotnet/standard/security/encrypting-data
            return value;
        }

        private string DecryptCredintialFileValue(string value)
        {
            string mac = GetMACAddress();
            //TODO: decrypt value with mac salt
            //https://docs.microsoft.com/en-us/dotnet/standard/security/encrypting-data
            return value;
        }

        private string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            PhysicalAddress address = nics[0].GetPhysicalAddress();
            //TODO: Test nics retrieval and mac address;
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.networkinformation.physicaladdress?view=net-5.0
            return address.ToString();
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
