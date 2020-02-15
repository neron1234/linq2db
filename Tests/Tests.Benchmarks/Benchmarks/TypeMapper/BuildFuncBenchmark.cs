﻿using System;
using System.Data;
using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// Notes:
	// benchmark shows expected performance degradation due to indirect call
	public class BuildFuncBenchmark
	{
		private Original.TestClass _classInstance = new Original.TestClass();
		private Func<ITestClass, Guid, object[], DataTable> _functionCall;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass));
			typeMapper.RegisterWrapper<Wrapped.TestClass>();

			_functionCall = typeMapper.BuildFunc<ITestClass, Guid, object[], DataTable>(typeMapper.MapLambda((Wrapped.TestClass conn, Guid schema, object[] restrictions) => conn.GetOleDbSchemaTable(schema, restrictions)));
		}

		[Benchmark]
		public DataTable BuildFunc()
		{
			return _functionCall(_classInstance, Guid.Empty, null);
		}

		[Benchmark(Baseline = true)]
		public DataTable DirectAccess()
		{
			return _classInstance.GetOleDbSchemaTable(Guid.Empty, null);
		}
	}
}
