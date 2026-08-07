using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace PotionPanic.Editor.Coordination
{
  public interface IMainThreadDispatcher
  {
    void Post(Action action);
  }

  public sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
  {
    public void Post(Action action)
    {
      action?.Invoke();
    }
  }

  public sealed class QueuedMainThreadDispatcher : IMainThreadDispatcher
  {
    private readonly Queue<Action> actions = new Queue<Action>();

    public void Post(Action action)
    {
      if (action == null)
      {
        return;
      }

      lock (actions)
      {
        actions.Enqueue(action);
      }
    }

    public void ExecutePending()
    {
      while (true)
      {
        Action action;
        lock (actions)
        {
          if (actions.Count == 0)
          {
            return;
          }

          action = actions.Dequeue();
        }

        action();
      }
    }
  }

  public sealed class UnityMainThreadDispatcher : IMainThreadDispatcher
  {
    private static readonly QueuedMainThreadDispatcher Queue = new QueuedMainThreadDispatcher();

    static UnityMainThreadDispatcher()
    {
      EditorApplication.update += Queue.ExecutePending;
    }

    public void Post(Action action)
    {
      Queue.Post(action);
    }
  }

  public interface ICoordinationGitContext
  {
    string GetBranch();
  }

  public sealed class GitCoordinationContext : ICoordinationGitContext
  {
    public string GetBranch()
    {
      try
      {
        var startInfo = new ProcessStartInfo
        {
          FileName = "git",
          Arguments = "rev-parse --abbrev-ref HEAD",
          WorkingDirectory = CoordinationProjectPaths.ProjectDirectory,
          UseShellExecute = false,
          CreateNoWindow = true,
          RedirectStandardOutput = true
        };
        using (var process = Process.Start(startInfo))
        {
          var branch = process.StandardOutput.ReadToEnd().Trim();
          process.WaitForExit();
          return process.ExitCode == 0 && branch != "HEAD" ? branch : string.Empty;
        }
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }
  }

  internal static class CoordinationProjectPaths
  {
    public static string ProjectDirectory => System.IO.Directory.GetParent(UnityEngine.Application.dataPath).FullName;
  }
}
