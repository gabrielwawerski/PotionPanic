using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit
  {
  }
}

namespace PotionPanic.Editor.Coordination
{
  public enum SaveConflictAction
  {
    OverrideAndSave,
    CancelSave,
    KeepWorking
  }

  public sealed class CoordinationSavePathInfo
  {
    public string Path { get; }
    public string LastKnownOwner { get; }

    public CoordinationSavePathInfo(string path, string lastKnownOwner)
    {
      if (!CoordinationPathMatcher.TryNormalize(path, out var normalizedPath))
      {
        throw new ArgumentException("A normalized asset path is required.", nameof(path));
      }

      Path = normalizedPath;
      LastKnownOwner = string.IsNullOrWhiteSpace(lastKnownOwner)
        ? "No owner known"
        : lastKnownOwner;
    }
  }

  public interface ISaveConflictDialog
  {
    SaveConflictAction Show(IReadOnlyList<CoordinationSavePathInfo> paths);
  }

  internal sealed class CoordinationUncoordinatedSaveRequest
  {
    public CoordinationUncoordinatedSaveReason Reason { get; init; }
    public IReadOnlyList<string> AssetPaths { get; init; }
    public string Detail { get; init; }
  }

  internal interface IUncoordinatedSavePrompt
  {
    bool ChooseLocalSave(CoordinationUncoordinatedSaveRequest request);
    bool ConfirmLocalSave(CoordinationUncoordinatedSaveRequest request);
  }

  public interface ICoordinationEditorDialogBackend
  {
    int ShowComplex(
      string title,
      string message,
      string primary,
      string cancel,
      string alternate);

    bool ShowConfirmation(
      string title,
      string message,
      string confirm,
      string cancel);
  }

  public sealed class SaveConflictDialog : ISaveConflictDialog
  {
    private readonly ICoordinationEditorDialogBackend backend;

    public SaveConflictDialog()
      : this(new UnityCoordinationEditorDialogBackend())
    {
    }

    public SaveConflictDialog(ICoordinationEditorDialogBackend backend)
    {
      this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public SaveConflictAction Show(IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      var selected = backend.ShowComplex(
        "Coordination save conflict",
        BuildMessage(
          "Another developer currently owns the editing lease.",
          paths),
        "Override and save",
        "Cancel save",
        "Keep working");

      switch (selected)
      {
        case 0:
          return SaveConflictAction.OverrideAndSave;
        case 1:
          return SaveConflictAction.CancelSave;
        default:
          return SaveConflictAction.KeepWorking;
      }
    }

    internal static string BuildMessage(
      string introduction,
      IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      var message = new StringBuilder(introduction);
      message.AppendLine();
      message.AppendLine();
      foreach (var path in paths ?? Array.Empty<CoordinationSavePathInfo>())
      {
        message.Append(path.Path);
        message.Append("  (last known owner: ");
        message.Append(path.LastKnownOwner);
        message.AppendLine(")");
      }

      return message.ToString().TrimEnd();
    }
  }

  public sealed class UncoordinatedSavePrompt : IUncoordinatedSavePrompt
  {
    private readonly ICoordinationEditorDialogBackend backend;

    public UncoordinatedSavePrompt()
      : this(new UnityCoordinationEditorDialogBackend())
    {
    }

    public UncoordinatedSavePrompt(ICoordinationEditorDialogBackend backend)
    {
      this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    bool IUncoordinatedSavePrompt.ChooseLocalSave(
      CoordinationUncoordinatedSaveRequest request)
    {
      return backend.ShowConfirmation(
        "Coordination unavailable",
        BuildUnavailableMessage(request),
        "Save locally without coordination",
        "Keep working");
    }

    bool IUncoordinatedSavePrompt.ConfirmLocalSave(
      CoordinationUncoordinatedSaveRequest request)
    {
      return backend.ShowConfirmation(
        "Confirm uncoordinated save",
        BuildConfirmationMessage(request),
        "Save locally",
        "Cancel");
    }

    public bool ChooseLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      return ((IUncoordinatedSavePrompt)this).ChooseLocalSave(
        LegacyRequest(paths));
    }

    public bool ConfirmLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      return backend.ShowConfirmation(
        "Confirm uncoordinated save",
        SaveConflictDialog.BuildMessage(
          "This save can still conflict with another developer's work. "
            + "A local reconciliation warning will remain.",
          paths),
        "Save locally",
        "Cancel");
    }

    private static string BuildUnavailableMessage(
      CoordinationUncoordinatedSaveRequest request)
    {
      var reason = request?.Reason ?? CoordinationUncoordinatedSaveReason.Offline;
      var explanation = ReasonExplanation(reason);
      if (!string.IsNullOrWhiteSpace(request?.Detail))
      {
        explanation += " " + request.Detail.Trim();
      }

      return BuildPathMessage(
        explanation + " Coordination cannot authorize this save.",
        request?.AssetPaths);
    }

    private static string BuildConfirmationMessage(
      CoordinationUncoordinatedSaveRequest request)
    {
      return BuildPathMessage(
        "This local save can still conflict with another developer's work. "
          + "A local reconciliation warning will remain until these paths "
          + "are reconciled.",
        request?.AssetPaths);
    }

    private static string BuildPathMessage(
      string introduction,
      IReadOnlyList<string> paths)
    {
      var message = new StringBuilder(introduction);
      foreach (var path in paths ?? Array.Empty<string>())
      {
        message.AppendLine();
        message.AppendLine();
        message.Append(path);
      }

      return message.ToString();
    }

    private static string ReasonExplanation(CoordinationUncoordinatedSaveReason reason)
    {
      switch (reason)
      {
        case CoordinationUncoordinatedSaveReason.Manual:
          return "Coordination is in Manual mode.";
        case CoordinationUncoordinatedSaveReason.Reconnecting:
          return "Coordination is reconnecting.";
        case CoordinationUncoordinatedSaveReason.AuthenticationFailed:
          return "Coordination authentication failed.";
        case CoordinationUncoordinatedSaveReason.RequestTimeout:
          return "The editing-lease request timed out.";
        case CoordinationUncoordinatedSaveReason.OverrideTransportFailure:
          return "The editing-lease override could not be sent.";
        default:
          return "Coordination is offline.";
      }
    }

    private static CoordinationUncoordinatedSaveRequest LegacyRequest(
      IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      return new CoordinationUncoordinatedSaveRequest
      {
        Reason = CoordinationUncoordinatedSaveReason.Offline,
        AssetPaths = (paths ?? Array.Empty<CoordinationSavePathInfo>())
          .Select(path => path.Path)
          .ToArray(),
        Detail = string.Empty
      };
    }
  }

  internal sealed class UnityCoordinationEditorDialogBackend
    : ICoordinationEditorDialogBackend
  {
    public int ShowComplex(
      string title,
      string message,
      string primary,
      string cancel,
      string alternate)
    {
      return EditorUtility.DisplayDialogComplex(
        title,
        message,
        primary,
        cancel,
        alternate);
    }

    public bool ShowConfirmation(
      string title,
      string message,
      string confirm,
      string cancel)
    {
      return EditorUtility.DisplayDialog(title, message, confirm, cancel);
    }
  }
}
