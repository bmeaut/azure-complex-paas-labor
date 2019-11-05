﻿using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyNewHome.Bll
{
    public class PetService : IDisposable
    {
        private const string DatabaseId = "MyNewHome";
        private const string ContainerId = "Pets";
        private const string PartitionKey = "/type";

        private CosmosClient _cosmosClient;
        private CosmosDatabase _database;
        private CosmosContainer _container;

        private readonly string _cosmosConnectionString;
        private readonly TelemetryClient _telemetryClient;

        public PetService(IConfiguration configuration, TelemetryClient telemetryClient)
        {
            _cosmosConnectionString = configuration.GetValue<string>("CosmosConnectionString");
            _telemetryClient = telemetryClient;
        }

        public async Task InitAsync()
        {
            _cosmosClient = new CosmosClient(_cosmosConnectionString);
            _database = await _cosmosClient.Databases.CreateDatabaseIfNotExistsAsync(DatabaseId);
            _container = await _database.Containers.CreateContainerIfNotExistsAsync(ContainerId, PartitionKey);
        }

        public async Task<Pet> GetPetAsync(string id, PetType type)
        {
            await InitAsync();

            return await _container.Items.ReadItemAsync<Pet>((int)type, id);
        }

        public async Task<IEnumerable<Pet>> ListPetsAsync()
        {
            await InitAsync();

            // Selecting all pets is a cross partition query. 
            // We set the max concurrency to 4, which controls the max number of partitions that our client will query in parallel.
            return await _container.Items.CreateItemQuery<Pet>("SELECT * FROM c WHERE c.published ORDER BY c._ts DESC", maxConcurrency: 4, maxItemCount: 20).ToList();
        }

        public async Task<Pet> AddPetAsync(Pet pet)
        {
            await InitAsync();

            if (pet.Id == null) pet.Id = Guid.NewGuid().ToString();
            pet.Published = false;

            if (pet.Birthdate > DateTime.Now) throw new ArgumentException("Pet birthdate cannot be in the future.", "birthdate");

            var newPet = await _container.Items.CreateItemAsync((int)pet.Type, pet);

            _telemetryClient.TrackEvent(
                "New pet added.",
                new Dictionary<string, string>
                {
                    { "Pet type", pet.Type.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "New pet", 1 },
                });

            return newPet;
        }

        public async Task<Pet> UpdatePetAsync(Pet pet)
        {
            await InitAsync();

            return await _container.Items.ReplaceItemAsync((int)pet.Type, pet.Id, pet);
        }

        public async Task DeletePetAsync(string id, PetType type)
        {
            await InitAsync();

            await _container.Items.DeleteItemAsync<Pet>((int)type, id);
        }

        public void Dispose()
        {
            _cosmosClient?.Dispose();
        }
    }
}
