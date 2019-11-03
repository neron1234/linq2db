﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NUnit.Framework;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class MiniProfilerTests : TestBase
	{
		public class MiniProfilerDataContext : DataConnection
		{
			public MiniProfilerDataContext(string configurationString)
				: base(GetDataProvider(), GetConnection(configurationString)) { }

			private static IDataProvider GetDataProvider()
			{
				return new SqlServerDataProvider("MiniProfiler." + ProviderName.SqlServer2000, SqlServerVersion.v2012);
			}

			private static IDbConnection GetConnection(string configurationString)
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = true;
				var dbConnection = new SqlConnection(GetConnectionString(configurationString));
				return new ProfiledDbConnection(dbConnection, MiniProfiler.Current);
			}
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			try
			{
				using (var mpcon = new MiniProfilerDataContext(context))
				{
					mpcon.GetTable<Northwind.Category>().ToList();
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = false;
			}
		}

		// provider-specific tests
		// tests must check all code, that use provider-specific functionality for specific provider
		// also test must create new instance of provider, to not benefit from existing instance
		[Test]
		public void TestAccess([IncludeDataSources(ProviderName.Access)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			var unmapped = type == ConnectionType.MiniProfilerNoMappings;// || type == ConnectionType.SimpleMiniProfilerNoMappings;
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				using (var db = CreateDataConnection(new AccessDataProvider(), context, type))
				{
					// TODO: reenable, when transactions fixed
#if NET46
					// assert custom schema table access
					var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);
					Assert.AreEqual(!unmapped, schema.Tables.Any(t => t.ForeignKeys.Any()));
#endif

					var trace = string.Empty;
					db.OnTraceConnection += (TraceInfo ti) =>
					{
						if (ti.TraceInfoStep == TraceInfoStep.BeforeExecute)
							trace = ti.SqlText;
					};

					// assert provider-specific parameter type name
					// DateTime, DateTime2 => Date
					// Text => LongVarChar
					// NText => LongVarWChar
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime)));
					Assert.True(trace.Contains("DECLARE @p Date "));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE datetimeDataType = @p", new DataParameter("@p", new DateTime(2012, 12, 12, 12, 12, 12), DataType.DateTime2)));
					Assert.True(trace.Contains("DECLARE @p Date "));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE textDataType = @p", new DataParameter("@p", "567", DataType.Text)));
					Assert.True(trace.Contains("DECLARE @p LongVarChar(3)"));
					Assert.AreEqual(2, db.Execute<int>("SELECT ID FROM AllTypes WHERE ntextDataType = @p", new DataParameter("@p", "111", DataType.NText)));
					Assert.True(trace.Contains("DECLARE @p LongVarWChar(3)"));
				}
			}
		}

		public enum ConnectionType
		{
			Raw,
			MiniProfiler,
			MiniProfilerNoMappings,
			// disabled for now, as it makes things too complicated
			//SimpleMiniProfiler,
			//SimpleMiniProfilerNoMappings
		}

		private DataConnection CreateDataConnection(IDataProvider provider, string context, ConnectionType type)
		{
			var ms = new MappingSchema();
			DataConnection db = null;
			db = new DataConnection(provider, () =>
			{
				var cn = provider.CreateConnection(DataConnection.GetConnectionString(context));

				switch (type)
				{
					case ConnectionType.MiniProfilerNoMappings      :
					case ConnectionType.MiniProfiler                :
						return new ProfiledDbConnection((DbConnection)cn, MiniProfiler.Current);
					//case ConnectionType.SimpleMiniProfilerNoMappings:
					//case ConnectionType.SimpleMiniProfiler          :
					//	return new SimpleProfiledConnection(cn, MiniProfiler.Current);
				}

				return cn;
			});

			switch (type)
			{
				case ConnectionType.MiniProfiler:
					ms.SetConvertExpression<ProfiledDbConnection, IDbConnection>(db => db.WrappedConnection);
					break;
				//case ConnectionType.SimpleMiniProfiler:
				//	ms.SetConvertExpression<SimpleProfiledConnection, IDbConnection>(db => db.WrappedConnection);
				//	break;
			}

			db.AddMappingSchema(ms);

			return db;
		}
	}
}
