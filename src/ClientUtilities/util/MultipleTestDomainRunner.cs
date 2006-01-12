using System;
using NUnit.Core;

namespace NUnit.Util
{
	/// <summary>
	/// Summary description for MultipleTestDomainRunner.
	/// </summary>
	public class MultipleTestDomainRunner : AggregatingTestRunner
	{
		#region Constructors
		public MultipleTestDomainRunner() : base( 0 ) { }

		public MultipleTestDomainRunner( int runnerID ) : base( runnerID ) { }
		#endregion

		#region Load Method Overrides
		public override bool Load( string assemblyName)
		{
			return Load(assemblyName, string.Empty);
		}

		public override bool Load(string assemblyName, string testName)
		{
			CreateRunners( 1 );
			return runners[0].Load(assemblyName, testName);
		}

		public override bool Load( string projectName, string[] assemblies )
		{
			this.projectName = projectName;
			CreateRunners( assemblies.Length );

			bool result = true;
			for( int index = 0; index < assemblies.Length; index++ )
				if ( !runners[index].Load( assemblies[index] ) )
					result = false;

			return result;
		}

		public override bool Load( string projectName, string[] assemblies, string testName )
		{
			this.projectName = projectName;
			CreateRunners( assemblies.Length );

			//TODO: Loading a namespace or fixture needs work
			bool result = true;
			for( int index = 0; index < assemblies.Length; index++ )
				if ( !runners[index].Load( assemblies[index], testName ) )
					result = false;

			return result;
		}

		private void CreateRunners( int count )
		{
			runners = new TestRunner[count];
			for( int index = 0; index < count; index++ )
				runners[index] = new TestDomain( this.runnerID * 100 + index + 1 );
		}
		#endregion
	}
}
