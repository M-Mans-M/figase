using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataUploader
{
    internal class Program
    {
        private static string connectionString = null;
        private static int addedPersons = 0;

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();
            connectionString = config.GetSection("ConnectionStrings").GetValue(typeof(string), "MySqlDatabase") as string;
            
            Console.Write("Begin adding data in Database... Press enter to stop.");

            var cts = new CancellationTokenSource();
            _ = Task.Run(async () => await runAddingPersons(cts));

            var input = Console.ReadLine();
            Console.WriteLine("Stop requested...");
            cts.Cancel();

            Task.Delay(1000).Wait();
            Console.WriteLine("Total added: " + addedPersons);
            Console.ReadLine();
        }

        private static async Task runAddingPersons(CancellationTokenSource cts)
        {
            while (!cts.IsCancellationRequested)
            {
                var result = await tryAddPerson();
                if (result) Interlocked.Increment(ref addedPersons);
            }
        }

        private static async Task<bool> tryAddPerson()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var ticks = DateTime.Now.Ticks;
                    var person = new 
                    {
                        Login = $"load_{ticks}",
                        Password = "some_password",
                        FirstName = $"firstName_{ticks}",
                        LastName = $"lastName_{ticks}",
                        Age = 1,
                        Gender = 1,
                        Hobby = 1,
                        City = "default city"
                    };

                    using (var command = new MySqlCommand($"INSERT INTO persons (Login, Password, FirstName, LastName, Age, Gender, Hobby, City) VALUES ('{person.Login}','{person.Password}','{person.FirstName}','{person.LastName}',{person.Age},{(int)person.Gender},{(int)person.Hobby},'{person.City}')", connection))
                    {
                        await command.ExecuteScalarAsync();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
