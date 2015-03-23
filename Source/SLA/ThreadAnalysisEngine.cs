﻿//===-----------------------------------------------------------------------==//
//
//                Lockpwn - a Static Lockset Analyser for Boogie
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

namespace Lockpwn
{
  internal sealed class ThreadAnalysisEngine
  {
    private AnalysisContext AC;
    private ExecutionTimer Timer;

    internal ThreadAnalysisEngine(AnalysisContext ac)
    {
      Contract.Requires(ac != null);
      this.AC = ac;
    }

    internal void Run()
    {
      if (ToolCommandLineOptions.Get().VerboseMode)
        Console.WriteLine(". ThreadAnalysis");

      if (ToolCommandLineOptions.Get().MeasureTime)
      {
        this.Timer = new ExecutionTimer();
        this.Timer.Start();
      }

      Analysis.Factory.CreateThreadCreationAnalysis(this.AC).Run();
      Analysis.Factory.CreateLockAbstraction(this.AC).Run();

      Refactoring.Factory.CreateLockRefactoring(this.AC).Run();
      Refactoring.Factory.CreateThreadRefactoring(this.AC).Run();

      Analysis.Factory.CreateSharedStateAnalysis(this.AC).Run();

      if (ToolCommandLineOptions.Get().MeasureTime)
      {
        this.Timer.Stop();
        Console.WriteLine("... ThreadAnalysis done [{0}]", this.Timer.Result());
      }
    }
  }
}