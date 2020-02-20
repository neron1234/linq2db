﻿using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// benchmark shows expected extra allocation and time penalty for wrapper instance creation
	public class WrapInstanceBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private TypeMapper _typeMapper;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestClass2();
		}

		[Benchmark]
		public Wrapped.TestClass2 TypeMapper()
		{
			return _typeMapper.Wrap<Wrapped.TestClass2>(_originalInstance);
		}

		[Benchmark(Baseline = true)]
		public Original.TestClass2 DirectAccess()
		{
			return _originalInstance;
		}
	}
}
