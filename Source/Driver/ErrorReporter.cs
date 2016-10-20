﻿//===-----------------------------------------------------------------------==//
//
// Lockpwn - blazing fast symbolic analysis for concurrent Boogie programs
//
// Copyright (c) 2015 Pantazis Deligiannis (pdeligia@me.com)
//
// This file is distributed under the MIT License. See LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using Microsoft.Boogie;

using Lockpwn.IO;

namespace Lockpwn
{
  public sealed class ErrorReporter
  {
//    private EntryPointPair Pair;

    public HashSet<string> UnprotectedResources;

    public bool FoundErrors;

    enum ErrorMsgType
    {
      Error,
      Note,
      NoError
    }

    public ErrorReporter(/*EntryPointPair pair*/)
    {
//      this.Pair = pair;
      this.UnprotectedResources = new HashSet<string>();
      this.FoundErrors = false;
    }

    public int ReportCounterexample(Counterexample error)
    {
      Contract.Requires(error != null);
      int errors = 0;

      if (error is AssertCounterexample)
      {
        AssertCounterexample cex = error as AssertCounterexample;

        if (QKeyValue.FindBoolAttribute(cex.FailingAssert.Attributes, "race_checking"))
        {
          errors += this.ReportRace(cex);
        }
        else
        {
          if (ToolCommandLineOptions.Get().VerboseMode)
            Output.PrintLine("..... Error: AssertCounterexample");
          errors++;
        }
      }

      if (errors > 0)
        this.FoundErrors = true;

      return errors;
    }

    private int ReportRace(AssertCounterexample cex)
    {
//      this.PopulateModelWithStatesIfNecessary(cex);

      var resource = this.GetSharedResourceName(cex.FailingAssert.Attributes);
      var conflictingActions = this.GetConflictingAccesses(cex, resource);
      this.UnprotectedResources.Add(resource);

      if (ToolCommandLineOptions.Get().VerboseMode)
        Output.PrintLine("..... Conflict in memory region: " + resource);
      if (ToolCommandLineOptions.Get().ShowErrorModel)
        this.Write(cex.Model, conflictingActions);

      int errorCounter = 0;
      foreach (var action1 in conflictingActions)
      {
        foreach (var action2 in conflictingActions)
        {
          if (this.AnalyseConflict(action1.Key, action2.Key, action1.Value, action2.Value))
            errorCounter++;
        }
      }

      return errorCounter == 0 ? 1 : errorCounter;
    }

    private bool AnalyseConflict(string state1, string state2, AssumeCmd assume1, AssumeCmd assume2)
    {
//      string t1 = this.GetThreadName(assume1.Attributes);
//      string t2 = this.GetThreadName(assume2.Attributes);

//      if (!this.Pair.EntryPoint1.Name.Equals(ep1))
//        return false;
//      if (!this.Pair.EntryPoint2.Name.Equals(ep2))
//        return false;

      string access1 = this.GetAccessType(assume1.Attributes);
      string access2 = this.GetAccessType(assume2.Attributes);

      if (access1.Equals("read") && access2.Equals("read"))
        return false;

//      var sourceInfoForAccess1 = new SourceLocationInfo(assume1.Attributes);
//      var sourceInfoForAccess2 = new SourceLocationInfo(assume2.Attributes);

//      ErrorReporter.ErrorWriteLine("\n" + sourceInfoForAccess1.GetFile() + ":",
//        "potential " + access1 + "-" + access2 + " race:\n", ErrorMsgType.Error);

//      Console.Error.Write(access1 + " by thread " + t1 + ", ");
//      Console.Error.WriteLine(sourceInfoForAccess1.ToString());
//      sourceInfoForAccess1.PrintStackTrace();

//      Console.Error.WriteLine(access2 + " by entry point " + ep2 + ", " + sourceInfoForAccess2.ToString());
//      sourceInfoForAccess2.PrintStackTrace();

      return true;
    }

    private Dictionary<string, AssumeCmd> GetConflictingAccesses(AssertCounterexample cex, string resource)
    {
      var assumes = new Dictionary<string, AssumeCmd>();
      foreach (var block in cex.Trace)
      {
        foreach (var assume in block.Cmds.OfType<AssumeCmd>())
        {
          var sharedResource = this.GetSharedResourceName(assume.Attributes);
          if (sharedResource == null || !sharedResource.Equals(resource))
            continue;

          var access = this.GetAccessType(assume.Attributes);
          if (access == null)
            continue;

          var state = this.GetAccessStateName(assume.Attributes);

          if (!assumes.ContainsKey(state))
            assumes.Add(state, assume);
        }
      }

      return assumes;
    }

    private string GetSharedResourceName(QKeyValue attributes)
    {
      var resource = QKeyValue.FindStringAttribute(attributes, "resource");
      return resource;
    }

    private string GetAccessType(QKeyValue attributes)
    {
      var access = QKeyValue.FindStringAttribute(attributes, "access");
      return access;
    }

    private string GetThreadName(QKeyValue attributes)
    {
      var ep = QKeyValue.FindStringAttribute(attributes, "thread");
      return ep;
    }

    private string GetAccessStateName(QKeyValue attributes)
    {
      var access = QKeyValue.FindStringAttribute(attributes, "captureState");
      return access;
    }

    private void PopulateModelWithStatesIfNecessary(Counterexample cex)
    {
      if (!cex.ModelHasStatesAlready)
      {
        cex.PopulateModelWithStates();
        cex.ModelHasStatesAlready = true;
      }
    }

    private static Model.CapturedState GetStateFromModel(string stateName, Model m)
    {
      Model.CapturedState state = null;
      foreach (var s in m.States)
      {
        if (s.Name.Equals(stateName))
        {
          state = s;
          break;
        }
      }
      return state;
    }

    public void Write(Model model, Dictionary<string, AssumeCmd> conflictingActions = null)
    {
      Output.PrintLine("*** MODEL");
//      foreach (var f in model.Functions.OrderBy(f => f.Name))
//        if (f.Arity == 0)
//        {
//          Output.PrintLine("{0} -> {1}", f.Name, f.GetConstant());
//        }
//      foreach (var f in model.Functions)
//        if (f.Arity != 0)
//        {
//          Output.PrintLine("{0} -> {1}", f.Name, "{");
//          foreach (var app in f.Apps)
//          {
//            Output.Print("  ");
//            foreach (var a in app.Args)
//              Output.Print("{0} ", a);
//            Output.PrintLine("-> {0}", app.Result);
//          }
//          if (f.Else != null)
//            Output.PrintLine("  else -> {0}", f.Else);
//          Output.PrintLine("}");
//        }

      foreach (var s in model.States)
      {
        if (conflictingActions != null &&
            !conflictingActions.Keys.Contains(s.Name))
          continue;
        if (s == model.InitialState && s.VariableCount == 0)
          continue;
        Output.PrintLine("*** STATE {0}", s.Name);
        foreach (var v in s.Variables)
          Output.PrintLine("  {0} -> {1}", v, s.TryGet(v));
        Output.PrintLine("*** END_STATE", s.Name);
      }

      Output.PrintLine("*** END_MODEL");
    }

    private int ReportRequiresFailure(CallCounterexample cex)
    {
      Console.Error.WriteLine();
      ErrorReporter.ErrorWriteLine(cex.FailingCall + ":",
        "a precondition for this call might not hold", ErrorMsgType.Error);
      ErrorReporter.ErrorWriteLine(cex.FailingRequires.Line + ":",
        "this is the precondition that might not hold", ErrorMsgType.Note);
      return 1;
    }

    private static void ErrorWriteLine(string locInfo, string message, ErrorMsgType msgtype)
    {
      Contract.Requires(message != null);

      if (!String.IsNullOrEmpty(locInfo))
      {
        Console.Error.Write(locInfo + " ");
      }

      switch (msgtype)
      {
        case ErrorMsgType.Error:
          Console.Error.Write("error: ");
          break;
        case ErrorMsgType.Note:
          Console.Error.Write("note: ");
          break;
        case ErrorMsgType.NoError:
        default:
          break;
      }

      Console.Error.WriteLine(message);
    }
  }
}

