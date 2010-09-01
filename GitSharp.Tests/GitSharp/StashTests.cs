// 
// StashTests.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.IO;
using System.Linq;
using GitSharp.Core.Tests;
using GitSharp.Commands;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Collections;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class StashTests : ApiTestCase
	{
		void EnsureModified (FileInfo file)
		{
			file.Refresh ();
			file.LastWriteTime = file.LastWriteTime.AddSeconds (1);
		}
		
		[Test]
		public void CreateStash ()
		{
			db = createWorkRepository ();
			using (var repo = GetTrashRepository ())
			{
				FileInfo testFile = writeTrashFile ("test", "111");
				repo.Index.Add ("test");
				repo.Commit ("test1");
				
				writeTrashFile ("test", "222");
				EnsureModified (testFile);
				
				Stash s = repo.Stashes.Create ("s1");
				Assert.AreEqual ("s1", s.Comment);
				
				Assert.AreEqual (1, repo.Stashes.Count ());
				s = repo.Stashes.First ();
				Assert.AreEqual ("s1", s.Comment);
				
				Assert.AreEqual ("111", File.ReadAllText (testFile.FullName));
				
				s.Apply ();
				
				Assert.AreEqual ("222", File.ReadAllText (testFile.FullName));
				
				repo.Stashes.Clear ();
				Assert.AreEqual (0, repo.Stashes.Count ());
			}
		}
		
		[Test]
		public void RestoreModificationStash ()
		{
			db = createWorkRepository ();
			using (var repo = GetTrashRepository ())
			{
				// Commit initial files
				FileInfo testFile1 = writeTrashFile ("test1", "t1-111");
				repo.Index.Add ("test1");
				FileInfo testFile2 = writeTrashFile ("test2", "t2-111");
				repo.Index.Add ("test2");
				FileInfo testFile3 = writeTrashFile ("test3", "t3-111");
				repo.Index.Add ("test3");
				repo.Commit ("Test commit");

				// Modify test1, stage it and modify again
				writeTrashFile ("test1", "t1-222");
				EnsureModified (testFile1);
				repo.Index.Add ("test1");
				writeTrashFile ("test1", "t1-333");
				EnsureModified (testFile1);
				
				// Modify test2 without staging
				writeTrashFile ("test2", "t2-222");
				EnsureModified (testFile2);
				
				// Modify test3 and stage
				writeTrashFile ("test3", "t3-222");
				EnsureModified (testFile3);
				repo.Index.Add ("test3");
				
				Stash s = repo.Stashes.Create ();
				Assert.AreEqual (1, repo.Stashes.Count ());
				
				Assert.AreEqual ("t1-111", File.ReadAllText (testFile1.FullName));
				Assert.AreEqual ("t2-111", File.ReadAllText (testFile2.FullName));
				Assert.AreEqual ("t3-111", File.ReadAllText (testFile3.FullName));
				
				s.Apply ();
				
				Assert.AreEqual ("t1-333", File.ReadAllText (testFile1.FullName));
				Assert.AreEqual ("t2-222", File.ReadAllText (testFile2.FullName));
				Assert.AreEqual ("t3-222", File.ReadAllText (testFile3.FullName));
				
				var status = repo.Status;
				AssertStatus (status, "test1", status.Staged);
				AssertStatus (status, "test2", status.Staged);
				AssertStatus (status, "test3", status.Staged);
			}
		}
		
		void AssertStatus (RepositoryStatus results, string file, params HashSet<string>[] fileStatuses)
		{
			Assert.IsNotNull(results);
			
			var allStatus = new HashSet<string> [] {
				results.Added,
				results.MergeConflict,
				results.Missing,
				results.Modified,
				results.Removed,
				results.Staged,
				results.Untracked
			};
			
			var allStatusName = new string[] {
				"Added",
				"MergeConflict",
				"Missing",
				"Modified",
				"Removed",
				"Staged",
				"Untracked"
			};
			
			for (int n=0; n<allStatus.Length; n++) {
				var status = allStatus [n];
				if (((IList)fileStatuses).Contains (status))
					Assert.IsTrue (status.Contains (file), "File " + file + " not found in " + allStatusName[n] + " collection");
				else
					Assert.IsFalse (status.Contains (file), "File " + file + " should no be in " + allStatusName[n] + " collection");
			}
		}
	}
}

