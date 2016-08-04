using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using static System.IO.Directory;
using static System.IO.SearchOption;
using static Amazon.DynamoDBv2.KeyType;
using static Amazon.DynamoDBv2.ScalarAttributeType;

namespace Sample
{
    [DynamoDBTable("SomeTable")]
    public class SomeEntity
    {
        [DynamoDBHashKey]
        public String Id { get; set; }
    }

    public class SomeTable
    {
        [DynamoDBHashKey]
        public String Id { get; set; }
    }

    public class Program
    {
        public static void Main(String[] args)
        {
            using (var process = LaunchLocalDynamoDb())
            {
                ExecuteAsync().Wait();
                process.Kill();
            }
        }

        private static async Task ExecuteAsync()
        {
            var config = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:8000" };
            var client = new AmazonDynamoDBClient("KeyId", "Key", config);

            await client.CreateTableAsync(
                new CreateTableRequest
                {
                    TableName = "SomeTable",
                    AttributeDefinitions = new List<AttributeDefinition> { new AttributeDefinition("Id", S) },
                    KeySchema = new List<KeySchemaElement> { new KeySchemaElement("Id", HASH) },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                });

            await client.PutItemAsync(
                new PutItemRequest
                {
                    TableName = "SomeTable",
                    Item = new Dictionary<String, AttributeValue> { ["Id"] = new AttributeValue("SomeId") }
                });

            var context = new DynamoDBContext(client);

            var withTableName = await context.LoadAsync<SomeTable>("SomeId");
            Console.WriteLine(withTableName.Id);

            try
            {
                var withEntityName = await context.LoadAsync<SomeEntity>("SomeId");
                Console.WriteLine(withEntityName.Id);
            }
            catch (AmazonDynamoDBException ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static Process LaunchLocalDynamoDb()
        {
            var dir = new DirectoryInfo(GetCurrentDirectory());
            var dynamoJar = dir.GetFiles("DynamoDBLocal.jar", AllDirectories).Select(x => x.FullName).First();
            var info = new ProcessStartInfo("java.exe", $"-jar {dynamoJar}");
            return Process.Start(info);
        }
    }
}