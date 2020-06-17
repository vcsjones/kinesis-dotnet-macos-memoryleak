using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;

namespace kinesis_dotnet_macos_memoryleak
{
    class Program
    {
        private static bool keepRunning = true;
        private static string AccessKey = "[DELETED]";
        private static string Secret = "[DELETED]";

        private static Random rgen = new Random();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Started program!");

            IAmazonKinesis kinesisClient = new AmazonKinesisClient(AccessKey, Secret, RegionEndpoint.USWest2);

            // Keeps the program running till the Ctrl-C shortcut is used, then stops it gracefully.
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                Program.keepRunning = false;
            };

            while (Program.keepRunning) {
                Console.WriteLine("Sending test record...");
                await SendRecord(kinesisClient);
                Console.WriteLine("Sent!");
            }

            Console.WriteLine("Exited gracefully.");
        }

        protected static async Task SendRecord(IAmazonKinesis kinesisClient) 
        {
            byte[] data = Encoding.UTF8.GetBytes(DateTime.Now.ToString());
            using MemoryStream stream = new MemoryStream(data);

            var response = await kinesisClient.PutRecordAsync(new PutRecordRequest() 
                {
                    StreamName = "vcsjones-test-stream",
                    PartitionKey = "" + rgen.NextDouble() * 100000,
                    Data = stream
                });
            Console.WriteLine(response.HttpStatusCode);
        }
    }
}
