﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.FunctionalTests;
using Microsoft.Data.Entity.FunctionalTests.TestModels;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Framework.DependencyInjection;
using Npgsql;

namespace EntityFramework7.Npgsql.FunctionalTests
{
    public class MonsterFixupNpgsqlTest : MonsterFixupTestBase
    {
        private static readonly HashSet<string> _createdDatabases = new HashSet<string>();

        private static readonly ConcurrentDictionary<string, AsyncLock> _creationLocks
            = new ConcurrentDictionary<string, AsyncLock>();

        protected override IServiceProvider CreateServiceProvider(bool throwingStateManager = false)
        {
            var serviceCollection = new ServiceCollection()
                .AddEntityFramework()
                .AddNpgsql()
                .ServiceCollection();

            if (throwingStateManager)
            {
                serviceCollection.AddScoped<IStateManager, ThrowingMonsterStateManager>();
            }

            return serviceCollection.BuildServiceProvider();
        }

        protected override DbContextOptions CreateOptions(string databaseName)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseNpgsql(CreateConnectionString(databaseName));

            return optionsBuilder.Options;
        }

        private static string CreateConnectionString(string name)
        {
            return NpgsqlTestStore.CreateConnectionString(name);
        }

        protected override async Task CreateAndSeedDatabase(string databaseName, Func<MonsterContext> createContext)
        {
            var creationLock = _creationLocks.GetOrAdd(databaseName, n => new AsyncLock());
            using (await creationLock.LockAsync())
            {
                if (!_createdDatabases.Contains(databaseName))
                {
                    using (var context = createContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();
                        context.SeedUsingFKs();
                    }

                    _createdDatabases.Add(databaseName);
                }
            }
        }

        public override void OnModelCreating<TMessage, TProductPhoto, TProductReview>(ModelBuilder builder)
        {
            base.OnModelCreating<TMessage, TProductPhoto, TProductReview>(builder);
            builder.Entity<TMessage>().Key(e => e.MessageId);
            builder.Entity<TProductPhoto>().Key(e => e.PhotoId);
            builder.Entity<TProductReview>().Key(e => e.ReviewId);
        }
    }
}