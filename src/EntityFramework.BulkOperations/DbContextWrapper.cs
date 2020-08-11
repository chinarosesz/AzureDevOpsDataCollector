﻿using System;
using System.Collections.Generic;
using System.Data;
using EntityFramework.BulkExtensions.Commons.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFramework.BulkExtensions.Commons.Context
{
    internal class DbContextWrapper
    {
        public EntityMapping EntityMapping { get; }
        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; }
        private bool IsInternalTransaction { get; }
        private readonly DbContext context;

        internal DbContextWrapper(DbContext context, EntityMapping entityMapping)
        {
            this.context = context;
            this.Connection = this.context.Database.GetDbConnection();
            this.Transaction = this.context.Database.CurrentTransaction?.GetDbTransaction();

            if (this.Connection.State != ConnectionState.Open)
            {
                this.Connection.Open();
            }

            IsInternalTransaction = (this.Transaction == null);
            this.Transaction = this.Transaction ?? this.context.Database.GetDbConnection().BeginTransaction();

            EntityMapping = entityMapping;
        }

        public int ExecuteSqlCommand(string command)
        {
            IDbCommand sqlCommand = Connection.CreateCommand();
            sqlCommand.Transaction = Transaction;
            sqlCommand.CommandTimeout = this.context.Database.GetCommandTimeout().Value;
            sqlCommand.CommandText = command;

            return sqlCommand.ExecuteNonQuery();
        }

        public IEnumerable<T> SqlQuery<T>(string command) where T : struct
        {
            List<T> list = new List<T>();
            IDbCommand sqlCommand = Connection.CreateCommand();
            sqlCommand.Transaction = Transaction;
            sqlCommand.CommandTimeout = this.context.Database.GetCommandTimeout().Value;
            sqlCommand.CommandText = command;

            using (IDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.FieldCount > 1) 
                    { 
                        throw new Exception("The select command must have one column only"); 
                    }

                    list.Add((T)reader.GetValue(0));
                }
            }

            return list;
        }

        public void Commit()
        {
            if (IsInternalTransaction) 
            { 
                Transaction.Commit(); 
            }
        }

        public void Rollback()
        {
            if (IsInternalTransaction)
            {
                Transaction.Rollback();
            }
        }
    }
}