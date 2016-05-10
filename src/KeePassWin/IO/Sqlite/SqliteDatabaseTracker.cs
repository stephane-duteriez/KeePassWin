﻿using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;

namespace KeePass
{
    public class SqliteDatabaseTracker : IDatabaseTracker
    {
        private readonly StorageItemAccessList _accessList;

        public SqliteDatabaseTracker()
        {
            _accessList = StorageApplicationPermissions.FutureAccessList;
        }

        public async Task<bool> AddDatabaseAsync(IFile dbFile)
        {
            using (var db = new KeePassSqliteContext())
            {
                if (db.KeePassDatabases.Include(e => e.KeePass).FirstOrDefault(e => e.KeePass.Path == dbFile.Path) != null)
                {
                    return false;
                }

                var entry = new DatabaseEntry
                {
                    KeePass = new File
                    {
                        Path = dbFile.Path,
                        AccessToken = Guid.NewGuid().ToString()
                    }
                };

                Debug.Assert(dbFile.AsStorageItem() != null);
                _accessList.AddOrReplace(entry.KeePass.AccessToken, dbFile.AsStorageItem());

                db.Add(entry, behavior: GraphBehavior.SingleObject);
                db.Add(entry.KeePass, behavior: GraphBehavior.SingleObject);

                await db.SaveChangesAsync();
            }

#if DEBUG
            using (var db = new KeePassSqliteContext())
            {
                var result = db.KeePassDatabases
                     .Include(e => e.KeePass)
                     .FirstOrDefault(e => e.KeePass.Path == dbFile.Path);

                Debug.Assert(result != null);
            }
#endif

            return true;
        }

        public async Task AddKeyFileAsync(IFile dbFile, IFile keyFile)
        {
            using (var db = new KeePassSqliteContext())
            {
                var entry = db.KeePassDatabases
                    .Include(e => e.KeePass)
                    .FirstOrDefault(e => e.KeePass.Path == dbFile.Path);

                if (entry == null)
                {
                    return;
                }

                entry.Key = new File
                {
                    Path = keyFile.Path,
                    AccessToken = Guid.NewGuid().ToString()
                };

                Debug.Assert(keyFile.AsStorageItem() != null);
                _accessList.AddOrReplace(entry.Key.AccessToken, keyFile.AsStorageItem());

                db.Add(entry.Key, behavior: GraphBehavior.SingleObject);

                await db.SaveChangesAsync();
            }

#if DEBUG
            using (var db = new KeePassSqliteContext())
            {
                var result = db.KeePassDatabases
                     .Include(e => e.KeePass)
                     .Include(e => e.Key)
                     .FirstOrDefault(e => e.KeePass.Path == dbFile.Path);

                Debug.Assert(result != null);
                Debug.Assert(result.Key.Path == keyFile.Path);
            }
#endif
        }

        public async Task<IFile> GetKeyFileAsync(IFile dbFile)
        {
            using (var db = new KeePassSqliteContext())
            {
                var entry = db.KeePassDatabases
                    .Include(d => d.KeePass)
                    .Include(d => d.Key)
                    .FirstOrDefault(d => d.KeePass.Path == dbFile.Path);

                var token = entry?.Key?.AccessToken;

                if (token == null || !_accessList.ContainsItem(token))
                {
                    return null;
                }

                if (!_accessList.ContainsItem(token))
                {
                    return null;
                }

                var key = await _accessList.GetFileAsync(token);

                if (key.IsAvailable)
                {
                    return new StorageItemFile(key);
                }
                else
                {
                    _accessList.Remove(token);
                    return null;
                }
            }
        }

        public async Task<IFile> GetDatabaseAsync(KeePassId id)
        {
            using (var db = new KeePassSqliteContext())
            {
                var entry = await db.KeePassDatabases
                    .Include(e => e.KeePass)
                    .FirstOrDefaultAsync(e => e.KeePass.Path == id.Id);

                return await GetDatabaseAsync(entry?.KeePass?.AccessToken);
            }
        }

        public async Task<IFile> GetDatabaseAsync(string accessToken)
        {
            if (accessToken == null)
            {
                return null;
            }

            try
            {
                return new StorageItemFile(await _accessList.GetFileAsync(accessToken));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<IFile>> GetDatabasesAsync()
        {
            using (var db = new KeePassSqliteContext())
            {
                var result = new List<IFile>();

                foreach (var file in db.KeePassDatabases.Include(k => k.KeePass))
                {
                    Debug.Assert(file.KeePass != null, "Ensure EF loads the KeePass file info");

                    var dbStorageItem = await GetDatabaseAsync(file.KeePass.AccessToken);

                    if (dbStorageItem != null)
                    {
                        result.Add(dbStorageItem);
                    }
                    else
                    {
                        // There was a problem with the db cache
                        db.KeePassDatabases.Remove(file);
                    }
                }

                await db.SaveChangesAsync();

                return result;
            }
        }

        private string GetDatabaseToken(KeePassId token) => $"{token}.kdbx";

        private string GetKeyToken(KeePassId token) => $"{token}.key";
    }
}