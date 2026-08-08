using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

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

  public interface IUncoordinatedSavePrompt
  {
    bool ChooseLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths);
    bool ConfirmLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths);
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

    public bool ChooseLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      return backend.ShowConfirmation(
        "Coordination unavailable",
        SaveConflictDialog.BuildMessage(
          "Coordination is unavailable. The affected files remain unsaved.",
          paths),
        "Save locally without coordination",
        "Keep working");
    }

    public bool ConfirmLocalSave(IReadOnlyList<CoordinationSavePathInfo> paths)
    {
      return backend.ShowConfirmation(
        "Confirm uncoordinated save",
        SaveConflictDialog.BuildMessage(
          "This save will proceed without an editing lease. "
            + "Confirm the paths and owners.",
          paths),
        "Save locally",
        "Cancel");
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
