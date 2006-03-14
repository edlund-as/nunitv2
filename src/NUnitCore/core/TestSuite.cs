#region Copyright (c) 2002-2003, James W. Newkirk, Michael C. Two, Alexei A. Vorontsov, Charlie Poole, Philip A. Craig
/************************************************************************************
'
' Copyright � 2002-2003 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov, Charlie Poole
' Copyright � 2000-2003 Philip A. Craig
'
' This software is provided 'as-is', without any express or implied warranty. In no 
' event will the authors be held liable for any damages arising from the use of this 
' software.
' 
' Permission is granted to anyone to use this software for any purpose, including 
' commercial applications, and to alter it and redistribute it freely, subject to the 
' following restrictions:
'
' 1. The origin of this software must not be misrepresented; you must not claim that 
' you wrote the original software. If you use this software in a product, an 
' acknowledgment (see the following) in the product documentation is required.
'
' Portions Copyright � 2003 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov, Charlie Poole
' or Copyright � 2000-2003 Philip A. Craig
'
' 2. Altered source versions must be plainly marked as such, and must not be 
' misrepresented as being the original software.
'
' 3. This notice may not be removed or altered from any source distribution.
'
'***********************************************************************************/
#endregion

namespace NUnit.Core
{
	using System;
	using System.Collections;
	using System.Reflection;
	using NUnit.Core.Filters;

	/// <summary>
	/// Summary description for TestSuite.
	/// </summary>
	/// 
	[Serializable]
	public class TestSuite : Test
	{
		private static readonly string EXPLICIT_SELECTION_REQUIRED = "Explicit selection required";
	
		protected enum SetUpState
		{
			SetUpNeeded,
			SetUpComplete,
			SetUpFailed
		}

		#region Fields
		/// <summary>
		/// Our collection of child tests
		/// </summary>
		private ArrayList tests = new ArrayList();

		private Type fixtureType;

		private object fixture;

		/// <summary>
		/// The test setup method for this suite
		/// </summary>
		protected MethodInfo testSetUp;

		/// <summary>
		/// The test teardown method for this suite
		/// </summary>
		protected MethodInfo testTearDown;

		/// <summary>
		/// The fixture setup method for this suite
		/// </summary>
		protected MethodInfo fixtureSetUp;

		/// <summary>
		/// The fixture teardown method for this suite
		/// </summary>
		protected MethodInfo fixtureTearDown;

		/// <summary>
		/// Indicates the status of any one-time setup needed
		/// </summary>
		protected SetUpState setUpStatus = SetUpState.SetUpNeeded;

		#endregion

		#region Constructors
		public TestSuite( string name ) 
			: base( name ) { }

		public TestSuite( string parentSuiteName, string name ) 
			: base( parentSuiteName, name ) { }

		public TestSuite( Type fixtureType )
			: base( fixtureType.FullName )
		{
			if ( fixtureType.Namespace != null )
				this.TestName.Name = FullName.Substring( FullName.LastIndexOf( '.' ) + 1 );
			this.fixtureType = fixtureType;
		}
		#endregion

		#region Public Methods
		public void Sort()
		{
			this.tests.Sort();

			foreach( Test test in Tests )
			{
				TestSuite suite = test as TestSuite;
				if ( suite != null )
					suite.Sort();
			}		
		}

		public void Add( Test test ) 
		{
			if(test.ShouldRun)
			{
				test.RunState = this.RunState;
				test.IgnoreReason = this.IgnoreReason;
			}
			test.Parent = this;
			tests.Add(test);
		}

		public void Add( object fixture )
		{
			Test test = TestFixtureBuilder.Make( fixture );
			if ( test != null )
				Add( test );
		}
		#endregion

		#region Properties
		public override IList Tests 
		{
			get { return tests; }
		}

		public bool SetUpFailed
		{
			get { return setUpStatus == SetUpState.SetUpFailed; }
		}

		public MethodInfo SetUpMethod
		{
			get { return testSetUp; }
		}

		public MethodInfo TearDownMethod
		{
			get { return testTearDown; }
		}

		public override bool IsSuite
		{
			get { return true; }
		}

		public override bool IsTestCase
		{
			get { return false; }
		}

		/// <summary>
		/// True if this is a fixture.
		/// TODO: An easier way to tell this?
		/// </summary>
		public override bool IsFixture
		{
			get	{ return false;	}
		}

		public override int TestCount
		{
			get
			{
				int count = 0;

				foreach(Test test in Tests)
				{
					count += test.TestCount;
				}
				return count;
			}
		}

		public Type FixtureType
		{
			get { return fixtureType; }
		}

		public  object Fixture
		{
			get { return fixture; }
			set { fixture = value; }
		}
		#endregion

		#region Test Overrides
		public override int CountTestCases()
		{
			return CountTestCases( TestFilter.Empty );
		}

		public override int CountTestCases(TestFilter filter)
		{
			int count = 0;

			if(this.Filter(filter)) 
			{
				foreach(Test test in Tests)
				{
					count += test.CountTestCases(filter);
				}
			}
			return count;
		}

		public override TestResult Run(EventListener listener)
		{
			return Run( listener, TestFilter.Empty);
		}
			
		public override TestResult Run(EventListener listener, TestFilter filter)
		{
			TestSuiteResult suiteResult = new TestSuiteResult( this, Name);

			listener.SuiteStarted( new TestInfo( this ) );
			long startTime = DateTime.Now.Ticks;

            switch (this.RunState)
            {
                case RunState.Runnable:
                    suiteResult.RunState = RunState.Executed;
                    DoOneTimeSetUp(suiteResult);
                    break;
                case RunState.Skipped:
                    suiteResult.Skip(this.IgnoreReason);
                    break;
                default:
                case RunState.Ignored:
                case RunState.NotRunnable:
                    suiteResult.Ignore(this.IgnoreReason);
                    break;
            }

			RunAllTests( suiteResult, listener, filter );

			if ( ShouldRun && !SetUpFailed )
				DoOneTimeTearDown( suiteResult );

			long stopTime = DateTime.Now.Ticks;
			double time = ((double)(stopTime - startTime)) / (double)TimeSpan.TicksPerSecond;
			suiteResult.Time = time;

			listener.SuiteFinished(suiteResult);
			return suiteResult;
		}

		public override bool Filter(TestFilter filter) 
		{
			return filter.Pass(this);
		}
		#endregion

		#region Virtual Methods
		protected virtual void DoOneTimeSetUp( TestResult suiteResult )
		{
			this.setUpStatus = SetUpState.SetUpComplete;
		}

		protected virtual void DoOneTimeTearDown( TestResult suiteResult )
		{
			this.setUpStatus = SetUpState.SetUpNeeded;
		}

		protected virtual void RunAllTests(
			TestSuiteResult suiteResult, EventListener listener, TestFilter filter )
		{
			foreach(Test test in ArrayList.Synchronized(Tests))
			{
				RunState saveRunState = test.RunState;
				if ( test.ShouldRun && !this.ShouldRun )
				{
					test.RunState = this.RunState;
					test.IgnoreReason = this.IgnoreReason;
				}
					
				if ( filter == null || test.Filter( filter ) )
				{
					bool skip = test.IsExplicit 
						&& ( filter == null || filter is NotFilter || filter.IsEmpty );

					if ( skip )
					{
						test.RunState = RunState.Skipped;
						test.IgnoreReason = EXPLICIT_SELECTION_REQUIRED;
					}

					TestResult result = test.Run( listener, filter );

					if ( skip ) 
						result.RunState = RunState.Skipped;
					suiteResult.AddResult( result );
				}
				
				if ( saveRunState != test.RunState ) 
				{
					test.RunState = saveRunState;
					test.IgnoreReason = null;
				}
			}
		}
		#endregion
	}
}
